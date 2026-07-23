import test from "node:test";
import assert from "node:assert/strict";
import { EventEmitter } from "node:events";
import { PassThrough, Writable } from "node:stream";
import { readCodexThread } from "../src/codex-app-server.mjs";

function createFakeSpawn({ initializationError = false } = {}) {
  const writes = [];
  const proc = new EventEmitter();
  proc.stdout = new PassThrough();
  proc.stdin = new Writable({
    write(chunk, _encoding, callback) {
      const messages = String(chunk).trim().split("\n").filter(Boolean).map(JSON.parse);
      for (const message of messages) {
        writes.push(message);
        if (message.id === 1) queueMicrotask(() => proc.stdout.write(`${JSON.stringify(initializationError ? { id: 1, error: { message: "bad init" } } : { id: 1, result: { userAgent: "test" } })}\n`));
        if (message.id === 2) queueMicrotask(() => proc.stdout.write(`${JSON.stringify({ id: 2, result: { thread: { id: "thr_1", turns: [] } } })}\n`));
      }
      callback();
    }
  });
  proc.kill = () => {};
  return { spawnImpl: () => proc, writes };
}

test("waits for initialize response before thread/read", async () => {
  const fake = createFakeSpawn();
  const thread = await readCodexThread("thr_1", { spawnImpl: fake.spawnImpl, timeoutMs: 1000 });
  assert.deepEqual(thread, { id: "thr_1", turns: [] });
  assert.deepEqual(fake.writes.map(({ method }) => method), ["initialize", "initialized", "thread/read"]);
});

test("returns null when initialization fails", async () => {
  const fake = createFakeSpawn({ initializationError: true });
  assert.equal(await readCodexThread("thr_1", { spawnImpl: fake.spawnImpl, timeoutMs: 1000 }), null);
  assert.deepEqual(fake.writes.map(({ method }) => method), ["initialize"]);
});
