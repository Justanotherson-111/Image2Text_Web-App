import { useEffect, useState } from "react";
import api from "../../api/axios";
import Toast from "../../components/UI/Toast";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Card, CardContent } from "@/components/ui/card";

export default function AdminPanel() {
  const [users, setUsers] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);

  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);

  useEffect(() => {
    api.get(`/admin/users?page=${page}&pageSize=${pageSize}`)
      .then(res => {
        setUsers(res.data.users);
        setTotal(res.data.total)
      })
      .catch(() => setError("Failed to load users"));
  }, [page, pageSize]);

  const deleteUser = async (id: string) => {
    if (!confirm("Are you sure you want to delete this user?")) return;
    await api.delete(`/admin/user/${id}`);
    setUsers(prev => prev.filter(u => u.id !== id));
  };

  return (
    <div className="mx-auto max-w-6xl space-y-8">
      <div className="space-y-1">
        <h1 className="text-3xl font-semibold tracking-tight">Admin Panel</h1>
        <p className="text-sm text-muted-foreground">
          Manage application users and permissions
        </p>
      </div>

      {error && (
        <Toast toasts={[{ id: "err", message: error, type: "error" }]} />
      )}

      <Card className="overflow-hidden">
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow className="bg-muted/40">
                <TableHead className="pl-6">Username</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Role</TableHead>
                <TableHead className="pr-6 text-right">Action</TableHead>
              </TableRow>
            </TableHeader>

            <TableBody>
              {users.map(u => (
                <TableRow key={u.id} className="hover:bg-muted/40">
                  <TableCell className="pl-6 font-medium">
                    {u.username}
                  </TableCell>
                  <TableCell>{u.email}</TableCell>
                  <TableCell>
                    <span className="rounded-md bg-secondary px-2 py-0.5 text-xs font-medium">
                      {u.userRole}
                    </span>
                  </TableCell>
                  <TableCell className="pr-6 text-right">
                    <Button
                      variant="destructive"
                      size="sm"
                      disabled={u.userRole === "Admin"}
                      className={
                        u.userRole === "Admin"
                          ? "pointer-events-none opacity-40"
                          : ""
                      }
                      onClick={() => deleteUser(u.id)}
                    >
                      Delete
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <div className="flex justify-between mt-2">
            <Button disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
            <span>{page} / {Math.ceil(total / pageSize)}</span>
            <Button disabled={page >= Math.ceil(total / pageSize)} onClick={() => setPage(p => p + 1)}>Next</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
