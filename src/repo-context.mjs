import { execFileSync } from "node:child_process";
import { existsSync, readFileSync, statSync } from "node:fs";
import { relative, resolve } from "node:path";
import { truncateMiddle } from "./utils.mjs";

function runGit(cwd, args, maxChars = 30000) {
  try {
    const output = execFileSync("git", args, {
      cwd,
      encoding: "utf8",
      stdio: ["ignore", "pipe", "ignore"],
      maxBuffer: 8 * 1024 * 1024
    }).trim();
    return truncateMiddle(output, maxChars);
  } catch {
    return "";
  }
}

function readSmallFile(root, relativeFilePath, maxChars = 6000) {
  const absolute = resolve(root, relativeFilePath);
  const containedPath = relative(resolve(root), absolute);
  if (containedPath.startsWith("..") || containedPath === "") return null;
  if (!existsSync(absolute)) return null;
  try {
    if (!statSync(absolute).isFile()) return null;
    return truncateMiddle(readFileSync(absolute, "utf8"), maxChars);
  } catch {
    return null;
  }
}

export function collectRepoContext(cwd) {
  const root = runGit(cwd, ["rev-parse", "--show-toplevel"], 2000) || resolve(cwd);
  const branch = runGit(root, ["branch", "--show-current"], 1000);
  const head = runGit(root, ["rev-parse", "HEAD"], 1000);
  const remote = runGit(root, ["remote", "get-url", "origin"], 2000);
  const status = runGit(root, ["status", "--short", "--branch"], 16000);
  const diffStat = runGit(root, ["diff", "--stat"], 12000);
  const diff = runGit(root, ["diff", "--no-ext-diff", "--unified=3"], 50000);
  const stagedDiff = runGit(root, ["diff", "--cached", "--no-ext-diff", "--unified=3"], 30000);
  const recentCommits = runGit(root, ["log", "-8", "--oneline", "--decorate"], 8000);
  const trackedFiles = runGit(root, ["ls-files"], 28000).split("\n").filter(Boolean).slice(0, 500).join("\n");
  const keyFileCandidates = ["AGENTS.md", "README.md", "README", "package.json", "pyproject.toml", "Cargo.toml", "go.mod", "pom.xml", "build.gradle", "requirements.txt", "docker-compose.yml", "compose.yml"];
  const keyFiles = {};
  for (const name of keyFileCandidates) {
    const content = readSmallFile(root, name);
    if (content != null) keyFiles[name] = content;
  }
  return { root, branch, head, remote, status, diffStat, diff, stagedDiff, recentCommits, trackedFiles, keyFiles };
}
