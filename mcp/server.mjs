#!/usr/bin/env node
import readline from "node:readline";
import { existsSync } from "node:fs";
import { homedir } from "node:os";
import { join, resolve } from "node:path";
import { collectRepoContext } from "../src/repo-context.mjs";
import { createHandoff } from "../src/handoff.mjs";
import {
  findCodexSession,
  listRecentCodexSessions
} from "../src/local-session.mjs";

const SERVER_NAME = "chatgpt-sidecar";
const SERVER_VERSION = "0.6.0";
const DEFAULT_PROTOCOL_VERSION = "2025-06-18";
const MODES = new Set(["plan", "debug", "review", "general"]);

function codexHomePath() {
  return process.env.CODEX_HOME || join(homedir(), ".codex");
}

function normalizedMode(value) {
  const mode = String(value || "general").toLowerCase();
  return MODES.has(mode) ? mode : "general";
}

function compactSession(session) {
  return {
    sessionId: session.sessionId,
    threadId: session.threadId,
    title: session.title,
    cwd: session.cwd,
    startedAt: session.startedAt,
    updatedAt: session.updatedAt,
    messageCount: session.messageCount,
    source: session.source,
    isSubagent: session.isSubagent
  };
}

function textResult(text, structuredContent = undefined) {
  const result = {
    content: [{ type: "text", text: String(text) }]
  };
  if (structuredContent !== undefined) result.structuredContent = structuredContent;
  return result;
}

function toolError(message) {
  return {
    isError: true,
    content: [{ type: "text", text: String(message) }]
  };
}

const READ_ONLY_ANNOTATIONS = {
  readOnlyHint: true,
  destructiveHint: false,
  idempotentHint: true,
  openWorldHint: false
};

const TOOLS = [
  {
    name: "sidecar_get_active_context",
    title: "Get active Codex context",
    description: "Read the most recently active root Codex conversation and its local repository context without starting a Codex thread or model turn. Use this first for Sidecar planning, debugging, review, or investigation.",
    annotations: READ_ONLY_ANNOTATIONS,
    inputSchema: {
      type: "object",
      additionalProperties: false,
      properties: {
        request: {
          type: "string",
          description: "What the user wants ChatGPT to do with the Codex context."
        },
        mode: {
          type: "string",
          enum: ["plan", "debug", "review", "general"],
          default: "general"
        },
        session_id: {
          type: "string",
          description: "Optional exact Codex session or thread id. Omit to use the newest root conversation."
        },
        cwd: {
          type: "string",
          description: "Optional project directory used to choose the newest matching root conversation."
        }
      },
      required: ["request"]
    },
    _meta: {
      "openai/toolInvocation/invoking": "Reading the active Codex context",
      "openai/toolInvocation/invoked": "Codex context attached"
    }
  },
  {
    name: "sidecar_list_recent_threads",
    title: "List recent Codex threads",
    description: "List recent saved root Codex conversations so the current project or thread can be selected when more than one Codex chat is active.",
    annotations: READ_ONLY_ANNOTATIONS,
    inputSchema: {
      type: "object",
      additionalProperties: false,
      properties: {
        limit: { type: "integer", minimum: 1, maximum: 20, default: 8 },
        cwd: { type: "string", description: "Optional exact project directory filter." }
      }
    }
  },
  {
    name: "sidecar_get_thread",
    title: "Read a Codex thread",
    description: "Read one saved Codex conversation by session or thread id without resuming it or starting a model turn.",
    annotations: READ_ONLY_ANNOTATIONS,
    inputSchema: {
      type: "object",
      additionalProperties: false,
      properties: {
        session_id: { type: "string" }
      },
      required: ["session_id"]
    }
  },
  {
    name: "sidecar_get_repo_context",
    title: "Read repository context",
    description: "Read Git status, diffs, recent commits, tracked files, and common project instruction or manifest files from a local repository.",
    annotations: READ_ONLY_ANNOTATIONS,
    inputSchema: {
      type: "object",
      additionalProperties: false,
      properties: {
        cwd: { type: "string", description: "Repository directory." }
      },
      required: ["cwd"]
    }
  }
];

function getActiveContext(args = {}) {
  const codexHome = codexHomePath();
  const session = findCodexSession({
    codexHome,
    sessionId: args.session_id,
    cwd: args.cwd
  });

  if (!session) {
    return toolError(`No saved Codex conversation was found under ${join(codexHome, "sessions")}. Open a Codex project and send at least one normal Codex message first.`);
  }

  const requestedCwd = args.cwd ? resolve(args.cwd) : null;
  const sessionCwd = session.cwd && existsSync(session.cwd) ? session.cwd : null;
  const repoCwd = sessionCwd || (requestedCwd && existsSync(requestedCwd) ? requestedCwd : process.cwd());
  const repo = collectRepoContext(repoCwd);
  const mode = normalizedMode(args.mode);
  const request = String(args.request || "Analyze the current Codex conversation and recommend the best next step.").trim();
  const handoff = createHandoff({
    mode,
    request,
    hookInput: {
      cwd: session.cwd || repoCwd,
      session_id: session.sessionId,
      turn_id: null,
      model: null
    },
    repo,
    thread: session.thread
  });

  return textResult(handoff, {
    selectedSession: compactSession(session),
    repository: {
      root: repo.root,
      branch: repo.branch,
      head: repo.head,
      remote: repo.remote
    },
    mode,
    modelTurnStarted: false,
    source: "saved-codex-rollout"
  });
}

function listRecentThreads(args = {}) {
  const sessions = listRecentCodexSessions({
    codexHome: codexHomePath(),
    limit: args.limit,
    cwd: args.cwd
  }).map(compactSession);

  return textResult(JSON.stringify({ sessions, modelTurnStarted: false }, null, 2), {
    sessions,
    modelTurnStarted: false
  });
}

function getThread(args = {}) {
  const sessionId = String(args.session_id || "").trim();
  if (!sessionId) return toolError("session_id is required");
  const session = findCodexSession({ codexHome: codexHomePath(), sessionId });
  if (!session || (session.sessionId !== sessionId && session.threadId !== sessionId)) {
    return toolError(`No saved Codex conversation matched ${sessionId}`);
  }
  return textResult(JSON.stringify(session.thread, null, 2), {
    session: compactSession(session),
    modelTurnStarted: false
  });
}

function getRepoContext(args = {}) {
  const cwd = resolve(String(args.cwd || ""));
  if (!args.cwd || !existsSync(cwd)) return toolError(`Repository directory does not exist: ${args.cwd || "(missing)"}`);
  const repo = collectRepoContext(cwd);
  return textResult(JSON.stringify(repo, null, 2), {
    repository: {
      root: repo.root,
      branch: repo.branch,
      head: repo.head,
      remote: repo.remote
    },
    modelTurnStarted: false
  });
}

function callTool(name, args) {
  switch (name) {
    case "sidecar_get_active_context":
      return getActiveContext(args);
    case "sidecar_list_recent_threads":
      return listRecentThreads(args);
    case "sidecar_get_thread":
      return getThread(args);
    case "sidecar_get_repo_context":
      return getRepoContext(args);
    default:
      return toolError(`Unknown Sidecar tool: ${name}`);
  }
}

function handleRequest(message) {
  const { id, method, params = {} } = message || {};

  if (method === "initialize") {
    return {
      jsonrpc: "2.0",
      id,
      result: {
        protocolVersion: params.protocolVersion || DEFAULT_PROTOCOL_VERSION,
        capabilities: {
          tools: { listChanged: false },
          prompts: { listChanged: false }
        },
        serverInfo: { name: SERVER_NAME, version: SERVER_VERSION },
        instructions: "Use sidecar_get_active_context to bring the active saved Codex conversation and repository into this ChatGPT conversation without starting a Codex model turn."
      }
    };
  }

  if (method === "ping") return { jsonrpc: "2.0", id, result: {} };
  if (method === "tools/list") return { jsonrpc: "2.0", id, result: { tools: TOOLS } };
  if (method === "tools/call") {
    return {
      jsonrpc: "2.0",
      id,
      result: callTool(params.name, params.arguments || {})
    };
  }
  if (method === "prompts/list") {
    return {
      jsonrpc: "2.0",
      id,
      result: {
        prompts: [
          {
            name: "sidecar",
            title: "Use active Codex context",
            description: "Attach the active saved Codex conversation and repository context.",
            arguments: [
              { name: "request", description: "What ChatGPT should do", required: true },
              { name: "mode", description: "plan, debug, review, or general", required: false }
            ]
          }
        ]
      }
    };
  }
  if (method === "prompts/get") {
    if (params.name !== "sidecar") {
      return {
        jsonrpc: "2.0",
        id,
        error: { code: -32602, message: `Unknown prompt: ${params.name}` }
      };
    }
    const request = String(params.arguments?.request || "Analyze my active Codex context.");
    const mode = normalizedMode(params.arguments?.mode);
    return {
      jsonrpc: "2.0",
      id,
      result: {
        description: "Use ChatGPT Sidecar",
        messages: [
          {
            role: "user",
            content: {
              type: "text",
              text: `Call sidecar_get_active_context with mode=${mode} and this request: ${request}. Then answer using the returned Codex thread and repository context.`
            }
          }
        ]
      }
    };
  }
  if (method === "resources/list") return { jsonrpc: "2.0", id, result: { resources: [] } };

  if (id === undefined || id === null) return null;
  return {
    jsonrpc: "2.0",
    id,
    error: { code: -32601, message: `Method not found: ${method}` }
  };
}

function writeMessage(message) {
  if (!message) return;
  process.stdout.write(`${JSON.stringify(message)}\n`);
}

const lines = readline.createInterface({ input: process.stdin, crlfDelay: Infinity });
lines.on("line", (line) => {
  if (!line.trim()) return;
  let message;
  try {
    message = JSON.parse(line);
  } catch (error) {
    writeMessage({
      jsonrpc: "2.0",
      id: null,
      error: { code: -32700, message: `Parse error: ${error instanceof Error ? error.message : String(error)}` }
    });
    return;
  }

  try {
    writeMessage(handleRequest(message));
  } catch (error) {
    if (message.id === undefined || message.id === null) return;
    writeMessage({
      jsonrpc: "2.0",
      id: message.id,
      error: {
        code: -32603,
        message: error instanceof Error ? error.message : String(error)
      }
    });
  }
});

process.on("uncaughtException", (error) => {
  process.stderr.write(`[chatgpt-sidecar] ${error.stack || error.message}\n`);
});
