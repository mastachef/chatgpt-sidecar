#!/usr/bin/env node
import {
  copyFileSync,
  cpSync,
  existsSync,
  mkdirSync,
  readFileSync,
  rmSync,
  writeFileSync
} from "node:fs";
import { homedir } from "node:os";
import { basename, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { execFileSync } from "node:child_process";
import { collectRepoContext } from "../src/repo-context.mjs";
import { createHandoff } from "../src/handoff.mjs";
import { copyToClipboard, openUrl } from "../src/platform.mjs";

const command = process.argv[2] || "help";
const pluginRoot = resolve(fileURLToPath(new URL("..", import.meta.url)));
const SIDECAR_STATUS = "Preparing ChatGPT sidecar handoff";

function commandExists(name) {
  try {
    execFileSync(name, ["--version"], { stdio: "ignore", windowsHide: true });
    return true;
  } catch {
    return false;
  }
}

function codexHomePath() {
  return process.env.CODEX_HOME || join(homedir(), ".codex");
}

function readHooksFile(path) {
  if (!existsSync(path)) return { hooks: {} };
  const parsed = JSON.parse(readFileSync(path, "utf8"));
  if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
    throw new Error(`${path} must contain a JSON object`);
  }
  if (!parsed.hooks || typeof parsed.hooks !== "object" || Array.isArray(parsed.hooks)) {
    parsed.hooks = {};
  }
  return parsed;
}

function isSidecarHookGroup(group) {
  const serialized = JSON.stringify(group || {});
  return serialized.includes("sidecar-runtime") || serialized.includes(SIDECAR_STATUS);
}

function copyRuntime(destination) {
  if (resolve(pluginRoot) === resolve(destination)) return;
  rmSync(destination, { recursive: true, force: true });
  const skipped = new Set([".git", "node_modules", ".sidecar", "coverage"]);
  cpSync(pluginRoot, destination, {
    recursive: true,
    force: true,
    filter(source) {
      return !skipped.has(basename(source));
    }
  });
}

function installSlashAlias(codexHome) {
  const source = join(pluginRoot, "prompts", "sidecar.md");
  const destination = join(codexHome, "prompts", "sidecar.md");
  mkdirSync(dirname(destination), { recursive: true });
  copyFileSync(source, destination);
  return destination;
}

function installGlobalHook() {
  const codexHome = codexHomePath();
  const runtimeDir = join(codexHome, "sidecar-runtime");
  const hooksPath = join(codexHome, "hooks.json");
  mkdirSync(codexHome, { recursive: true });
  copyRuntime(runtimeDir);

  const hookScript = join(runtimeDir, "src", "hook.mjs");
  const quotedNode = `"${process.execPath}"`;
  const quotedScript = `"${hookScript}"`;
  const hookCommand = `${quotedNode} ${quotedScript}`;

  const hooksConfig = readHooksFile(hooksPath);
  const existing = Array.isArray(hooksConfig.hooks.UserPromptSubmit)
    ? hooksConfig.hooks.UserPromptSubmit
    : [];

  hooksConfig.hooks.UserPromptSubmit = [
    ...existing.filter((group) => !isSidecarHookGroup(group)),
    {
      hooks: [
        {
          type: "command",
          command: hookCommand,
          commandWindows: hookCommand,
          timeout: 45,
          statusMessage: SIDECAR_STATUS
        }
      ]
    }
  ];

  if (existsSync(hooksPath)) {
    copyFileSync(hooksPath, `${hooksPath}.backup-${Date.now()}`);
  }
  writeFileSync(hooksPath, `${JSON.stringify(hooksConfig, null, 2)}\n`, "utf8");
  const aliasPath = installSlashAlias(codexHome);

  console.log(`Installed stable Sidecar runtime at ${runtimeDir}`);
  console.log(`Installed global UserPromptSubmit hook in ${hooksPath}`);
  console.log(`Installed optional prompt alias at ${aliasPath}`);
  console.log("Fully restart Codex and trust the new global hook once, then test with:");
  console.log("sidecar: plan summarize this project without modifying files");
}

function uninstallGlobalHook() {
  const codexHome = codexHomePath();
  const runtimeDir = join(codexHome, "sidecar-runtime");
  const hooksPath = join(codexHome, "hooks.json");
  if (existsSync(hooksPath)) {
    const hooksConfig = readHooksFile(hooksPath);
    const existing = Array.isArray(hooksConfig.hooks.UserPromptSubmit)
      ? hooksConfig.hooks.UserPromptSubmit
      : [];
    hooksConfig.hooks.UserPromptSubmit = existing.filter((group) => !isSidecarHookGroup(group));
    writeFileSync(hooksPath, `${JSON.stringify(hooksConfig, null, 2)}\n`, "utf8");
  }
  rmSync(runtimeDir, { recursive: true, force: true });
  console.log("Removed the Sidecar global hook and stable runtime. Existing non-Sidecar hooks were preserved.");
}

if (command === "doctor") {
  const major = Number.parseInt(process.versions.node.split(".")[0], 10);
  const codexHome = codexHomePath();
  const aliasPath = join(codexHome, "prompts", "sidecar.md");
  const hooksPath = join(codexHome, "hooks.json");
  let globalHookInstalled = false;
  try {
    globalHookInstalled = existsSync(hooksPath) && isSidecarHookGroup(readHooksFile(hooksPath));
  } catch {
    globalHookInstalled = false;
  }
  const report = {
    node: process.version,
    nodeSupported: major >= 20,
    codexAvailable: commandExists(process.env.CODEX_BIN || "codex"),
    gitAvailable: commandExists("git"),
    platform: process.platform,
    pluginRoot,
    codexHome,
    globalHookInstalled,
    primaryTrigger: "sidecar: <mode> <request>",
    skillTrigger: "$sidecar <mode> <request>",
    slashAlias: "/prompts:sidecar <mode> <request>",
    slashAliasInstalled: existsSync(aliasPath),
    literalSlashSidecarSupportedByCodex: false
  };
  console.log(JSON.stringify(report, null, 2));
  process.exit(report.nodeSupported && report.codexAvailable && report.gitAvailable ? 0 : 1);
}

if (command === "install-global-hook") {
  installGlobalHook();
  process.exit(0);
}

if (command === "uninstall-global-hook") {
  uninstallGlobalHook();
  process.exit(0);
}

if (command === "install-slash-alias") {
  const destination = installSlashAlias(codexHomePath());
  console.log(`Installed Sidecar slash alias at ${destination}`);
  console.log("Restart Codex, then use: /prompts:sidecar plan <request>");
  process.exit(0);
}

if (command === "bundle") {
  const cwd = process.cwd();
  const request = process.argv.slice(3).join(" ") || "Review this project and produce an implementation-ready next-step plan.";
  const repo = collectRepoContext(cwd);
  const handoff = createHandoff({
    mode: "general",
    request,
    hookInput: { cwd, session_id: null, turn_id: null, model: null },
    repo,
    thread: null
  });
  copyToClipboard(handoff);
  openUrl("https://chatgpt.com/");
  console.log(handoff);
  process.exit(0);
}

console.log(`Codex ChatGPT Sidecar\n\nCommands:\n  doctor                   Check local requirements and global-hook status\n  install-global-hook      Install Sidecar in ~/.codex/hooks.json\n  uninstall-global-hook    Remove only the Sidecar global hook/runtime\n  install-slash-alias      Install /prompts:sidecar into Codex home\n  bundle [request]         Build a repo-only ChatGPT handoff\n\nInside Codex after installing the global hook:\n  sidecar: plan <request>        Most deterministic trigger\n  $sidecar plan <request>        Bundled skill trigger\n  /prompts:sidecar plan <request>  Optional slash alias\n\nNote: current Codex does not support plugin-defined literal /sidecar commands.`);
