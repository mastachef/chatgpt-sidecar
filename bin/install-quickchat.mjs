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

const sourceRoot = resolve(fileURLToPath(new URL("..", import.meta.url)));
const codexHome = process.env.CODEX_HOME || join(homedir(), ".codex");
const runtimeDir = join(codexHome, "sidecar-runtime");
const launcherDir = join(codexHome, "sidecar-bin");
const runtimeHelper = join(runtimeDir, "bin", "sidecar-quickchat.mjs");
const quickChatScript = join(runtimeDir, "windows", "sidecar-quickchat.ps1");
const hotkeyScript = join(runtimeDir, "windows", "sidecar-hotkey.ps1");
const cmdPath = join(launcherDir, "sidecar-quickchat.cmd");
const hooksPath = join(codexHome, "hooks.json");
const legacyAliasPath = join(codexHome, "prompts", "sidecar.md");

function psQuote(value) {
  return `'${String(value).replaceAll("'", "''")}'`;
}

function copyRuntime() {
  if (resolve(sourceRoot) === resolve(runtimeDir)) return;
  rmSync(runtimeDir, { recursive: true, force: true });
  const skipped = new Set([".git", "node_modules", ".sidecar", "coverage"]);
  cpSync(sourceRoot, runtimeDir, {
    recursive: true,
    force: true,
    filter(source) {
      return !skipped.has(basename(source));
    }
  });
}

function removeLegacyHookAndAlias() {
  if (existsSync(hooksPath)) {
    try {
      const config = JSON.parse(readFileSync(hooksPath, "utf8"));
      if (config?.hooks && Array.isArray(config.hooks.UserPromptSubmit)) {
        config.hooks.UserPromptSubmit = config.hooks.UserPromptSubmit.filter((group) => {
          const serialized = JSON.stringify(group || {});
          return !serialized.includes("sidecar-runtime")
            && !serialized.includes("Preparing ChatGPT sidecar handoff");
        });
        copyFileSync(hooksPath, `${hooksPath}.backup-${Date.now()}`);
        writeFileSync(hooksPath, `${JSON.stringify(config, null, 2)}\n`, "utf8");
      }
    } catch {
      // Do not damage a hook file with syntax we do not understand.
    }
  }
  rmSync(legacyAliasPath, { force: true });
}

function installShortcutsAndListener() {
  const quickChatArgs = [
    "-NoProfile",
    "-STA",
    "-WindowStyle Hidden",
    "-ExecutionPolicy Bypass",
    `-File \"${quickChatScript}\"`,
    `-NodePath \"${process.execPath}\"`,
    `-RuntimeScript \"${runtimeHelper}\"`
  ].join(" ");

  const listenerArgs = [
    "-NoProfile",
    "-WindowStyle Hidden",
    "-ExecutionPolicy Bypass",
    `-File \"${hotkeyScript}\"`,
    `-NodePath \"${process.execPath}\"`,
    `-RuntimeScript \"${runtimeHelper}\"`,
    `-QuickChatScript \"${quickChatScript}\"`
  ].join(" ");

  const script = [
    "$ErrorActionPreference = 'Stop'",
    "$shell = New-Object -ComObject WScript.Shell",
    `$quickChatArgs = ${psQuote(quickChatArgs)}`,
    `$listenerArgs = ${psQuote(listenerArgs)}`,
    "$desktopPath = Join-Path ([Environment]::GetFolderPath('Desktop')) 'ChatGPT Sidecar.lnk'",
    "$shortcut = $shell.CreateShortcut($desktopPath)",
    `$shortcut.TargetPath = ${psQuote("powershell.exe")}`,
    "$shortcut.Arguments = $quickChatArgs",
    `$shortcut.WorkingDirectory = ${psQuote(runtimeDir)}`,
    `$shortcut.Description = ${psQuote("Send the active Codex context to ChatGPT Quick Chat")}`,
    "$shortcut.Hotkey = ''",
    "$shortcut.Save()",
    "$programs = [Environment]::GetFolderPath('Programs')",
    "$startMenuPath = Join-Path $programs 'ChatGPT Sidecar.lnk'",
    "$startShortcut = $shell.CreateShortcut($startMenuPath)",
    `$startShortcut.TargetPath = ${psQuote("powershell.exe")}`,
    "$startShortcut.Arguments = $quickChatArgs",
    `$startShortcut.WorkingDirectory = ${psQuote(runtimeDir)}`,
    `$startShortcut.Description = ${psQuote("Send the active Codex context to ChatGPT Quick Chat")}`,
    "$startShortcut.Save()",
    "$startup = [Environment]::GetFolderPath('Startup')",
    "$startupPath = Join-Path $startup 'ChatGPT Sidecar Hotkey.lnk'",
    "$startupShortcut = $shell.CreateShortcut($startupPath)",
    `$startupShortcut.TargetPath = ${psQuote("powershell.exe")}`,
    "$startupShortcut.Arguments = $listenerArgs",
    `$startupShortcut.WorkingDirectory = ${psQuote(runtimeDir)}`,
    `$startupShortcut.Description = ${psQuote("ChatGPT Sidecar Ctrl+Alt+S listener")}`,
    "$startupShortcut.Save()",
    "$userPath = [Environment]::GetEnvironmentVariable('Path', 'User')",
    "if ([string]::IsNullOrWhiteSpace($userPath)) { $userPath = '' }",
    `$launcherDir = ${psQuote(launcherDir)}`,
    "if (($userPath -split ';') -notcontains $launcherDir) { [Environment]::SetEnvironmentVariable('Path', (($userPath.TrimEnd(';') + ';' + $launcherDir).Trim(';')), 'User') }",
    "Get-CimInstance Win32_Process -Filter \"Name='powershell.exe' OR Name='pwsh.exe'\" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like '*sidecar-hotkey.ps1*' -and $_.ProcessId -ne $PID } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }",
    "Start-Sleep -Milliseconds 250",
    "Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -ArgumentList $listenerArgs | Out-Null"
  ].join("; ");

  execFileSync("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], {
    stdio: "inherit",
    windowsHide: true
  });
}

if (process.platform !== "win32") {
  console.error("The one-hotkey Quick Chat automation installer currently supports Windows only.");
  process.exit(1);
}

const nodeMajor = Number.parseInt(process.versions.node.split(".")[0], 10);
if (nodeMajor < 20) {
  console.error(`Node.js 20 or newer is required; found ${process.version}.`);
  process.exit(1);
}

mkdirSync(codexHome, { recursive: true });
mkdirSync(launcherDir, { recursive: true });
copyRuntime();
removeLegacyHookAndAlias();

writeFileSync(
  cmdPath,
  `@echo off\r\npowershell.exe -NoProfile -STA -WindowStyle Hidden -ExecutionPolicy Bypass -File \"${quickChatScript}\" -NodePath \"${process.execPath}\" -RuntimeScript \"${runtimeHelper}\"\r\n`,
  "utf8"
);

installShortcutsAndListener();

console.log(`Installed Sidecar Quick Chat runtime at ${runtimeDir}`);
console.log(`Installed command launcher at ${cmdPath}`);
console.log("Installed Desktop and Start Menu shortcuts named 'ChatGPT Sidecar'.");
console.log("Installed and started a real Ctrl+Alt+S global hotkey listener.");
console.log("The hotkey listener will start automatically when you sign in to Windows.");
console.log("Removed legacy Sidecar hooks and prompt aliases.");
console.log("Normal workflow: stay in Codex, press Ctrl+Alt+S, enter the request, and Sidecar opens and submits a real ChatGPT Quick Chat.");
console.log("No message is submitted to the Codex thread.");
