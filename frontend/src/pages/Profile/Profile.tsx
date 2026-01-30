import { useAuth } from "../../auth/AuthContext";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export default function Profile() {
  const { user, isLoading } = useAuth();

  if (isLoading) return <div>Loading profile...</div>;
  if (!user) return <div>No user logged in.</div>;

  return (
    <div className="mx-auto max-w-xl">
      <Card>
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl font-semibold">
            Profile
          </CardTitle>
          <p className="text-sm text-muted-foreground">
            Your account information
          </p>
        </CardHeader>

        <CardContent className="space-y-4">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Username</span>
            <span className="font-medium">{user.username}</span>
          </div>

          {user.email && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Email</span>
              <span className="font-medium">{user.email}</span>
            </div>
          )}

          <div className="flex justify-between">
            <span className="text-muted-foreground">Role</span>
            <span className="font-medium">{user.role}</span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
