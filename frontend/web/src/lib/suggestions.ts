/**
 * Matching for SuggestionTextarea: which suggestions fit what's typed so far, and the inline
 * completion Tab would accept. Case and repeated spaces never matter.
 */

function normalize(text: string): string {
  return text.trim().replace(/\s+/g, ' ').toLowerCase();
}

/** The suggestions that fit `text`, best first: those it starts (e.g. "sen" → "Senior citizen"),
 * then those with a word it starts ("cit" → "Senior citizen"), then — from three letters on, so a
 * single letter doesn't match half the list — any that contain it. Empty text fits everything. A
 * suggestion already typed exactly is left out. */
export function matchSuggestions(text: string, suggestions: readonly string[]): string[] {
  const query = normalize(text);
  if (!query) return [...suggestions];

  const starts: string[] = [];
  const wordStarts: string[] = [];
  const contains: string[] = [];
  for (const suggestion of suggestions) {
    const candidate = normalize(suggestion);
    if (candidate === query) continue;
    if (candidate.startsWith(query)) starts.push(suggestion);
    else if (candidate.includes(` ${query}`)) wordStarts.push(suggestion);
    else if (query.length >= 3 && candidate.includes(query)) contains.push(suggestion);
  }
  return [...starts, ...wordStarts, ...contains];
}

/** The rest of `suggestion` after what's typed — "ior citizen" for "Sen" — or null when the
 * suggestion doesn't start with it. Empty text completes to the whole suggestion. */
export function completionFor(text: string, suggestion: string | undefined): string | null {
  if (!suggestion) return null;
  if (!suggestion.toLowerCase().startsWith(text.toLowerCase()) || suggestion.length === text.length) return null;
  return suggestion.slice(text.length);
}

/** Joins suggestion lists in order, dropping repeats (ignoring case and spacing) and blanks. */
export function mergeSuggestions(...lists: readonly (readonly string[])[]): string[] {
  const seen = new Set<string>();
  const merged: string[] = [];
  for (const list of lists) {
    for (const suggestion of list) {
      const key = normalize(suggestion);
      if (!key || seen.has(key)) continue;
      seen.add(key);
      merged.push(suggestion.trim());
    }
  }
  return merged;
}
