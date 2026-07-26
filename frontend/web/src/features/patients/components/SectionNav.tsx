import { useEffect, useRef, useState } from 'react';
import { cn } from '@/lib/utils';

export interface SectionNavItem {
  id: string;
  label: string;
}

interface SectionNavProps {
  sections: SectionNavItem[];
  className?: string;
}

/**
 * Lightweight in-page section navigator + scrollspy — a sticky jump-list beside a long
 * form, not the full non-linear wizard/hard-gates/status-icons Section Navigator from
 * docs/PatientRegistrationModule.md §3 (that's deferred, see docs/DecisionLog.md). This
 * exists purely so a long single-page form stays easy to scan and jump around in.
 */
export function SectionNav({ sections, className }: SectionNavProps) {
  const [activeId, setActiveId] = useState(sections[0]?.id);
  const observerRef = useRef<IntersectionObserver | null>(null);

  useEffect(() => {
    const elements = sections
      .map((section) => document.getElementById(section.id))
      .filter((el): el is HTMLElement => el !== null);

    observerRef.current?.disconnect();
    observerRef.current = new IntersectionObserver(
      (entries) => {
        const visible = entries
          .filter((entry) => entry.isIntersecting)
          .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
        if (visible[0]) {
          setActiveId(visible[0].target.id);
        }
      },
      { rootMargin: '-96px 0px -70% 0px', threshold: 0 },
    );

    for (const el of elements) {
      observerRef.current.observe(el);
    }

    return () => observerRef.current?.disconnect();
  }, [sections]);

  function handleClick(event: React.MouseEvent<HTMLAnchorElement>, id: string) {
    event.preventDefault();
    document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    setActiveId(id);
    window.history.replaceState(null, '', `#${id}`);
  }

  return (
    <nav aria-label="Form sections" className={cn('sticky top-20 flex flex-col gap-0.5 self-start', className)}>
      {sections.map((section) => {
        const isActive = section.id === activeId;
        return (
          <a
            key={section.id}
            href={`#${section.id}`}
            aria-current={isActive ? 'true' : undefined}
            onClick={(event) => handleClick(event, section.id)}
            className={cn(
              'rounded-md border-l-2 px-2.5 py-1.5 text-[13px] leading-tight transition-colors',
              isActive
                ? 'border-primary bg-primary/10 font-medium text-primary'
                : 'border-transparent text-muted-foreground hover:bg-muted hover:text-foreground',
            )}
          >
            {section.label}
          </a>
        );
      })}
    </nav>
  );
}
