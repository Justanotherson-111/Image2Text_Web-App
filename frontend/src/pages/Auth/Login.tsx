import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import Button from "../../components/UI/Button";
import Input from "../../components/UI/Input";
import Toast from "../../components/UI/Toast";
import AuthContainer from "../../components/UI/AuthContainer";

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(username, password);
      navigate("/dashboard");
    } catch (err: any) {
      setError(err.response?.data || "Login failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <AuthContainer>
      <h1 style={{ textAlign: "center", marginBottom: 20 }}>Login</h1>
      {error && <Toast toasts={[{ id: "error1", message: error, type: "error" }]} />}
      <form onSubmit={handleSubmit}>
        <Input placeholder="Username" value={username} onChange={(e) => setUsername(e.target.value)} />
        <Input placeholder="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
        <Button type="submit" disabled={loading}>{loading ? "Logging in..." : "Login"}</Button>
      </form>
      <p style={{ textAlign: "center", marginTop: 12 }}>
        Don't have an account? <Link to="/register" style={{ color: "#2563eb" }}>Register</Link>
      </p>
    </AuthContainer>
  );
}
