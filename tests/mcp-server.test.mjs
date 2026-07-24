import test from "node:test";
import assert from "node:assert/strict";
import { mkdtempSync, mkdirSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawn } from "node:child_process";
import readline from "node:readline";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const serverPath = join(repoRoot, "mcp", "server.mjs");

function createFixture() {
  const root = mkdtempSync(join(tmpdir(), "chatgpt-sidecar-mcp-"));
  const codexHome = join(root, ".codex");
  const project = join(root, "project");
  const sessionPath = join(codexHome, "sessions", "2026", "07", "23", "rollout-test.jsonl");
  mkdirSync(dirname(sessionPath), { recursive: true });
  mkdirSync(project, { recursive: true });
  writeFileSync(join(project, "README.md"), "# Fixture project\n", "utf8");

  const lines = [
    {
      timestamp: "2026-07-23T20:00:00.000Z",
      type: "session_meta",
      payload: {
        meta: {
          session_id: "session-fixture",
          id: "thread-fixture",
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
        content: [{ type: "input_text", text: "Implement historical backfilling." }]
      }
    },
    {
      timestamp: "2026-07-23T20:00:02.000Z",
      type: "response_item",
      payload: {
        type: "message",
        role: "assistant",
        content: [{ type: "output_text", text: "We should inspect the ingestion pipeline." }]
      }
    }
  ];
  writeFileSync(sessionPath, `${lines.map((line) => JSON.stringify(line)).join("\n")}\n`, "utf8");
  return { root, codexHome, project };
}

function startServer(codexHome) {
  const proc = spawn(process.execPath, [serverPath, "--stdio"], {
    cwd: repoRoot,
    env: { ...process.env, CODEX_HOME: codexHome },
    stdio: ["pipe", "pipe", "pipe"]
  });
  const pending = new Map();
  const lines = readline.createInterface({ input: proc.stdout });
  lines.on("line", (line) => {
    const message = JSON.parse(line);
    const resolvePending = pending.get(message.id);
    if (resolvePending) {
      pending.delete(message.id);
      resolvePending(message);
    }
  });

  function request(id, method, params = {}) {
    return new Promise((resolveRequest, reject) => {
      const timer = setTimeout(() => {
        pending.delete(id);
        reject(new Error(`Timed out waiting for ${method}`));
      }, 4000);
      pending.set(id, (message) => {
        clearTimeout(timer);
        resolveRequest(message);
      });
      proc.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
    });
  }

  return { proc, request };
}

test("MCP server returns active saved Codex and repository context without a Codex process", async () => {
  const fixture = createFixture();
  const { proc, request } = startServer(fixture.codexHome);
  try {
    const initialized = await request(1, "initialize", { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "test", version: "1" } });
    assert.equal(initialized.result.serverInfo.name, "chatgpt-sidecar");

    const listed = await request(2, "tools/list");
    assert.deepEqual(
      listed.result.tools.map((tool) => tool.name),
      [
        "sidecar_get_active_context",
        "sidecar_list_recent_threads",
        "sidecar_get_thread",
        "sidecar_get_repo_context"
      ]
    );

    const context = await request(3, "tools/call", {
      name: "sidecar_get_active_context",
      arguments: {
        mode: "plan",
        request: "Design the next implementation step."
      }
    });

    assert.equal(context.result.isError, undefined);
    assert.equal(context.result.structuredContent.selectedSession.sessionId, "session-fixture");
    assert.equal(context.result.structuredContent.modelTurnStarted, false);
    assert.match(context.result.content[0].text, /Implement historical backfilling/);
    assert.match(context.result.content[0].text, /Design the next implementation step/);
    assert.match(context.result.content[0].text, /Fixture project/);
  } finally {
    proc.kill();
    rmSync(fixture.root, { recursive: true, force: true });
  }
});
