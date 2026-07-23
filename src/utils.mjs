/** @param {unknown} value */
export function asText(value) {
  return value == null ? "" : String(value);
}

/**
 * Truncate large text while preserving both ends.
 * @param {string} text
 * @param {number} maxChars
 */
export function truncateMiddle(text, maxChars) {
  const value = asText(text);
  if (value.length <= maxChars) return value;
  const marker = `\n\n... [${value.length - maxChars} characters omitted] ...\n\n`;
  const available = Math.max(0, maxChars - marker.length);
  const left = Math.ceil(available * 0.62);
  const right = available - left;
  return `${value.slice(0, left)}${marker}${value.slice(value.length - right)}`;
}

/**
 * Best-effort redaction before repository and thread text leaves the machine.
 * @param {string} text
 */
export function redactSensitiveText(text) {
  let value = asText(text);
  value = value.replace(/-----BEGIN [^-\r\n]*PRIVATE KEY-----[\s\S]*?-----END [^-\r\n]*PRIVATE KEY-----/gi, "[REDACTED PRIVATE KEY]");
  value = value.replace(/\bBearer\s+[A-Za-z0-9._~+/=-]{12,}/gi, "Bearer [REDACTED]");
  value = value.replace(/\bsk-[A-Za-z0-9_-]{12,}\b/g, "[REDACTED OPENAI KEY]");
  value = value.replace(/\bgh[pousr]_[A-Za-z0-9]{20,}\b/g, "[REDACTED GITHUB TOKEN]");
  value = value.replace(/\bxox[baprs]-[A-Za-z0-9-]{10,}\b/g, "[REDACTED SLACK TOKEN]");
  value = value.replace(/\bAKIA[0-9A-Z]{16}\b/g, "[REDACTED AWS ACCESS KEY]");
  value = value.replace(/\b(https?:\/\/)([^\s/@:]+):([^\s/@]+)@/gi, "$1[REDACTED]@");
  value = value.replace(/(^|[\s,{])(\"?[A-Za-z0-9_.-]*(?:API[_-]?KEY|TOKEN|SECRET|PASSWORD|PASSWD|PRIVATE[_-]?KEY|ACCESS[_-]?KEY)[A-Za-z0-9_.-]*\"?\s*[:=]\s*)(\"[^\"\r\n]*\"|'[^'\r\n]*'|[^\s,}\r\n]+)/gim, "$1$2[REDACTED]");
  return value;
}

/** @param {string} value */
export function fence(value) {
  return `\n\`\`\`text\n${value || "(none)"}\n\`\`\`\n`;
}

export function isoFileStamp(date = new Date()) {
  return date.toISOString().replace(/[:.]/g, "-");
}
