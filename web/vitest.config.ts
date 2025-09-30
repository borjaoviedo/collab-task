/// <reference types="vitest" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: 'src/tests/setupTests.ts',
    coverage: {
      provider: 'v8',
      reportsDirectory: 'coverage',
      reporter: ['text', 'lcov'],
      lines: 60,
      functions: 60,
      branches: 60,
      statements: 60,
      exclude: ['**/*.d.ts', '**/types.ts', 'src/shared/api/types.ts'],
    },
  },
})
