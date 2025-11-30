import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import api from "../api/axios";

interface User {
  username: string;
  email?: string;
  role: "Admin" | "User";
}

interface AuthContextProps {
  user: User | null;
  isLoading: boolean;
  hasValidToken: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  checkAuth: () => Promise<void>;
  validateToken: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextProps | undefined>(undefined);

const ACCESS_TOKEN_KEY = "accessToken";

export const getAccessToken = () => localStorage.getItem(ACCESS_TOKEN_KEY);
export const setAccessToken = (token: string) => localStorage.setItem(ACCESS_TOKEN_KEY, token);
export const removeAccessToken = () => localStorage.removeItem(ACCESS_TOKEN_KEY);

export const logout = () => {
  removeAccessToken();
  localStorage.setItem("logout", Date.now().toString()); // notify other tabs
};

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [hasValidToken, setHasValidToken] = useState(false);

  const fetchUser = async () => {
    try {
      const { data } = await api.get("/auth/me");
      setUser(data);
      setHasValidToken(true);
    } catch {
      setUser(null);
      setHasValidToken(false);
    }
  };

  const checkAuth = async () => {
    setIsLoading(true);
    await fetchUser();
    setIsLoading(false);
  };

  const validateToken = async (): Promise<boolean> => {
    try {
      await api.get("/auth/me");
      setHasValidToken(true);
      return true;
    } catch {
      setHasValidToken(false);
      return false;
    }
  };

  const login = async (username: string, password: string) => {
    const { data } = await api.post("/auth/login", { username, password });
    setAccessToken(data.accessToken);
    await fetchUser();
  };

  useEffect(() => {
    const handleStorage = (e: StorageEvent) => {
      if (e.key === "logout") setUser(null);
    };
    window.addEventListener("storage", handleStorage);
    return () => window.removeEventListener("storage", handleStorage);
  }, []);

  const contextValue: AuthContextProps = {
    user,
    isLoading,
    hasValidToken,
    login,
    logout,
    checkAuth,
    validateToken,
  };

  return <AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
};
