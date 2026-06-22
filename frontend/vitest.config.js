import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

const cssStub = {
  name: 'css-stub',
  enforce: 'pre',
  resolveId(source) {
    if (source.endsWith('.css') || /\.css\?/.test(source)) {
      return { id: '\0css-stub', moduleSideEffects: false }
    }
    return null
  },
  load(id) {
    if (id === '\0css-stub') {
      return 'export default {}'
    }
    return null
  }
}

export default defineConfig({
  plugins: [vue(), cssStub],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  optimizeDeps: {
    exclude: ['vuetify']
  },
  ssr: {
    noExternal: ['vuetify']
  },
  test: {
    environment: 'happy-dom',
    globals: true,
    setupFiles: ['./tests/setup.js'],
    css: false,
    server: {
      deps: {
        inline: ['vuetify']
      }
    }
  }
})
