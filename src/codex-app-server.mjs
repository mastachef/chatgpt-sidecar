import { spawn } from "node:child_process";
import readline from "node:readline";

/**
 * Read a stored Codex thread through app-server without resuming it.
 * Returns null when Codex CLI/app-server is unavailable or the handshake fails.
 *
 * @param {string} threadId
 * @param {{timeoutMs?: number, spawnImpl?: typeof spawn, command?: string}} [options]
 */
export async function readCodexThread(threadId, options = {}) {
  if (!threadId) return null;
  const timeoutMs = options.timeoutMs ?? 10000;
  const spawnImpl = options.spawnImpl ?? spawn;
  const command = options.command ?? process.env.CODEX_BIN ?? "codex";

  return await new Promise((resolve) => {
    let settled = false;
    let proc;
    let timer;

    const finish = (value) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      try { proc?.stdin?.end(); } catch {}
      try { proc?.kill(); } catch {}
      resolve(value);
    };

    timer = setTimeout(() => finish(null), timeoutMs);

    try {
      proc = spawnImpl(command, ["app-server"], {
        stdio: ["pipe", "pipe", "ignore"],
        windowsHide: true
      });
    } catch {
      finish(null);
      return;
    }

    proc.on("error", () => finish(null));
    proc.on("exit", () => finish(null));
    proc.stdin?.on("error", () => finish(null));

    const send = (message) => {
      if (!proc?.stdin?.writable) return false;
      proc.stdin.write(`${JSON.stringify(message)}\n`);
      return true;
    };

    const lines = readline.createInterface({ input: proc.stdout });
    lines.on("line", (line) => {
      let message;
      try { message = JSON.parse(line); } catch { return; }

      if (message?.id === 1) {
        if (message.error) {
          finish(null);
          return;
        }

        send({ method: "initialized", params: {} });
        send({
          method: "thread/read",
          id: 2,
          params: { threadId, includeTurns: true }
        });
        return;
      }

      if (message?.id === 2) {
        finish(message?.error ? null : message?.result?.thread ?? null);
      }
    });

    send({
      method: "initialize",
      id: 1,
      params: {
        clientInfo: {
          name: "codex_chatgpt_sidecar",
          title: "Codex ChatGPT Sidecar",
          version: "0.2.0"
        }
      }
    });
  });
}
