import react from '@vitejs/plugin-react';

// Dùng 127.0.0.1 thay vì localhost để tránh IPv6 (::1) gây lỗi proxy kiểu "Parse Error: Data after Connection: close" với một số bản Node/Kestrel.
const API_ORIGIN = 'http://127.0.0.1:5142';

/** @type {import('vite').UserConfig} */
export default {
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: API_ORIGIN,
        changeOrigin: true,
        rewrite: (path) => path,
      },
      '/uploads': {
        target: API_ORIGIN,
        changeOrigin: true,
        rewrite: (path) => path,
      },
      '/messageHub': {
        target: API_ORIGIN,
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path,
        logLevel: 'debug'
      },
      '/notificationHub': {
        target: API_ORIGIN,
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path,
        logLevel: 'debug'
      },
      '/postHub': {
        target: API_ORIGIN,
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path,
        logLevel: 'debug'
      },
      '/storyHub': {
        target: API_ORIGIN,
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path,
        logLevel: 'debug'
      },
      '/commentHub': {
        target: API_ORIGIN,
        changeOrigin: true,
        ws: true,
        rewrite: (path) => path,
        logLevel: 'debug'
      },
    },
  },
};
