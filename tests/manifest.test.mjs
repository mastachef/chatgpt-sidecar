import test from "node:test";
import assert from "node:assert/strict";
import { existsSync, readFileSync } from "node:fs";

function readJson(path) {
  return JSON.parse(readFileSync(new URL(`../${path}`, import.meta.url), "utf8"));
}

test("plugin and marketplace metadata match the native Sidecar Dock layout", () => {
  const manifest = readJson(".codex-plugin/plugin.json");
  const marketplace = readJson(".agents/plugins/marketplace.json");
  const hooks = readJson("hooks/hooks.json");
  const packageJson = readJson("package.json");

  assert.equal(manifest.name, "chatgpt-sidecar");
  assert.equal(manifest.version, packageJson.version);
  assert.match(manifest.version, /^0\.8\.0-alpha\./);
  assert.equal(Object.hasOwn(manifest, "skills"), false);
  assert.equal(Object.hasOwn(manifest, "mcpServers"), false);
  assert.equal(Object.hasOwn(manifest, "hooks"), false);
  assert.equal(manifest.interface.displayName, "ChatGPT Sidecar Dock");
  assert.ok(Array.isArray(manifest.interface.defaultPrompt));
  assert.ok(manifest.interface.defaultPrompt.length > 0);

  assert.equal(existsSync(new URL("../apps/Sidecar.Dock/Sidecar.Dock.csproj", import.meta.url)), true);
  assert.equal(existsSync(new URL("../apps/Sidecar.Dock/MainWindow.xaml", import.meta.url)), true);

  assert.equal(marketplace.plugins[0].name, "chatgpt-sidecar");
  assert.deepEqual(marketplace.plugins[0].source, { source: "local", path: "./" });
  assert.equal(marketplace.plugins[0].category, "Developer Tools");

  assert.deepEqual(hooks.hooks, {});
});
