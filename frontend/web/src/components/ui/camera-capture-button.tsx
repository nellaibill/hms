import { Camera, RotateCcw } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { Button } from './button';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from './dialog';

interface CameraCaptureButtonProps {
  disabled?: boolean;
  /** Base name for the captured file, before the "-{timestamp}.jpg" suffix — e.g. "patient-photo". */
  fileNamePrefix: string;
  onCapture: (file: File) => void;
}

/**
 * Opens a live camera preview in a dialog and lets the user snap a photo instead of picking a
 * file — sits next to FileChooserButton wherever a photo upload accepts either source (see
 * docs/PatientRegistrationModule.md §13). Camera access is requested only while the dialog is
 * open, and every track is stopped the instant it closes — by capturing, cancelling, or an
 * error — so the camera light never stays on in the background.
 *
 * Graceful degradation per §13: if getUserMedia is unsupported, no camera is found, or
 * permission is denied, an inline message replaces the preview and the dialog's only action is
 * to close. Nothing else needs to change when a camera isn't available — the caller's own
 * FileChooserButton right next to this one is already the fallback path.
 */
export function CameraCaptureButton({ disabled, fileNamePrefix, onCapture }: CameraCaptureButtonProps) {
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [capturedUrl, setCapturedUrl] = useState<string | null>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const capturedBlobRef = useRef<Blob | null>(null);

  // One effect drives the whole camera lifecycle off `open` alone — re-running it (by toggling
  // `open` off and back on, see handleRetake) is how a retake asks the camera for a fresh frame
  // rather than trying to resume a stream that capture already tore down.
  useEffect(() => {
    if (!open) return;

    setError(null);

    if (!navigator.mediaDevices?.getUserMedia) {
      setError('Camera not available — upload a photo instead.');
      return;
    }

    let cancelled = false;
    navigator.mediaDevices
      .getUserMedia({ video: { facingMode: 'user' } })
      .then((stream) => {
        if (cancelled) {
          stream.getTracks().forEach((track) => track.stop());
          return;
        }
        streamRef.current = stream;
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
        }
      })
      .catch(() => {
        if (!cancelled) setError('Camera not available — upload a photo instead.');
      });

    return () => {
      cancelled = true;
      streamRef.current?.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
    };
  }, [open]);

  function handleCapture() {
    const video = videoRef.current;
    const canvas = canvasRef.current;
    if (!video || !canvas || video.videoWidth === 0) return;

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d')?.drawImage(video, 0, 0);
    canvas.toBlob(
      (blob) => {
        if (!blob) return;
        capturedBlobRef.current = blob;
        setCapturedUrl(URL.createObjectURL(blob));
        streamRef.current?.getTracks().forEach((track) => track.stop());
        streamRef.current = null;
      },
      'image/jpeg',
      0.9,
    );
  }

  function handleRetake() {
    if (capturedUrl) URL.revokeObjectURL(capturedUrl);
    setCapturedUrl(null);
    capturedBlobRef.current = null;
    setOpen(false);
    // Reopening on the next tick re-runs the effect above, requesting a fresh stream.
    setTimeout(() => setOpen(true), 0);
  }

  function handleUsePhoto() {
    const blob = capturedBlobRef.current;
    if (!blob) return;
    onCapture(new File([blob], `${fileNamePrefix}-${Date.now()}.jpg`, { type: 'image/jpeg' }));
    handleOpenChange(false);
  }

  function handleOpenChange(next: boolean) {
    if (!next) {
      streamRef.current?.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
      if (capturedUrl) URL.revokeObjectURL(capturedUrl);
      setCapturedUrl(null);
    }
    setOpen(next);
  }

  return (
    <>
      <Button type="button" size="sm" variant="outline" disabled={disabled} onClick={() => setOpen(true)}>
        <Camera />
        Use Camera
      </Button>

      <Dialog open={open} onOpenChange={handleOpenChange}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Capture photo</DialogTitle>
          </DialogHeader>

          {error ? (
            <p className="text-sm text-muted-foreground">{error}</p>
          ) : capturedUrl ? (
            <img src={capturedUrl} alt="Captured preview" className="w-full rounded-md" />
          ) : (
            <video ref={videoRef} autoPlay playsInline muted className="w-full rounded-md bg-black" />
          )}
          <canvas ref={canvasRef} className="hidden" />

          <DialogFooter>
            {error ? (
              <Button type="button" size="sm" onClick={() => handleOpenChange(false)}>
                Close
              </Button>
            ) : capturedUrl ? (
              <>
                <Button type="button" size="sm" variant="outline" onClick={handleRetake}>
                  <RotateCcw />
                  Retake
                </Button>
                <Button type="button" size="sm" onClick={handleUsePhoto}>
                  Use Photo
                </Button>
              </>
            ) : (
              <Button type="button" size="sm" onClick={handleCapture}>
                Capture
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
