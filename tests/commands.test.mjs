import test from "node:test";
import assert from "node:assert/strict";
import { parseSidecarCommand } from "../src/commands.mjs";

test("ignores normal prompts", () => {
  assert.equal(parseSidecarCommand("fix the tests"), null);
});

test("parses the bundled sidecar skill", () => {
  assert.deepEqual(parseSidecarCommand("$sidecar plan auth"), {
    command: "$sidecar",
    mode: "plan",
    request: "auth"
  });
});

test("parses the expanded slash alias marker", () => {
  assert.deepEqual(parseSidecarCommand("SIDECAR_HANDOFF: debug failing login"), {
    command: "/prompts:sidecar",
    mode: "debug",
    request: "failing login"
  });
});

test("parses plain-text fallback", () => {
  assert.deepEqual(parseSidecarCommand("sidecar: review"), {
    command: "sidecar:",
    mode: "review",
    request: ""
  });
});

test("keeps literal slash alias parseable for future clients", () => {
  assert.deepEqual(parseSidecarCommand("/sidecar plan auth"), {
    command: "/sidecar",
    mode: "plan",
    request: "auth"
  });
});

test("keeps v0.2 trigger compatible", () => {
  assert.deepEqual(parseSidecarCommand("gpt-debug: failing login"), {
    command: "gpt-debug:",
    mode: "debug",
    request: "failing login"
  });
});
