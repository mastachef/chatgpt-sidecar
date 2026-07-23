#!/usr/bin/env node
import { resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { execFileSync } from "node:child_process";
import { collectRepoContext } from "../src/repo-context.mjs";
import { createHandoff } from "../src/handoff.mjs";
import { copyToClipboard, openUrl } from "../src/platform.mjs";

const command = process.argv[2] || "help";

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
  const report = {
    node: process.version,
    nodeSupported: major >= 20,
    codexAvailable: commandExists(process.env.CODEX_BIN || "codex"),
    gitAvailable: commandExists("git"),
    platform: process.platform,
    pluginRoot: resolve(fileURLToPath(new URL("..", import.meta.url))),
    trigger: "gpt: <request>"
  };
  console.log(JSON.stringify(report, null, 2));
  process.exit(report.nodeSupported && report.codexAvailable && report.gitAvailable ? 0 : 1);
}

if (command === "bundle") {
  const cwd = process.cwd();
  const request = process.argv.slice(3).join(" ") || "Review this project and produce an implementation-ready next-step plan.";
  const repo = collectRepoContext(cwd);
  const handoff = createHandoff({ mode: "general", request, hookInput: { cwd, session_id: null, turn_id: null, model: null }, repo, thread: null });
  copyToClipboard(handoff);
  openUrl("https://chatgpt.com/");
  console.log(handoff);
  process.exit(0);
}

console.log(`Codex ChatGPT Sidecar\n\nCommands:\n  doctor                 Check local requirements\n  bundle [request]       Build a repo-only ChatGPT handoff\n\nInside Codex (normal composer text, not a slash command):\n  gpt: <request>\n  gpt-plan: <request>\n  gpt-debug: <problem>\n  gpt-review:`);
