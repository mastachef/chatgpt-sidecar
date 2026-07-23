import { spawn, spawnSync } from "node:child_process";

/** @param {string} text */
export function copyToClipboard(text) {
  const platform = process.platform;
  if (platform === "win32") {
    const result = spawnSync("clip.exe", [], { input: text, encoding: "utf8", windowsHide: true });
    return result.status === 0;
  }
  if (platform === "darwin") {
    const result = spawnSync("pbcopy", [], { input: text, encoding: "utf8" });
    return result.status === 0;
  }

  for (const command of ["wl-copy", "xclip", "xsel"]) {
    const args = command === "xclip" ? ["-selection", "clipboard"] : command === "xsel" ? ["--clipboard", "--input"] : [];
    const result = spawnSync(command, args, { input: text, encoding: "utf8" });
    if (!result.error && result.status === 0) return true;
  }
  return false;
}

/** @param {string} url */
export function openUrl(url) {
  try {
    let child;
    if (process.platform === "win32") {
      child = spawn("cmd", ["/c", "start", "", url], { detached: true, stdio: "ignore", windowsHide: true });
    } else if (process.platform === "darwin") {
      child = spawn("open", [url], { detached: true, stdio: "ignore" });
    } else {
      child = spawn("xdg-open", [url], { detached: true, stdio: "ignore" });
    }
    child.unref();
    return true;
  } catch {
    return false;
  }
}
