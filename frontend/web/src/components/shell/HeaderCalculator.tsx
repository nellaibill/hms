import { useState } from 'react';
import { Calculator as CalculatorIcon } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';
import { HeaderIconLabel } from '@/components/shell/HeaderIconLabel';

type Operator = '+' | '-' | '×' | '÷';

function compute(a: number, b: number, operator: Operator): number {
  switch (operator) {
    case '+':
      return a + b;
    case '-':
      return a - b;
    case '×':
      return a * b;
    case '÷':
      return b === 0 ? NaN : a / b;
  }
}

const KEYS: Array<{ label: string; span?: number }> = [
  { label: '7' }, { label: '8' }, { label: '9' }, { label: '÷' },
  { label: '4' }, { label: '5' }, { label: '6' }, { label: '×' },
  { label: '1' }, { label: '2' }, { label: '3' }, { label: '-' },
  { label: '0', span: 2 }, { label: '.' }, { label: '+' },
];

/** A small self-contained 4-function calculator — no backend, just local arithmetic state. */
function Calculator() {
  const [display, setDisplay] = useState('0');
  const [stored, setStored] = useState<number | null>(null);
  const [pendingOp, setPendingOp] = useState<Operator | null>(null);
  const [awaitingOperand, setAwaitingOperand] = useState(false);

  function clear() {
    setDisplay('0');
    setStored(null);
    setPendingOp(null);
    setAwaitingOperand(false);
  }

  function inputDigit(digit: string) {
    if (awaitingOperand) {
      setDisplay(digit === '.' ? '0.' : digit);
      setAwaitingOperand(false);
      return;
    }
    if (digit === '.' && display.includes('.')) return;
    setDisplay(display === '0' && digit !== '.' ? digit : display + digit);
  }

  function applyOperator(operator: Operator) {
    const value = Number.parseFloat(display);
    if (stored !== null && pendingOp && !awaitingOperand) {
      setDisplay(String(compute(stored, value, pendingOp)));
      setStored(compute(stored, value, pendingOp));
    } else {
      setStored(value);
    }
    setPendingOp(operator);
    setAwaitingOperand(true);
  }

  function equals() {
    if (stored === null || !pendingOp) return;
    const value = Number.parseFloat(display);
    setDisplay(String(compute(stored, value, pendingOp)));
    setStored(null);
    setPendingOp(null);
    setAwaitingOperand(true);
  }

  function handleKey(label: string) {
    if (label === '+' || label === '-' || label === '×' || label === '÷') {
      applyOperator(label);
    } else {
      inputDigit(label);
    }
  }

  return (
    <div className="w-56 p-1">
      <div className="mb-2 rounded-md bg-muted px-3 py-2 text-right text-lg font-mono tabular-nums text-foreground">{display}</div>
      <div className="grid grid-cols-4 gap-1">
        {KEYS.map((key) => (
          <Button
            key={key.label}
            type="button"
            variant="outline"
            size="sm"
            className={key.span === 2 ? 'col-span-2' : undefined}
            onClick={() => handleKey(key.label)}
          >
            {key.label}
          </Button>
        ))}
        <Button type="button" variant="secondary" size="sm" onClick={clear}>
          C
        </Button>
        <Button type="button" variant="default" size="sm" className="col-span-3" onClick={equals}>
          =
        </Button>
      </div>
    </div>
  );
}

/** Labeled "Tools" in the top bar (matches the app's updated header design) — still just the
 * calculator popover underneath for now; the label is generic in case more tools join it later. */
export function HeaderCalculator() {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-auto w-auto px-2.5 py-1" aria-label="Tools">
          <HeaderIconLabel icon={CalculatorIcon} label="Tools" showChevron />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" onCloseAutoFocus={(event) => event.preventDefault()}>
        <Calculator />
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
