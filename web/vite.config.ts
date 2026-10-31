/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// docs/testing.md, Determinism: fixed time zone for every test run.
process.env.TZ = 'Europe/Istanbul'

export default defineConfig({
  plugins: [react()],
  server: {
    // Local development: the API runs on :5080 (src/Api/PolsanAuction.Api/Properties/launchSettings.json).
    proxy: { '/api': 'http://localhost:5080' },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // Resource hygiene: run once, never watch (scripts/check.conf).
    watch: false,
  },
})
