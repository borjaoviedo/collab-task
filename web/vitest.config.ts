/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'


export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['src/tests/setupTests.ts'],
    globals: false,
    coverage: {
      provider: 'v8',
      reportsDirectory: 'coverage',
      reporter: ['text', 'lcov'],
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        '**/*.d.ts',
        'src/tests/**',
        'src/**/__tests__/**',
        'src/**/mocks/**',
      ],
      thresholds: { lines: 60, functions: 60, branches: 60, statements: 60 },
    }
  },
  resolve: {
  alias: {
    '@shared': '/src/shared',
    '@features': '/src/features',
    '@presentation': '/src/presentation',
    '@app': '/src/app',
  },
},
})
