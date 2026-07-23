import test from "node:test";
import assert from "node:assert/strict";
import { redactSensitiveText } from "../src/utils.mjs";

test("redacts common credentials", () => {
  const input = [
    "OPENAI_API_KEY=sk-abcdefghijklmnopqrstuvwxyz",
    "Authorization: Bearer abcdefghijklmnopqrstuvwxyz",
    "https://user:secret@example.com/repo.git",
    "-----BEGIN PRIVATE KEY-----\nsecret\n-----END PRIVATE KEY-----"
  ].join("\n");
  const output = redactSensitiveText(input);
  assert.doesNotMatch(output, /abcdefghijklmnopqrstuvwxyz/);
  assert.doesNotMatch(output, /user:secret/);
  assert.doesNotMatch(output, /BEGIN PRIVATE KEY/);
  assert.match(output, /REDACTED/);
});
