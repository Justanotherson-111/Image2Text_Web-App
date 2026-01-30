import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  type ReactNode,
} from "react";
import api from "../api/axios";
import {
  setAccessToken,
  getAccessToken,
  clearAccessToken,
} from "./AuthService";

interface User {
  username: string;
  email?: string;
  role: "Admin" | "User";
}

interface AuthContextProps {
  user: User | null;
  isLoading: boolean;
  login: (u: string, p: string) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextProps | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshUser = useCallback(async () => {
    if (!getAccessToken()) {
      setUser(null);
      return;
    }

    try {
      const { data } = await api.get("/auth/me");
      setUser(data);
    } catch {
      clearAccessToken();
      setUser(null);
    }
  }, []);

  // ✅ Bootstrap ONCE
  useEffect(() => {
    (async () => {
      await refreshUser();
      setIsLoading(false);
    })();
  }, [refreshUser]);

  const login = useCallback(async (username: string, password: string) => {
    const { data } = await api.post("/auth/login", { username, password });
    setAccessToken(data.accessToken);
    await refreshUser();
  }, [refreshUser]);

  const logout = useCallback(() => {
    clearAccessToken();
    setUser(null);
    window.location.href = "/";
  }, []);

  return (
    <AuthContext.Provider
      value={{ user, isLoading, login, logout, refreshUser }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
