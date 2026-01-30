import axios, {
  AxiosHeaders,
  type InternalAxiosRequestConfig,
} from "axios";
import {
  getAccessToken,
  setAccessToken,
  logout,
} from "../auth/AuthService";

/* ================= TYPES ================= */

interface CustomAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

/* ================= AXIOS INSTANCE ================= */

const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
  timeout: 15000,
});

/* ================= SINGLE-FLIGHT REFRESH ================= */

let refreshPromise: Promise<string> | null = null;

/* ================= REQUEST INTERCEPTOR ================= */

api.interceptors.request.use((config: CustomAxiosRequestConfig) => {
  const token = getAccessToken();

  if (!(config.headers instanceof AxiosHeaders)) {
    config.headers = AxiosHeaders.from(config.headers);
  }

  // 🚫 Never attach token to refresh endpoint
  if (token && !config.url?.includes("/auth/refresh")) {
    config.headers.set("Authorization", `Bearer ${token}`);
  }

  return config;
});

/* ================= RESPONSE INTERCEPTOR ================= */

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as CustomAxiosRequestConfig | undefined;

    // Only handle 401 with a valid request
    if (!originalRequest || error.response?.status !== 401) {
      return Promise.reject(error);
    }

    // 🚫 Absolute hard-stop: never refresh the refresh call
    if (originalRequest.url?.includes("/auth/refresh")) {
      logout();
      return Promise.reject(error);
    }

    // 🚫 Never retry twice
    if (originalRequest._retry) {
      logout();
      return Promise.reject(error);
    }

    originalRequest._retry = true;

    try {
      // ================= SINGLE REFRESH =================
      if (!refreshPromise) {
        refreshPromise = api
          .post("/auth/refresh")
          .then((res) => {
            const newToken: string | undefined = res?.data?.accessToken;
            if (!newToken) throw new Error("No access token");

            setAccessToken(newToken);
            return newToken;
          })
          .catch((err) => {
            logout();
            throw err;
          })
          .finally(() => {
            refreshPromise = null;
          });
      }

      const newAccessToken = await refreshPromise;

      // Retry original request with new token
      originalRequest.headers = AxiosHeaders.from(originalRequest.headers);
      originalRequest.headers.set(
        "Authorization",
        `Bearer ${newAccessToken}`
      );

      return api(originalRequest);
    } catch (err) {
      return Promise.reject(err);
    }
  }
);

export default api;
