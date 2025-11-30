import { Routes, Route, Navigate } from "react-router-dom";
import { useAuth } from "./auth/AuthContext";
import ProtectedRoute from "./auth/ProtectedRoute";
import Layout from "./components/Layout/Layout";

// Pages
import Login from "./pages/Auth/Login";
import Register from "./pages/Auth/Register";
import Dashboard from "./pages/Dashboard/Dashboard";
import Profile from "./pages/Profile/Profile";
import AdminPanel from "./pages/Admin/AdminPanel";
import NotFound from "./pages/NotFound";
import ImageUpload from "./pages/Dashboard/ImageUpload";
import ExtractedText from "./pages/Dashboard/ExtractedText";

export default function App() {
  const { user } = useAuth();

  return (
    <Routes>
      {/* Auth routes */}
      <Route path="/" element={!user ? <Login /> : <Navigate to="/dashboard" />} />
      <Route path="/register" element={!user ? <Register /> : <Navigate to="/dashboard" />} />

      {/* Protected routes */}
      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/image-upload" element={<ImageUpload />} />
          <Route path="/extracted-text" element={<ExtractedText/>} />
        </Route>
      </Route>

      {/* Admin routes */}
      <Route element={<ProtectedRoute roles={["Admin"]} />}>
        <Route element={<Layout />}>
          <Route path="/admin" element={<AdminPanel />} />
        </Route>
      </Route>

      {/* fallback */}
      <Route path="*" element={<NotFound />} />
    </Routes>
  );
}
