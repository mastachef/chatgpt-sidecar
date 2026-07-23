#!/usr/bin/env node
import { copyFileSync, existsSync, mkdirSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { execFileSync } from "node:child_process";
import { collectRepoContext } from "../src/repo-context.mjs";
import { createHandoff } from "../src/handoff.mjs";
import { copyToClipboard, openUrl } from "../src/platform.mjs";

const command = process.argv[2] || "help";
const pluginRoot = resolve(fileURLToPath(new URL("..", import.meta.url)));

function commandExists(name) {
  try {
    execFileSync(name, ["--version"], { stdio: "ignore", windowsHide: true });
    return true;
  } catch {
    return false;
  }
}

if (command === "doctor") {
  const major = Number.parseInt(process.versions.node.split(".")[0], 10);
  const codexHome = process.env.CODEX_HOME || join(homedir(), ".codex");
  const aliasPath = join(codexHome, "prompts", "sidecar.md");
  const report = {
    node: process.version,
    nodeSupported: major >= 20,
    codexAvailable: commandExists(process.env.CODEX_BIN || "codex"),
    gitAvailable: commandExists("git"),
    platform: process.platform,
    pluginRoot,
    primaryTrigger: "$sidecar <request>",
    slashAlias: "/prompts:sidecar <request>",
    slashAliasInstalled: existsSync(aliasPath),
    literalSlashSidecarSupportedByCodex: false
  };
  console.log(JSON.stringify(report, null, 2));
  process.exit(report.nodeSupported && report.codexAvailable && report.gitAvailable ? 0 : 1);
}

if (command === "install-slash-alias") {
  const codexHome = process.env.CODEX_HOME || join(homedir(), ".codex");
  const source = join(pluginRoot, "prompts", "sidecar.md");
  const destination = join(codexHome, "prompts", "sidecar.md");
  mkdirSync(dirname(destination), { recursive: true });
  copyFileSync(source, destination);
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

console.log(`Codex ChatGPT Sidecar\n\nCommands:\n  doctor                 Check local requirements\n  install-slash-alias    Install /prompts:sidecar into Codex home\n  bundle [request]       Build a repo-only ChatGPT handoff\n\nInside Codex:\n  $sidecar plan <request>       Official bundled skill trigger\n  /prompts:sidecar plan <request>  Optional slash alias\n  sidecar: plan <request>       Plain-text fallback\n\nNote: current Codex does not support plugin-defined literal /sidecar commands.`);
