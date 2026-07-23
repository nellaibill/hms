export function AppFooter() {
  return (
    <footer className="flex h-10 items-center justify-between border-t border-border px-6 text-xs text-muted-foreground">
      <span>© {new Date().getFullYear()} Lakshmi Hospitals. All rights reserved.</span>
      <span>HMS v0.1.0 · Application Shell (mock data)</span>
    </footer>
  );
}
