const KEY = 'tp_auth';

export function getAuth() {
  try { return JSON.parse(localStorage.getItem(KEY)); } catch { return null; }
}
export function saveAuth(auth) {
  try { localStorage.setItem(KEY, JSON.stringify(auth)); } catch { /* ignore */ }
}
export function clearAuth() {
  try { localStorage.removeItem(KEY); } catch { /* ignore */ }
}

export function installAuthFetch(apiBase) {
  if (window.__authFetchInstalled) return;
  window.__authFetchInstalled = true;
  const original = window.fetch.bind(window);
  window.fetch = (input, init = {}) => {
    const url = typeof input === 'string' ? input : input.url;
    const auth = getAuth();
    if (auth?.token && url.startsWith(apiBase)) {
      init = { ...init, headers: { ...(init.headers || {}), Authorization: `Bearer ${auth.token}` } };
    }
    return original(input, init);
  };
}