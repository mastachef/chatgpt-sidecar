import { spawn } from "node:child_process";
import readline from "node:readline";

/**
 * Read a stored Codex thread through app-server without resuming it.
 * Returns null when Codex CLI/app-server is unavailable.
 *
 * @param {string} threadId
 * @param {{timeoutMs?: number}} [options]
 */
export async function readCodexThread(threadId, options = {}) {
  if (!threadId) return null;
  const timeoutMs = options.timeoutMs ?? 6000;

  return await new Promise((resolve) => {
    let settled = false;
    let proc;
    const finish = (value) => {
      if (settled) return;
      settled = true;
      clearTimeout(timer);
      try { proc?.kill(); } catch {}
      resolve(value);
    };

    const timer = setTimeout(() => finish(null), timeoutMs);

    try {
      proc = spawn("codex", ["app-server"], {
        stdio: ["pipe", "pipe", "ignore"],
        windowsHide: true
      });
    } catch {
      finish(null);
      return;
    }

    proc.on("error", () => finish(null));
    proc.on("exit", () => finish(null));

    const lines = readline.createInterface({ input: proc.stdout });
    lines.on("line", (line) => {
      let message;
      try { message = JSON.parse(line); } catch { return; }
      if (message?.id === 2) {
        finish(message?.result?.thread ?? null);
      }
    });

    const send = (message) => proc.stdin.write(`${JSON.stringify(message)}\n`);
    send({
      method: "initialize",
      id: 1,
      params: {
        clientInfo: {
          name: "chatgpt_sidecar",
          title: "ChatGPT Sidecar",
          version: "0.1.0"
        }
      }
    });
    send({ method: "initialized", params: {} });
    send({
      method: "thread/read",
      id: 2,
      params: { threadId, includeTurns: true }
    });
  });
}
