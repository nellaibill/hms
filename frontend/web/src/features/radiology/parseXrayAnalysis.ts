/** The fixed section headings the model is instructed to reply with (see
 * HMS.Modules.Radiology.Application.XrayAnalysisPrompt). */
const SECTION_TITLES = [
  'ANATOMICAL REGION',
  'IMAGE QUALITY',
  'KEY FINDINGS',
  'FRACTURE ASSESSMENT',
  'JOINT & ALIGNMENT',
  'OTHER OBSERVATIONS',
  'AI CONFIDENCE',
  'CLINICAL REVIEW',
] as const;

export interface XrayAnalysisSection {
  title: string;
  body: string;
}

const HEADING_PATTERN = new RegExp(`^(${SECTION_TITLES.join('|')})\\s*:?\\s*(.*)$`, 'i');

/**
 * Splits the model's reply into its labelled sections. Tolerates markdown decoration around a
 * heading (`**KEY FINDINGS:**`) and text on the heading's own line. The CLINICAL REVIEW section is
 * dropped — the API's own `disclaimer` is shown instead. A reply with no recognisable heading is
 * returned whole as one untitled section rather than discarded, since a clinician should still
 * see what the model said.
 */
export function parseXrayAnalysis(text: string): XrayAnalysisSection[] {
  const sections: XrayAnalysisSection[] = [];
  let current: { title: string; lines: string[] } | null = null;

  for (const rawLine of text.split(/\r?\n/)) {
    const match = HEADING_PATTERN.exec(rawLine.replace(/[*#]/g, '').trim());
    if (match) {
      if (current) sections.push({ title: current.title, body: current.lines.join('\n').trim() });
      current = { title: match[1].toUpperCase(), lines: match[2] ? [match[2]] : [] };
    } else if (current) {
      current.lines.push(rawLine);
    }
  }
  if (current) sections.push({ title: current.title, body: current.lines.join('\n').trim() });

  if (sections.length === 0) return [{ title: '', body: text.trim() }];

  return sections.filter((section) => section.title !== 'CLINICAL REVIEW');
}
