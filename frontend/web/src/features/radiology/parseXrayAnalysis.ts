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

export type XraySectionTitle = (typeof SECTION_TITLES)[number];

export interface XrayAnalysisSection {
  /** One of the fixed headings, or '' for a reply with no recognisable heading. */
  title: XraySectionTitle | '';
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
  let current: { title: XraySectionTitle; lines: string[] } | null = null;

  for (const rawLine of text.split(/\r?\n/)) {
    const match = HEADING_PATTERN.exec(rawLine.replace(/[*#]/g, '').trim());
    if (match) {
      if (current) sections.push({ title: current.title, body: current.lines.join('\n').trim() });
      current = { title: match[1].toUpperCase() as XraySectionTitle, lines: match[2] ? [match[2]] : [] };
    } else if (current) {
      current.lines.push(rawLine);
    }
  }
  if (current) sections.push({ title: current.title, body: current.lines.join('\n').trim() });

  if (sections.length === 0) return [{ title: '', body: text.trim() }];

  return sections.filter((section) => section.title !== 'CLINICAL REVIEW');
}

export type FractureVerdict = 'none' | 'possible' | 'identified' | 'unknown';
export type AiConfidence = 'High' | 'Moderate' | 'Low';

export interface XrayAnalysisSummary {
  verdict: FractureVerdict;
  confidence: AiConfidence | null;
  region: string | null;
}

function sectionBody(sections: XrayAnalysisSection[], title: XraySectionTitle): string {
  return sections.find((section) => section.title === title)?.body ?? '';
}

/** The at-a-glance facts of a reply: the fracture verdict, the model's own confidence and the body
 * region. Anything that can't be read confidently falls back to 'unknown'/null rather than guessing. */
export function summarizeXrayAnalysis(text: string): XrayAnalysisSummary {
  const sections = parseXrayAnalysis(text);
  const fracture = sectionBody(sections, 'FRACTURE ASSESSMENT').split('\n')[0]?.toLowerCase() ?? '';
  const confidence = /\b(high|moderate|low)\b/i.exec(sectionBody(sections, 'AI CONFIDENCE'))?.[1].toLowerCase();

  let verdict: FractureVerdict = 'unknown';
  if (/\bno (obvious|visible|definite|acute)?\s*fracture/.test(fracture)) verdict = 'none';
  else if (/\bpossible\b|\bsuspected\b|\bequivocal\b/.test(fracture)) verdict = 'possible';
  else if (/\bfracture identified\b|\bfracture (is )?(seen|present|visible)\b/.test(fracture)) verdict = 'identified';

  return {
    verdict,
    confidence: confidence ? ((confidence[0].toUpperCase() + confidence.slice(1)) as AiConfidence) : null,
    region: sectionBody(sections, 'ANATOMICAL REGION').split('\n')[0]?.trim() || null,
  };
}

/** "- item" / "* item" / "1. item" lines as a list; a body with no list markers becomes one item. */
export function parseBullets(body: string): string[] {
  const items = body
    .split('\n')
    .map((line) => line.replace(/^\s*(?:[-*•]|\d+[.)])\s*/, '').trim())
    .filter(Boolean);
  return items;
}

const FRACTURE_DETAIL_LABELS = ['Bone', 'Location', 'Pattern', 'Displacement', 'Angulation'] as const;

/** The "- Bone: … - Location: …" detail lines under FRACTURE ASSESSMENT that the model actually
 * filled in (empty or "N/A" values are dropped). */
export function parseFractureDetails(body: string): { label: string; value: string }[] {
  const details: { label: string; value: string }[] = [];
  for (const line of body.split('\n')) {
    const match = /^\s*[-*•]?\s*(Bone|Location|Pattern|Displacement|Angulation)\s*:\s*(.+)$/i.exec(line.replace(/\*\*/g, ''));
    if (!match) continue;
    const value = match[2].trim();
    if (!value || /^(n\/?a|none|not (applicable|assessed|determinable)|-|—)\.?$/i.test(value)) continue;
    const label = FRACTURE_DETAIL_LABELS.find((known) => known.toLowerCase() === match[1].toLowerCase());
    if (label) details.push({ label, value });
  }
  return details;
}

/** The first line of the fracture assessment (the verdict sentence), without the detail lines. */
export function fractureHeadline(body: string): string {
  const first = body.split('\n').find((line) => line.trim() && !/^\s*[-*•]/.test(line));
  return first?.trim() ?? '';
}
