interface ImportMetaEnv {
  readonly VITE_BENTO_GATEWAY_BASE_URL?: string;
  readonly VITE_BENTO_CACHE_ADMIN_KEY?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
