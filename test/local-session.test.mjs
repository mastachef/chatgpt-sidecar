import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, rmSync, utimesSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { findLatestCodexSession, parseCodexRollout } from "../src/local-session.mjs";

function writeRollout(root, name, options = {}) {
  const dir = join(root, "sessions", "2026", "07", "23");
  mkdirSync(dir, { recursive: true });
  const path = join(dir, `${name}.jsonl`);
  const meta = {
    id: options.id || name,
    session_id: options.id || name,
    cwd: options.cwd || join(root, "repo"),
    timestamp: "2026-07-23T00:00:00Z",
    originator: "codex",
    parent_thread_id: options.parentThreadId || null,
    agent_role: options.agentRole || null
  };
  const lines = [
    { timestamp: meta.timestamp, type: "session_meta", payload: { meta } },
    {
      timestamp: meta.timestamp,
      type: "response_item",
      payload: { type: "message", role: "user", content: [{ type: "input_text", text: options.user || "hello" }] }
    },
    {
      timestamp: meta.timestamp,
      type: "response_item",
      payload: { type: "message", role: "assistant", content: [{ type: "output_text", text: options.assistant || "world" }] }
    }
  ];
  writeFileSync(path, `${lines.map((line) => JSON.stringify(line)).join("\n")}\n`, "utf8");
  const date = new Date(options.mtime || "2026-07-23T00:00:00Z");
  utimesSync(path, date, date);
  return path;
}

test("parses message text and session metadata from a saved rollout", () => {
  const root = mkdtempSync(join(tmpdir(), "sidecar-session-"));
  try {
    const path = writeRollout(root, "root", { id: "thread-1", user: "plan this", assistant: "done" });
    const parsed = parseCodexRollout(path);
    assert.equal(parsed.sessionId, "thread-1");
    assert.equal(parsed.isSubagent, false);
    assert.deepEqual(parsed.thread.messages.map(({ role, text }) => ({ role, text })), [
      { role: "user", text: "plan this" },
      { role: "assistant", text: "done" }
    ]);
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
});

test("prefers the newest root conversation over a newer subagent rollout", () => {
  const root = mkdtempSync(join(tmpdir(), "sidecar-session-"));
  try {
    writeRollout(root, "visible", { id: "visible", mtime: "2026-07-23T01:00:00Z" });
    writeRollout(root, "subagent", {
      id: "subagent",
      parentThreadId: "visible",
      agentRole: "worker",
      mtime: "2026-07-23T02:00:00Z"
    });
    assert.equal(findLatestCodexSession({ codexHome: root }).sessionId, "visible");
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
});

test("can select the newest saved root session for a specific repository", () => {
  const root = mkdtempSync(join(tmpdir(), "sidecar-session-"));
  try {
    const repoA = join(root, "repo-a");
    const repoB = join(root, "repo-b");
    writeRollout(root, "a", { id: "a", cwd: repoA, mtime: "2026-07-23T01:00:00Z" });
    writeRollout(root, "b", { id: "b", cwd: repoB, mtime: "2026-07-23T02:00:00Z" });
    assert.equal(findLatestCodexSession({ codexHome: root, cwd: repoA }).sessionId, "a");
  } finally {
    rmSync(root, { recursive: true, force: true });
  }
});
