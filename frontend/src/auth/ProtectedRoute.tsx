import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { useEffect, useState } from "react";

interface ProtectedRouteProps {
  roles?: ("Admin" | "User")[];
}

export default function ProtectedRoute({ roles }: ProtectedRouteProps) {
  const { user, checkAuth, isLoading } = useAuth();
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const init = async () => {
      await checkAuth(); // only runs for protected/admin routes
      setReady(true);
    };
    init();
  }, []);

  if (!ready || isLoading) return <div>Loading...</div>;

  if (!user) return <Navigate to="/" />; // not logged in

  if (roles && !roles.includes(user.role)) return <Navigate to="/dashboard" />; // role guard

  return <Outlet />;
}
