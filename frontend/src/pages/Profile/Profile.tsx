import { useAuth } from "../../auth/AuthContext";

export default function Profile() {
  const { user, isLoading } = useAuth();

  if (isLoading) return <div>Loading profile...</div>;

  if (!user) return <div>No user logged in.</div>;

  return (
    <div className="profile-page">
      <h1>Profile</h1>
      <p><strong>Username:</strong> {user.username}</p>
      {user.email && <p><strong>Email:</strong> {user.email}</p>}
      <p><strong>Role:</strong> {user.role}</p>
    </div>
  );
}
