import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const helperPath = join(repoRoot, "bin", "sidecar-quickchat.mjs");

function fixture() {
  const root = mkdtempSync(join(tmpdir(), "sidecar-quickchat-"));
  const codexHome = join(root, ".codex");
  const project = join(root, "project");
  const rollout = join(codexHome, "sessions", "2026", "07", "23", "rollout-fixture.jsonl");
  mkdirSync(dirname(rollout), { recursive: true });
  mkdirSync(project, { recursive: true });
  writeFileSync(join(project, "README.md"), "# Quick Chat fixture\n", "utf8");

  const records = [
    {
      timestamp: "2026-07-23T20:00:00.000Z",
      type: "session_meta",
      payload: {
        meta: {
          session_id: "session-quick-chat",
          id: "thread-quick-chat",
          cwd: project,
          timestamp: "2026-07-23T20:00:00.000Z",
          originator: "codex",
          source: "app"
        }
      }
    },
    {
      timestamp: "2026-07-23T20:00:01.000Z",
      type: "response_item",
      payload: {
        type: "message",
        role: "user",
        content: [{ type: "input_text", text: "Implement the backfill pipeline." }]
      }
    },
    {
      timestamp: "2026-07-23T20:00:02.000Z",
      type: "response_item",
      payload: {
        type: "message",
        role: "assistant",
        content: [{ type: "output_text", text: "We identified the ingestion module." }]
      }
    }
  ];
  writeFileSync(rollout, `${records.map((record) => JSON.stringify(record)).join("\n")}\n`, "utf8");
  return { root, codexHome, project };
}

function run(codexHome, args) {
  return spawnSync(process.execPath, [helperPath, ...args], {
    cwd: repoRoot,
    env: { ...process.env, CODEX_HOME: codexHome },
    encoding: "utf8"
  });
}

test("Quick Chat helper lists and prepares a saved Codex context without a Codex process", () => {
  const data = fixture();
  try {
    const listed = run(data.codexHome, ["list", "5"]);
    assert.equal(listed.status, 0, listed.stderr);
    const sessions = JSON.parse(listed.stdout);
    assert.equal(sessions.length, 1);
    assert.equal(sessions[0].sessionId, "session-quick-chat");
    assert.match(sessions[0].title, /Implement the backfill pipeline/);

    const prepared = run(data.codexHome, [
      "prepare",
      "session-quick-chat",
      "plan",
      "Design the next safe implementation step."
    ]);
    assert.equal(prepared.status, 0, prepared.stderr);
    const result = JSON.parse(prepared.stdout);
    assert.equal(result.modelTurnStarted, false);
    assert.equal(result.selectedSession.sessionId, "session-quick-chat");
    assert.equal(result.mode, "plan");

    const prompt = readFileSync(result.handoffPath, "utf8");
    assert.match(prompt, /ChatGPT Quick Chat/);
    assert.match(prompt, /Implement the backfill pipeline/);
    assert.match(prompt, /Design the next safe implementation step/);
    assert.match(prompt, /Quick Chat fixture/);
  } finally {
    rmSync(data.root, { recursive: true, force: true });
  }
});
