import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'node:path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "src"),
      '@app': path.resolve(__dirname, 'src/app'),
      '@shared': path.resolve(__dirname, 'src/shared'),
      '@styles': path.resolve(__dirname, 'src/styles'),
      '@features': path.resolve(__dirname, 'src/features'),
      '@presentation': path.resolve(__dirname, 'src/presentation'),
    },
  },
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    hmr: { clientPort: 8081 }
  }
})