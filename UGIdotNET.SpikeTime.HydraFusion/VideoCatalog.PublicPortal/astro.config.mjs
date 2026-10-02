import { defineConfig } from 'astro/config';
import node from '@astrojs/node';

export default defineConfig({
  output: 'server',
  adapter: node({
    mode: 'standalone',
  }),
  server: {
    host: true,
    port: 4321,
  },
  vite: {
    server: {
      hmr: {
        port: 4321,
      },
    },
  },
});
