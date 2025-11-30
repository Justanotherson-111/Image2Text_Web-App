import { useState, useEffect } from "react";
import api from "../../api/axios";
import Button from "../UI/Button";
import Toast from "../UI/Toast";
import { useAuth } from "../../auth/AuthContext";
import { startSignalR, joinImageRoom } from "../../services/signalr";

export default function UploadBox({ onUploadSuccess }: { onUploadSuccess: (imageId: string) => void }) {
  const { user, isLoading, hasValidToken, validateToken } = useAuth();
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    validateToken().catch(() => {});
  }, [validateToken]);

  const handleUpload = async () => {
    if (!file) return setError("Please select a file");
    setLoading(true);
    setError(null);

    try {
      await startSignalR();

      const formData = new FormData();
      formData.append("file", file);
      const { data } = await api.post("/image/upload", formData);

      const imageId = data.id;
      onUploadSuccess(imageId);
      setFile(null);

      joinImageRoom(imageId).catch(err => console.error("Failed to join SignalR room:", err));
    } catch (err: any) {
      setError(err.response?.data || "Upload failed");
    } finally {
      setLoading(false);
    }
  };

  if (isLoading) return <p>Loading...</p>;
  if (!user) return <p>Please login to upload images</p>;

  return (
    <div className="upload-box">
      {error && <Toast toasts={[{ id: "error1", message: error, type: "error" }]} />}
      <input type="file" onChange={(e) => setFile(e.target.files?.[0] || null)} />
      <Button onClick={handleUpload} disabled={loading || !hasValidToken}>
        {loading ? "Uploading..." : hasValidToken ? "Upload" : "Validating..."}
      </Button>
    </div>
  );
}
