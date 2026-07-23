import test from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";

function readJson(path) {
  return JSON.parse(readFileSync(new URL(`../${path}`, import.meta.url), "utf8"));
}

test("plugin and marketplace metadata match the Sidecar v0.3 layout", () => {
  const manifest = readJson(".codex-plugin/plugin.json");
  const marketplace = readJson(".agents/plugins/marketplace.json");
  const hooks = readJson("hooks/hooks.json");

  assert.equal(manifest.name, "chatgpt-sidecar");
  assert.equal(manifest.version, "0.3.0");
  assert.equal(manifest.skills, "./skills/");
  assert.equal(Object.hasOwn(manifest, "hooks"), false);
  assert.ok(Array.isArray(manifest.interface.defaultPrompt));
  assert.ok(manifest.interface.defaultPrompt.every((value) => value.startsWith("$sidecar")));
  assert.equal(existsSync(new URL("../skills/sidecar/SKILL.md", import.meta.url)), true);
  assert.equal(existsSync(new URL("../prompts/sidecar.md", import.meta.url)), true);

  assert.equal(marketplace.plugins[0].name, "chatgpt-sidecar");
  assert.deepEqual(marketplace.plugins[0].source, { source: "local", path: "./" });

  const commandHook = hooks.hooks.UserPromptSubmit[0].hooks[0];
  assert.equal(commandHook.type, "command");
  assert.match(commandHook.command, /\$\{PLUGIN_ROOT\}/);
  assert.match(commandHook.commandWindows, /\$\{PLUGIN_ROOT\}/);
});
