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
import { listCodexHooks } from "../src/codex-app-server.mjs";
import { findLatestCodexSession } from "../src/local-session.mjs";
import { isoFileStamp } from "../src/utils.mjs";

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

function removeSidecarHookEntry({ removeRuntime = false } = {}) {
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
  if (removeRuntime) rmSync(runtimeDir, { recursive: true, force: true });
}

function installGlobalHook() {
  const codexHome = codexHomePath();
  const runtimeDir = join(codexHome, "sidecar-runtime");
  const hooksPath = join(codexHome, "hooks.json");
  mkdirSync(codexHome, { recursive: true });
  copyRuntime(runtimeDir);

  const hookScript = join(runtimeDir, "src", "hook.mjs");
  const hookCommand = `"${process.execPath}" "${hookScript}"`;
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

  if (existsSync(hooksPath)) copyFileSync(hooksPath, `${hooksPath}.backup-${Date.now()}`);
  writeFileSync(hooksPath, `${JSON.stringify(hooksConfig, null, 2)}\n`, "utf8");
  installSlashAlias(codexHome);
  console.log("Installed the legacy hook path. The external launcher is recommended instead.");
}

function parseLaunchArgs(args) {
  const values = [...args];
  const allowed = new Set(["plan", "debug", "review", "general"]);
  let mode = "general";
  if (allowed.has(String(values[0] || "").toLowerCase())) mode = values.shift().toLowerCase();
  const request = values.join(" ").trim() || "Review the current Codex session and produce the best next-step plan.";
  return { mode, request };
}

function launchSidecar(args) {
  const { mode, request } = parseLaunchArgs(args);
  const codexHome = codexHomePath();
  const session = findLatestCodexSession({ codexHome });
  if (!session) {
    throw new Error(`No saved Codex session was found under ${join(codexHome, "sessions")}. Open a Codex project and send at least one normal message first.`);
  }

  const sessionCwd = session.cwd && existsSync(session.cwd) ? session.cwd : process.cwd();
  const repo = collectRepoContext(sessionCwd);
  const handoff = createHandoff({
    mode,
    request,
    hookInput: {
      cwd: session.cwd || sessionCwd,
      session_id: session.sessionId,
      turn_id: null,
      model: null
    },
    repo,
    thread: session.thread
  });

  const handoffDir = join(repo.root, ".sidecar", "handoffs");
  mkdirSync(handoffDir, { recursive: true });
  const handoffPath = join(handoffDir, `${isoFileStamp()}-${mode}.md`);
  writeFileSync(handoffPath, handoff, "utf8");

  const copied = copyToClipboard(handoff);
  const opened = openUrl("https://chatgpt.com/");
  console.log(`Sidecar read the saved Codex session directly: ${session.path}`);
  console.log(`Saved handoff: ${handoffPath}`);
  console.log(copied ? "Copied handoff to clipboard." : "Clipboard copy failed; open the saved Markdown file instead.");
  console.log(opened ? "Opened ChatGPT." : "Could not open ChatGPT automatically.");
  console.log("No Codex prompt, thread, or model turn was started.");
}

function psQuote(value) {
  return `'${String(value).replaceAll("'", "''")}'`;
}

function installExternalLauncher() {
  const codexHome = codexHomePath();
  const runtimeDir = join(codexHome, "sidecar-runtime");
  const launcherDir = join(codexHome, "sidecar-bin");
  const runtimeScript = join(runtimeDir, "bin", "sidecar.mjs");
  const cmdPath = join(launcherDir, "sidecar.cmd");
  const ps1Path = join(launcherDir, "sidecar-launch.ps1");
  const aliasPath = join(codexHome, "prompts", "sidecar.md");

  mkdirSync(launcherDir, { recursive: true });
  copyRuntime(runtimeDir);
  removeSidecarHookEntry({ removeRuntime: false });
  rmSync(aliasPath, { force: true });

  writeFileSync(
    cmdPath,
    `@echo off\r\n"${process.execPath}" "${runtimeScript}" launch %*\r\n`,
    "utf8"
  );

  writeFileSync(
    ps1Path,
    [
      "$ErrorActionPreference = 'Stop'",
      "$mode = Read-Host 'Mode [plan/debug/review/general] (default: plan)'",
      "if ([string]::IsNullOrWhiteSpace($mode)) { $mode = 'plan' }",
      "$request = Read-Host 'What should ChatGPT Sidecar do?'",
      "if ([string]::IsNullOrWhiteSpace($request)) { Write-Host 'Cancelled.'; exit 1 }",
      `& ${psQuote(process.execPath)} ${psQuote(runtimeScript)} launch $mode $request`,
      "if ($LASTEXITCODE -ne 0) { Read-Host 'Press Enter to close' | Out-Null }"
    ].join("\r\n") + "\r\n",
    "utf8"
  );

  let shortcutInstalled = false;
  try {
    const shortcutArgs = `-NoProfile -ExecutionPolicy Bypass -File "${ps1Path}"`;
    const script = [
      "$shell = New-Object -ComObject WScript.Shell",
      "$shortcutPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'ChatGPT Sidecar.lnk'",
      "$shortcut = $shell.CreateShortcut($shortcutPath)",
      `$shortcut.TargetPath = ${psQuote("powershell.exe")}`,
      `$shortcut.Arguments = ${psQuote(shortcutArgs)}`,
      `$shortcut.WorkingDirectory = ${psQuote(runtimeDir)}`,
      `$shortcut.Description = ${psQuote("Open ChatGPT Sidecar without submitting a Codex turn")}`,
      `$shortcut.Hotkey = ${psQuote("CTRL+ALT+S")}`,
      "$shortcut.Save()",
      "$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')",
      "if ([string]::IsNullOrWhiteSpace($userPath)) { $userPath = '' }",
      `$launcherDir = ${psQuote(launcherDir)}`,
      "if (($userPath -split ';') -notcontains $launcherDir) { [Environment]::SetEnvironmentVariable('Path', (($userPath.TrimEnd(';') + ';' + $launcherDir).Trim(';')), 'User') }"
    ].join("; ");
    execFileSync("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], {
      stdio: "ignore",
      windowsHide: true
    });
    shortcutInstalled = true;
  } catch {
    shortcutInstalled = false;
  }

  console.log(`Installed hook-free Sidecar runtime at ${runtimeDir}`);
  console.log(`Installed command launcher at ${cmdPath}`);
  console.log("Removed the Sidecar global hook and prompt alias so accidental Codex submissions cannot trigger it.");
  console.log(shortcutInstalled
    ? "Installed Desktop shortcut 'ChatGPT Sidecar' with hotkey Ctrl+Alt+S."
    : `Could not create the Desktop shortcut automatically. Run: powershell -File "${ps1Path}"`);
  console.log("Open a new PowerShell window to use: sidecar plan \"your request\"");
  console.log("This launcher reads saved session files directly and never submits a Codex turn.");
}

if (command === "launch") {
  try {
    launchSidecar(process.argv.slice(3));
    process.exit(0);
  } catch (error) {
    console.error(`Sidecar launch failed: ${error instanceof Error ? error.message : String(error)}`);
    process.exit(1);
  }
}

if (command === "install-launcher") {
  installExternalLauncher();
  process.exit(0);
}

if (command === "doctor") {
  const major = Number.parseInt(process.versions.node.split(".")[0], 10);
  const codexHome = codexHomePath();
  const hooksPath = join(codexHome, "hooks.json");
  const sessionsPath = join(codexHome, "sessions");
  let globalHookInstalled = false;
  try {
    globalHookInstalled = existsSync(hooksPath) && isSidecarHookGroup(readHooksFile(hooksPath));
  } catch {
    globalHookInstalled = false;
  }
  const report = {
    node: process.version,
    nodeSupported: major >= 20,
    gitAvailable: commandExists("git"),
    platform: process.platform,
    pluginRoot,
    codexHome,
    savedSessionsAvailable: existsSync(sessionsPath),
    externalLauncherInstalled: existsSync(join(codexHome, "sidecar-bin", "sidecar.cmd")),
    legacyGlobalHookInstalled: globalHookInstalled,
    recommendedCommand: "sidecar plan \"your request\"",
    modelTurnRequired: false
  };
  console.log(JSON.stringify(report, null, 2));
  process.exit(report.nodeSupported && report.gitAvailable && report.savedSessionsAvailable ? 0 : 1);
}

if (command === "verify-hook") {
  const cwd = resolve(process.argv[3] || process.cwd());
  const discovered = await listCodexHooks(cwd);
  if (!discovered) {
    console.error("Could not query Codex App Server. Use `install-launcher`; the external launcher does not need App Server or hooks.");
    process.exit(2);
  }
  const allHooks = discovered.flatMap((entry) => Array.isArray(entry?.hooks) ? entry.hooks : []);
  const sidecarHooks = allHooks.filter((hook) => isSidecarHookGroup(hook));
  console.log(JSON.stringify({ cwd, sidecarHooks, modelTurnStarted: false }, null, 2));
  process.exit(sidecarHooks.length ? 0 : 1);
}

if (command === "install-global-hook") {
  installGlobalHook();
  process.exit(0);
}

if (command === "uninstall-global-hook") {
  removeSidecarHookEntry({ removeRuntime: true });
  console.log("Removed the Sidecar global hook and runtime. Existing non-Sidecar hooks were preserved.");
  process.exit(0);
}

if (command === "install-slash-alias") {
  const destination = installSlashAlias(codexHomePath());
  console.log(`Installed Sidecar slash alias at ${destination}`);
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

console.log(`ChatGPT Sidecar\n\nRecommended hook-free setup:\n  node ./bin/sidecar.mjs install-launcher\n\nThen use either:\n  Ctrl+Alt+S\n  sidecar plan "your request"\n\nCommands:\n  launch [plan|debug|review|general] [request]\n  install-launcher       Install the external Windows launcher and remove Sidecar hooks\n  doctor                 Verify saved sessions and launcher status\n  bundle [request]       Build a repo-only handoff\n\nLegacy compatibility commands:\n  verify-hook [cwd]\n  install-global-hook\n  uninstall-global-hook\n  install-slash-alias\n\nThe external launcher reads ~/.codex/sessions directly and never submits a Codex model turn.`);