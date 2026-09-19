import { ApiError, type OpdConsultationFormValues } from '@hms/shared';
import { Loader2, Mic, Sparkles, Square } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import type { UseFormSetValue } from 'react-hook-form';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { useToast } from '@/components/ui/toast-context';
import { useStructureOpdConsultationNoteMutation } from '../hooks/useOpdConsultationMutations';
import { textareaClassName } from './textareaClassName';

/** The Web Speech API has no official TS lib types (it's non-standard, Chrome/Edge only). This
 * declares just the surface this panel uses — see MDN's SpeechRecognition docs. */
interface MinimalSpeechRecognitionResult {
  readonly isFinal: boolean;
  readonly 0: { readonly transcript: string };
}

interface MinimalSpeechRecognitionEvent extends Event {
  readonly resultIndex: number;
  readonly results: ArrayLike<MinimalSpeechRecognitionResult>;
}

interface MinimalSpeechRecognition extends EventTarget {
  continuous: boolean;
  interimResults: boolean;
  lang: string;
  onresult: ((event: MinimalSpeechRecognitionEvent) => void) | null;
  onerror: ((event: Event) => void) | null;
  onend: (() => void) | null;
  start: () => void;
  stop: () => void;
}

declare global {
  interface Window {
    SpeechRecognition?: new () => MinimalSpeechRecognition;
    webkitSpeechRecognition?: new () => MinimalSpeechRecognition;
  }
}

const AI_NOT_CONFIGURED_ERROR_CODE = 'OPD_CONSULTATION.AI_NOT_CONFIGURED';

const NARRATIVE_FIELD_KEYS = [
  'presentingComplaints',
  'clinicalHistory',
  'examinationFindings',
  'planOfManagement',
  'followUpInstructions',
  'emergencyReviewInstructions',
] as const satisfies readonly (keyof OpdConsultationFormValues)[];

interface AiNoteDictationPanelProps {
  consultationId: string;
  setValue: UseFormSetValue<OpdConsultationFormValues>;
  disabled?: boolean;
}

/**
 * Lets the consultant dictate (live, via the browser's Web Speech API) or paste/type a
 * consultation transcript, then calls the backend to structure it into the form's own narrative
 * fields (Presenting Complaints / Clinical History / Examination Findings / Plan of Management /
 * Follow-up / Emergency Review). Nothing here auto-saves — the existing Save Draft/Complete
 * buttons remain the only persistence path; the consultant is expected to review the populated
 * fields before saving, same as if they'd typed them directly.
 */
export function AiNoteDictationPanel({ consultationId, setValue, disabled }: AiNoteDictationPanelProps) {
  const { toast } = useToast();
  const [transcript, setTranscript] = useState('');
  const [isRecording, setIsRecording] = useState(false);
  const recognitionRef = useRef<MinimalSpeechRecognition | null>(null);
  const baseTranscriptRef = useRef('');
  const mutation = useStructureOpdConsultationNoteMutation(consultationId);

  const speechRecognitionSupported = typeof window !== 'undefined' && Boolean(window.SpeechRecognition ?? window.webkitSpeechRecognition);

  useEffect(() => {
    return () => {
      recognitionRef.current?.stop();
    };
  }, []);

  function startRecording() {
    const SpeechRecognitionCtor = window.SpeechRecognition ?? window.webkitSpeechRecognition;
    if (!SpeechRecognitionCtor) return;

    const recognition = new SpeechRecognitionCtor();
    recognition.continuous = true;
    recognition.interimResults = true;
    recognition.lang = navigator.language || 'en-IN';
    baseTranscriptRef.current = transcript ? `${transcript} ` : '';

    recognition.onresult = (event) => {
      let finalText = '';
      let interimText = '';
      for (let i = event.resultIndex; i < event.results.length; i += 1) {
        const result = event.results[i];
        if (result.isFinal) finalText += result[0].transcript;
        else interimText += result[0].transcript;
      }
      if (finalText) baseTranscriptRef.current += `${finalText} `;
      setTranscript(`${baseTranscriptRef.current}${interimText}`.trim());
    };
    recognition.onerror = () => setIsRecording(false);
    recognition.onend = () => setIsRecording(false);

    recognition.start();
    recognitionRef.current = recognition;
    setIsRecording(true);
  }

  function stopRecording() {
    recognitionRef.current?.stop();
    recognitionRef.current = null;
    setIsRecording(false);
  }

  function handleGenerate() {
    mutation.mutate(
      { transcript },
      {
        onSuccess: (fields) => {
          for (const key of NARRATIVE_FIELD_KEYS) {
            setValue(key, fields[key] ?? '', { shouldDirty: true });
          }
          toast({ title: 'Note generated', description: 'Review the populated fields before saving.', variant: 'success' });
        },
        onError: (error) => {
          const isNotConfigured = error instanceof ApiError && error.errorCode === AI_NOT_CONFIGURED_ERROR_CODE;
          toast({
            title: 'Could not generate note',
            description: isNotConfigured
              ? "AI note generation isn't configured for this environment."
              : error instanceof ApiError
                ? error.message
                : 'Something went wrong. Please try again.',
            variant: 'error',
          });
        },
      },
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <Label htmlFor="ai-note-transcript">Transcript</Label>
        {speechRecognitionSupported && (
          <Button
            type="button"
            variant={isRecording ? 'destructive' : 'outline'}
            size="sm"
            disabled={disabled}
            onClick={isRecording ? stopRecording : startRecording}
            className="gap-1.5"
          >
            {isRecording ? <Square className="h-3.5 w-3.5" /> : <Mic className="h-3.5 w-3.5" />}
            {isRecording ? 'Stop' : 'Dictate'}
          </Button>
        )}
      </div>

      <textarea
        id="ai-note-transcript"
        rows={4}
        placeholder="Dictate using the mic, or paste/type the consultation transcript here…"
        className={textareaClassName}
        value={transcript}
        disabled={disabled}
        onChange={(e) => setTranscript(e.target.value)}
      />

      <div>
        <Button type="button" size="sm" disabled={disabled || !transcript.trim() || mutation.isPending} onClick={handleGenerate} className="gap-1.5">
          {mutation.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
          {mutation.isPending ? 'Generating…' : 'Generate Note'}
        </Button>
      </div>
    </div>
  );
}
