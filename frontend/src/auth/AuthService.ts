const ACCESS_TOKEN_KEY = "accessToken";
const LOGOUT_EVENT_KEY = "logout";

// In-memory access token for instant reads
let inMemoryToken: string | null = null;

export function getAccessToken() {
  return inMemoryToken || localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function setAccessToken(token: string) {
  inMemoryToken = token;
  localStorage.setItem(ACCESS_TOKEN_KEY, token);
}

export function clearAccessToken() {
  inMemoryToken = null;
  localStorage.removeItem(ACCESS_TOKEN_KEY);
}

/**
 * Full logout across tabs
 */
export function logout() {
  clearAccessToken();
  localStorage.setItem(LOGOUT_EVENT_KEY, Date.now().toString());
  window.location.href = "/";
}

/**
 * Listen for cross-tab logout
 */
export function listenLogout(callback: () => void) {
  const handler = (e: StorageEvent) => {
    if (e.key === LOGOUT_EVENT_KEY) callback();
  };
  window.addEventListener("storage", handler);
  return () => window.removeEventListener("storage", handler);
}
