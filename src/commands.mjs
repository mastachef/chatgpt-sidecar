const COMMANDS = new Map([
  ["gpt", "general"],
  ["gpt-plan", "plan"],
  ["gpt-debug", "debug"],
  ["gpt-review", "review"]
]);

const LEGACY_SLASH_COMMANDS = new Map([
  ["/gpt", "general"],
  ["/gpt-plan", "plan"],
  ["/gpt-debug", "debug"],
  ["/gpt-review", "review"]
]);

/**
 * Parse an explicit sidecar trigger.
 *
 * Current Codex clients reject unknown custom slash commands before
 * UserPromptSubmit hooks run. The supported trigger therefore uses normal
 * composer text such as `gpt: plan auth` or `gpt-debug: failing login`.
 * Legacy slash aliases remain parseable for direct hook tests and for any
 * future client that forwards unknown slash commands.
 *
 * @param {string} prompt
 * @returns {{command: string, mode: string, request: string} | null}
 */
export function parseSidecarCommand(prompt) {
  const trimmed = String(prompt ?? "").trim();
  if (!trimmed) return null;

  const colonMatch = /^(gpt(?:-(?:plan|debug|review))?)\s*:\s*([\s\S]*)$/i.exec(trimmed);
  if (colonMatch) {
    const command = colonMatch[1].toLowerCase();
    return {
      command: `${command}:`,
      mode: COMMANDS.get(command),
      request: colonMatch[2].trim()
    };
  }

  const legacyFirstSpace = trimmed.search(/\s/);
  const legacyCommand = legacyFirstSpace === -1 ? trimmed : trimmed.slice(0, legacyFirstSpace);
  const legacyMode = LEGACY_SLASH_COMMANDS.get(legacyCommand.toLowerCase());
  if (!legacyMode) return null;

  return {
    command: legacyCommand.toLowerCase(),
    mode: legacyMode,
    request: legacyFirstSpace === -1 ? "" : trimmed.slice(legacyFirstSpace).trim()
  };
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
