import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './app/App';
import { installStaleBuildReload } from './lib/staleBuild';
import './index.css';

installStaleBuildReload();

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('Root element (#root) not found.');
}

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
