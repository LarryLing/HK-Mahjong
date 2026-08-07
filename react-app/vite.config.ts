import { defineConfig } from "vite";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import babel from "@rolldown/plugin-babel";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  server: {
    allowedHosts: [".trycloudflare.com"],
    headers: {
      "Cross-Origin-Opener-Policy": "same-origin",
      "Cross-Origin-Embedder-Policy": "require-corp",
    },
    fs: {
      allow: [".."],
    },
  },
  build: {
    chunkSizeWarningLimit: 2000,
    assetsInlineLimit: 0,
    rollupOptions: {
      output: {
        assetFileNames: (assetInfo) => {
          const primaryName = assetInfo.names?.[0] || "";

          if (primaryName.includes("unity-build")) {
            return "unity-build/[name].[ext]";
          }
          return "assets/[name]-[hash].[ext]";
        },
      },
    },
  },
});
