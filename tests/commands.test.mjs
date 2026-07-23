import test from "node:test";
import assert from "node:assert/strict";
import { parseSidecarCommand } from "../src/commands.mjs";

test("ignores normal prompts", () => {
  assert.equal(parseSidecarCommand("fix the tests"), null);
});

test("parses generic handoff", () => {
  assert.deepEqual(parseSidecarCommand("/gpt plan auth"), {
    command: "/gpt",
    mode: "general",
    request: "plan auth"
  });
});

test("parses mode aliases case-insensitively", () => {
  assert.deepEqual(parseSidecarCommand("  /GPT-DEBUG failing login  "), {
    command: "/gpt-debug",
    mode: "debug",
    request: "failing login"
  });
});
