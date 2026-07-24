#!/usr/bin/env node
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { homedir } from "node:os";
import { join, resolve } from "node:path";
import { collectRepoContext } from "../src/repo-context.mjs";
import { createHandoff } from "../src/handoff.mjs";
import { copyToClipboard } from "../src/platform.mjs";
import { findCodexSession, listRecentCodexSessions } from "../src/local-session.mjs";
import { isoFileStamp, truncateMiddle } from "../src/utils.mjs";

const command = process.argv[2] || "help";
const MODES = new Set(["plan", "debug", "review", "general"]);

function codexHomePath() {
  return process.env.CODEX_HOME || join(homedir(), ".codex");
}

function compactSession(session) {
  return {
    sessionId: session.sessionId,
    threadId: session.threadId,
    title: session.title || "Untitled Codex thread",
    cwd: session.cwd,
    startedAt: session.startedAt,
    updatedAt: session.updatedAt,
    messageCount: session.messageCount,
    source: session.source,
    isSubagent: session.isSubagent
  };
}

function selectedSession(sessionId) {
  const codexHome = codexHomePath();
  const session = findCodexSession({ codexHome, sessionId: sessionId || undefined });
  if (!session) {
    throw new Error(`No saved Codex conversation was found under ${join(codexHome, "sessions")}.`);
  }
  if (sessionId && session.sessionId !== sessionId && session.threadId !== sessionId) {
    throw new Error(`No saved Codex conversation matched ${sessionId}.`);
  }
  return session;
}

function prepareQuickChat(sessionId, rawMode, requestParts) {
  const session = selectedSession(sessionId);
  const mode = MODES.has(String(rawMode || "").toLowerCase())
    ? String(rawMode).toLowerCase()
    : "plan";
  const request = requestParts.join(" ").trim()
    || "Summarize the current Codex work and determine the best next implementation step.";
  const sessionCwd = session.cwd && existsSync(session.cwd) ? session.cwd : process.cwd();
  const repo = collectRepoContext(sessionCwd);
  const handoff = createHandoff({
    mode,
    request,
    hookInput: {
      cwd: session.cwd || sessionCwd,
      session_id: session.sessionId,
      turn_id: null,
      model: null
    },
    repo,
    thread: session.thread
  });

  const quickChatPrompt = truncateMiddle([
    "You are ChatGPT Quick Chat working beside an active Codex session.",
    "First give a concise state-of-work summary, then directly complete the requested planning, debugging, or review task.",
    "Do not claim you changed files. End with a compact CODEX EXECUTION PACKET that can be pasted back into Codex.",
    "",
    handoff
  ].join("\n"), 80000);

  const handoffDir = join(repo.root, ".sidecar", "handoffs");
  mkdirSync(handoffDir, { recursive: true });
  const handoffPath = join(handoffDir, `${isoFileStamp()}-${mode}-quick-chat.md`);
  writeFileSync(handoffPath, quickChatPrompt, "utf8");
  const copied = copyToClipboard(quickChatPrompt);

  return {
    selectedSession: compactSession(session),
    repositoryRoot: repo.root,
    mode,
    request,
    handoffPath,
    copied,
    promptCharacters: quickChatPrompt.length,
    modelTurnStarted: false
  };
}

try {
  if (command === "list") {
    const requestedLimit = Number.parseInt(process.argv[3] || "8", 10);
    const limit = Number.isFinite(requestedLimit) ? Math.max(1, Math.min(20, requestedLimit)) : 8;
    const sessions = listRecentCodexSessions({
      codexHome: codexHomePath(),
      limit,
      includeSubagents: false
    }).map(compactSession);
    process.stdout.write(`${JSON.stringify(sessions)}\n`);
    process.exit(0);
  }

  if (command === "prepare") {
    const sessionId = String(process.argv[3] || "").trim();
    const mode = process.argv[4] || "plan";
    const result = prepareQuickChat(sessionId, mode, process.argv.slice(5));
    process.stdout.write(`${JSON.stringify(result)}\n`);
    process.exit(0);
  }

  process.stdout.write("ChatGPT Sidecar Quick Chat helper\n\nCommands:\n  list [limit]\n  prepare <session-id> <plan|debug|review|general> <request>\n");
} catch (error) {
  process.stderr.write(`${error instanceof Error ? error.message : String(error)}\n`);
  process.exit(1);
}
