const MODE_WORDS = new Map([
  ["plan", "plan"],
  ["debug", "debug"],
  ["review", "review"]
]);

function parseRequest(raw) {
  const text = String(raw ?? "").trim();
  const match = /^(plan|debug|review)(?:(?:\s+|:\s*)([\s\S]*))?$/i.exec(text);
  if (!match) return { mode: "general", request: text };
  return {
    mode: MODE_WORDS.get(match[1].toLowerCase()),
    request: (match[2] || "").trim()
  };
}

export function parseSidecarCommand(prompt) {
  const trimmed = String(prompt ?? "").trim();
  if (!trimmed) return null;

  const skillMatch = /^\$sidecar(?:\s+([\s\S]*))?$/i.exec(trimmed);
  if (skillMatch) {
    const parsed = parseRequest(skillMatch[1] || "");
    return { command: "$sidecar", ...parsed };
  }

  const markerMatch = /^SIDECAR_HANDOFF\s*:\s*([\s\S]*)$/i.exec(trimmed);
  if (markerMatch) {
    const parsed = parseRequest(markerMatch[1]);
    return { command: "/prompts:sidecar", ...parsed };
  }

  const textMatch = /^sidecar\s*:\s*([\s\S]*)$/i.exec(trimmed);
  if (textMatch) {
    const parsed = parseRequest(textMatch[1]);
    return { command: "sidecar:", ...parsed };
  }

  const futureSlashMatch = /^\/sidecar(?:\s+([\s\S]*))?$/i.exec(trimmed);
  if (futureSlashMatch) {
    const parsed = parseRequest(futureSlashMatch[1] || "");
    return { command: "/sidecar", ...parsed };
  }

  const legacyMatch = /^(gpt(?:-(?:plan|debug|review))?)\s*:\s*([\s\S]*)$/i.exec(trimmed);
  if (legacyMatch) {
    const legacy = legacyMatch[1].toLowerCase();
    const mode = legacy === "gpt-plan" ? "plan" : legacy === "gpt-debug" ? "debug" : legacy === "gpt-review" ? "review" : "general";
    return { command: `${legacy}:`, mode, request: legacyMatch[2].trim() };
  }

  return null;
}

export function modeInstruction(mode) {
  switch (mode) {
    case "plan":
      return "Produce an implementation-ready plan. Identify exact files, ordered steps, risks, tests, and acceptance criteria. Do not write code unless a small example is essential.";
    case "debug":
      return "Diagnose the failure using the supplied conversation, Git diff, and repository context. Rank likely causes, identify evidence to collect, and propose the smallest verified fix.";
    case "review":
      return "Review the current repository changes. Focus on correctness, regressions, security, missing tests, and unnecessary complexity. Give findings before suggestions.";
    default:
      return "Help complete the request using the supplied Codex conversation and repository context. Investigate and plan here so Codex can later focus on implementation and verification.";
  }
}
