const COMMANDS = new Map([
  ["/gpt", "general"],
  ["/gpt-plan", "plan"],
  ["/gpt-debug", "debug"],
  ["/gpt-review", "review"]
]);

/**
 * @param {string} prompt
 * @returns {{command: string, mode: string, request: string} | null}
 */
export function parseSidecarCommand(prompt) {
  const trimmed = String(prompt ?? "").trim();
  if (!trimmed.startsWith("/")) return null;

  const firstSpace = trimmed.search(/\s/);
  const command = firstSpace === -1 ? trimmed : trimmed.slice(0, firstSpace);
  const mode = COMMANDS.get(command.toLowerCase());
  if (!mode) return null;

  const request = firstSpace === -1 ? "" : trimmed.slice(firstSpace).trim();
  return { command: command.toLowerCase(), mode, request };
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
