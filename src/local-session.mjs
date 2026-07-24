import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { join, resolve } from "node:path";

function normalizedPath(value) {
  const resolved = resolve(String(value || "."));
  return process.platform === "win32" ? resolved.toLowerCase() : resolved;
}

function collectJsonlFiles(root, output = []) {
  if (!existsSync(root)) return output;
  for (const entry of readdirSync(root, { withFileTypes: true })) {
    const absolute = join(root, entry.name);
    if (entry.isDirectory()) collectJsonlFiles(absolute, output);
    else if (entry.isFile() && entry.name.toLowerCase().endsWith(".jsonl")) output.push(absolute);
  }
  return output;
}

function textFromContent(content) {
  if (typeof content === "string") return content.trim();
  if (!Array.isArray(content)) return "";
  return content
    .map((part) => {
      if (typeof part === "string") return part;
      if (!part || typeof part !== "object") return "";
      if (typeof part.text === "string") return part.text;
      if (typeof part.content === "string") return part.content;
      return "";
    })
    .filter(Boolean)
    .join("\n")
    .trim();
}

function addMessage(messages, role, text, timestamp) {
  const clean = String(text || "").trim();
  if (!clean) return;
  const previous = messages.at(-1);
  if (previous?.role === role && previous?.text === clean) return;
  messages.push({ role, text: clean, timestamp: timestamp || null });
}

export function parseCodexRollout(path) {
  const raw = readFileSync(path, "utf8");
  const messages = [];
  let meta = {};
  let git = null;

  for (const line of raw.split(/\r?\n/)) {
    if (!line.trim()) continue;
    let record;
    try {
      record = JSON.parse(line);
    } catch {
      continue;
    }

    if (record?.type === "session_meta") {
      const payload = record.payload || {};
      meta = payload.meta || payload;
      git = payload.git || null;
      continue;
    }

    if (record?.type === "response_item") {
      const payload = record.payload || {};
      if (payload.type === "message" && typeof payload.role === "string") {
        addMessage(messages, payload.role, textFromContent(payload.content), record.timestamp);
      }
      continue;
    }

    if (record?.type === "event_msg") {
      const payload = record.payload || {};
      if (payload.type === "user_message") {
        addMessage(messages, "user", payload.message, record.timestamp);
      } else if (payload.type === "agent_message") {
        addMessage(messages, "assistant", payload.message, record.timestamp);
      }
    }
  }

  const stats = statSync(path);
  const sessionId = meta.session_id || meta.id || null;
  return {
    path,
    sessionId,
    cwd: meta.cwd || null,
    source: meta.source || null,
    originator: meta.originator || null,
    startedAt: meta.timestamp || null,
    updatedAt: stats.mtime.toISOString(),
    git,
    thread: {
      id: meta.id || sessionId,
      sessionId,
      cwd: meta.cwd || null,
      source: meta.source || null,
      originator: meta.originator || null,
      startedAt: meta.timestamp || null,
      updatedAt: stats.mtime.toISOString(),
      rolloutPath: path,
      messages: messages.slice(-160)
    }
  };
}

/**
 * Read the newest saved Codex rollout directly from CODEX_HOME without starting
 * Codex App Server, a thread, or a model turn.
 */
export function findLatestCodexSession(options = {}) {
  const codexHome = options.codexHome;
  if (!codexHome) throw new Error("codexHome is required");
  const sessionsRoot = join(codexHome, "sessions");
  const requestedCwd = options.cwd ? normalizedPath(options.cwd) : null;

  const candidates = collectJsonlFiles(sessionsRoot)
    .map((path) => {
      try {
        return { path, mtimeMs: statSync(path).mtimeMs };
      } catch {
        return null;
      }
    })
    .filter(Boolean)
    .sort((a, b) => b.mtimeMs - a.mtimeMs)
    .slice(0, options.maxCandidates || 250);

  let newest = null;
  for (const candidate of candidates) {
    let parsed;
    try {
      parsed = parseCodexRollout(candidate.path);
    } catch {
      continue;
    }
    if (!parsed.thread.messages.length) continue;
    if (!newest) newest = parsed;
    if (!requestedCwd) return parsed;
    if (parsed.cwd && normalizedPath(parsed.cwd) === requestedCwd) return parsed;
  }
  return newest;
}
