import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../auth/AuthContext";
import Button from "../UI/Button";
import { logout } from "../../auth/AuthService";

export default function Sidebar() {
  const { user } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    // Clear the token from memory and localStorage
    logout();

    // Redirect to login page
    navigate("/", { replace: true });
  };


  return (
    <aside
      style={{
        width: 200,
        background: "#f5f5f5",
        padding: "20px",
        display: "flex",
        flexDirection: "column",
        gap: 12,
      }}
    >
      <h2>Menu</h2>
      <ul style={{ listStyle: "none", padding: 0, margin: 0, flex: 1 }}>
        <li>
          <Link to="/dashboard">Dashboard</Link>
        </li>
        <li>
          <Link to="/profile">Profile</Link>
        </li>
        {user?.role === "Admin" && (
          <li>
            <Link to="/admin">Admin Panel</Link>
          </li>
        )}
        {/* Add new pages */}
        <li>
          <Link to="/image-upload">Upload Image</Link>
        </li>
        <li>
          <Link to="/extracted-text">Extracted Text</Link>
        </li>
      </ul>
      <Button onClick={handleLogout}>Logout</Button>
    </aside>
  );
}
