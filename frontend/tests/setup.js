import { vi } from 'vitest'
import { config } from '@vue/test-utils'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { createPinia } from 'pinia'

if (typeof window !== 'undefined' && !window.visualViewport) {
  window.visualViewport = {
    width: window.innerWidth,
    height: window.innerHeight,
    scale: 1,
    offsetLeft: 0,
    offsetTop: 0,
    pageLeft: 0,
    pageTop: 0,
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false
  }
}

const vuetify = createVuetify({ components, directives })
config.global.plugins = [vuetify, createPinia()]

window.alert = vi.fn()
window.confirm = vi.fn()
