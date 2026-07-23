import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import prettierConfig from 'eslint-config-prettier';

export default tseslint.config(
  { ignores: ['node_modules', 'android', 'ios', '.expo', 'dist'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  prettierConfig,
);
