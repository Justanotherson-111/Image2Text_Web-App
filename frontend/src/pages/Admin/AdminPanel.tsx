import { useEffect, useState } from "react";
import api from "../../api/axios";
import Button from "../../components/UI/Button";
import Toast from "../../components/UI/Toast";

interface User {
    id: string;
    username: string;
    email: string;
    userRole: "Admin" | "User";
}

export default function AdminPanel() {
    const [users, setUsers] = useState<User[]>([]);
    const [error, setError] = useState<string | null>(null);

    const fetchUsers = async () => {
        try {
            const { data } = await api.get("/admin/users");
            setUsers(data);
        } catch (err: any) {
            setError(err.response?.data || "Failed to fetch users");
        }
    };

    const deleteUser = async (id: string) => {
        if (!confirm("Delete this user?")) return;
        try {
            await api.delete(`/admin/user/${id}`);
            fetchUsers();
        } catch (err: any) {
            setError(err.response?.data || "Delete failed");
        }
    };

    useEffect(() => {
        fetchUsers();
    }, []);

    return (
        <div className="admin-panel">
            <h1>Admin Panel</h1>
            {error && <Toast toasts={[{ id: "error1", message: error, type: "error" }]} />}
            <table>
                <thead>
                    <tr><th>Username</th><th>Email</th><th>Role</th><th>Action</th></tr>
                </thead>
                <tbody>
                    {users.map((u) => (
                        <tr key={u.id}>
                            <td>{u.username}</td>
                            <td>{u.email}</td>
                            <td>{u.userRole}</td>
                            <td><Button onClick={() => deleteUser(u.id)}>Delete</Button></td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
