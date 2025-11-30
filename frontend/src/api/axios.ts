import axios, { type AxiosRequestHeaders } from "axios";
import { getAccessToken, setAccessToken, logout } from "../auth/AuthService";

const api = axios.create({
  baseURL: "/api",
  withCredentials: true, // send HttpOnly refresh cookie
});

let isRefreshing = false;
let failedQueue: { resolve: (value?: any) => void; reject: (error: any) => void }[] = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((p) => {
    if (error) p.reject(error);
    else p.resolve(token);
  });
  failedQueue = [];
};

api.interceptors.request.use((config) => {
  const token = getAccessToken();

  if (!config.headers) config.headers = {} as AxiosRequestHeaders;

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (res) => res,
  async (err) => {
    const originalRequest = err.config;
    if (err.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return axios(originalRequest);
        });
      }
      originalRequest._retry = true;
      isRefreshing = true;
      try {
        const { data } = await axios.post("/api/auth/refresh", null, { withCredentials: true });
        setAccessToken(data.accessToken);
        api.defaults.headers.common.Authorization = `Bearer ${data.accessToken}`;
        processQueue(null, data.accessToken);
        return api(originalRequest);
      } catch (e) {
        processQueue(e, null);
        logout();
        return Promise.reject(e);
      } finally {
        isRefreshing = false;
      }
    }
    return Promise.reject(err);
  }
);

export default api;
