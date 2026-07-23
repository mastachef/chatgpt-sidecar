#!/usr/bin/env node
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { parseSidecarCommand } from "./commands.mjs";
import { readCodexThread } from "./codex-app-server.mjs";
import { collectRepoContext } from "./repo-context.mjs";
import { createHandoff } from "./handoff.mjs";
import { copyToClipboard, openUrl } from "./platform.mjs";
import { isoFileStamp } from "./utils.mjs";

async function readStdin() {
  let data = "";
  for await (const chunk of process.stdin) data += chunk;
  return data;
}

function emit(value) {
  process.stdout.write(`${JSON.stringify(value)}\n`);
}

try {
  const raw = await readStdin();
  const hookInput = JSON.parse(raw || "{}");
  const parsed = parseSidecarCommand(hookInput.prompt);

  // UserPromptSubmit fires for every prompt because this event does not support matchers.
  if (!parsed) process.exit(0);

  const [thread, repo] = await Promise.all([
    readCodexThread(hookInput.session_id),
    Promise.resolve(collectRepoContext(hookInput.cwd || process.cwd()))
  ]);

  const handoff = createHandoff({
    mode: parsed.mode,
    request: parsed.request,
    hookInput,
    repo,
    thread
  });

  const dataRoot = process.env.PLUGIN_DATA || join(repo.root, ".sidecar");
  const handoffDir = join(dataRoot, "handoffs");
  mkdirSync(handoffDir, { recursive: true });
  const handoffPath = join(handoffDir, `${isoFileStamp()}-${parsed.mode}.md`);
  writeFileSync(handoffPath, handoff, "utf8");

  const copied = copyToClipboard(handoff);
  const opened = openUrl("https://chatgpt.com/");
  const actions = [
    `Saved handoff to ${handoffPath}`,
    copied ? "copied it to the clipboard" : "clipboard copy was unavailable",
    opened ? "opened ChatGPT" : "could not open ChatGPT automatically"
  ].join(", ");

  emit({
    decision: "block",
    reason: `Sidecar intercepted ${parsed.command}: ${actions}. Paste the handoff into ChatGPT; Codex did not run this prompt.`
  });
} catch (error) {
  emit({
    decision: "block",
    reason: `Sidecar command failed before reaching Codex: ${error instanceof Error ? error.message : String(error)}`
  });
}
