import { useState } from "react";
import { useNavigate } from "react-router-dom";
import api from "../../api/axios";
import Button from "../../components/UI/Button";
import Input from "../../components/UI/Input";
import Toast from "../../components/UI/Toast";

export default function Register() {
    const navigate = useNavigate();
    const [username, setUsername] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setLoading(true);
        try {
            await api.post("/auth/register", { username, email, password });
            navigate("/");
        } catch (err: any) {
            setError(err.response?.data || "Registration failed");
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="register-page">
            <h1>Register</h1>
            {error && <Toast toasts={[{ id: "error1", message: error, type: "error" }]} />}
            <form onSubmit={handleSubmit}>
                <Input placeholder="Username" value={username} onChange={(e) => setUsername(e.target.value)} />
                <Input placeholder="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
                <Input placeholder="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
                <Button type="submit" disabled={loading}>{loading ? "Registering..." : "Register"}</Button>
            </form>
        </div>
    );
}
