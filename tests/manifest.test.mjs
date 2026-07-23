import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

function readJson(path) {
  return JSON.parse(readFileSync(new URL(`../${path}`, import.meta.url), "utf8"));
}

test("plugin and marketplace metadata match audited v0.2 layout", () => {
  const manifest = readJson(".codex-plugin/plugin.json");
  const marketplace = readJson(".agents/plugins/marketplace.json");
  const hooks = readJson("hooks/hooks.json");
  assert.equal(manifest.name, "chatgpt-sidecar");
  assert.equal(manifest.version, "0.2.0");
  assert.equal(Object.hasOwn(manifest, "hooks"), false);
  assert.ok(Array.isArray(manifest.interface.defaultPrompt));
  assert.equal(marketplace.plugins[0].name, "chatgpt-sidecar");
  const commandHook = hooks.hooks.UserPromptSubmit[0].hooks[0];
  assert.match(commandHook.command, /\$\{PLUGIN_ROOT\}/);
  assert.match(commandHook.commandWindows, /\$\{PLUGIN_ROOT\}/);
});
