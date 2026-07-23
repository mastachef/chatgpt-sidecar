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

/** @param {string} value */
export function fence(value) {
  return `\n\`\`\`text\n${value || "(none)"}\n\`\`\`\n`;
}

export function isoFileStamp(date = new Date()) {
  return date.toISOString().replace(/[:.]/g, "-");
}
