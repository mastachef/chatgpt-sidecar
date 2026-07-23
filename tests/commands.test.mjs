import test from "node:test";
import assert from "node:assert/strict";
import { parseSidecarCommand } from "../src/commands.mjs";

test("ignores normal prompts", () => {
  assert.equal(parseSidecarCommand("fix the tests"), null);
});

test("parses supported normal-text handoff trigger", () => {
  assert.deepEqual(parseSidecarCommand("gpt: plan auth"), { command: "gpt:", mode: "general", request: "plan auth" });
});

test("parses mode triggers case-insensitively", () => {
  assert.deepEqual(parseSidecarCommand("  GPT-DEBUG: failing login  "), { command: "gpt-debug:", mode: "debug", request: "failing login" });
});

test("parses an empty review request", () => {
  assert.deepEqual(parseSidecarCommand("gpt-review:"), { command: "gpt-review:", mode: "review", request: "" });
});

test("keeps legacy slash aliases parseable", () => {
  assert.deepEqual(parseSidecarCommand("/gpt-plan auth"), { command: "/gpt-plan", mode: "plan", request: "auth" });
});
