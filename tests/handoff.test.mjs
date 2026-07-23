import test from "node:test";
import assert from "node:assert/strict";
import { createHandoff } from "../src/handoff.mjs";

test("handoff includes request, repo and execution packet", () => {
  const output = createHandoff({
    mode: "plan",
    request: "Add authentication",
    hookInput: { session_id: "thr_1", turn_id: "turn_1", cwd: "/repo", model: "test" },
    repo: {
      root: "/repo",
      branch: "main",
      head: "abc",
      remote: "origin",
      status: "clean",
      diffStat: "",
      diff: "",
      stagedDiff: "",
      recentCommits: "abc initial",
      trackedFiles: "src/index.js",
      keyFiles: { "README.md": "hello" }
    },
    thread: { id: "thr_1", turns: [] }
  });
  assert.match(output, /Add authentication/);
  assert.match(output, /CODEX EXECUTION PACKET/);
  assert.match(output, /src\/index\.js/);
  assert.match(output, /thr_1/);
});
