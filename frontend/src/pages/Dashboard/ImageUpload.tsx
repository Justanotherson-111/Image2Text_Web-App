import { useState } from "react";
import UploadBox from "../../components/Image/UploadBox";
import ImageList from "../../components/Image/ImageList";
import Toast from "../../components/UI/Toast";
import { useAuth } from "../../auth/AuthContext";

export default function ImageUpload() {
  const { isLoading, user } = useAuth();
  const [success, setSuccess] = useState(false);
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  if (isLoading) return <p>Loading...</p>;
  if (!user) return <p>Please login to upload images</p>;

  const handleUploadSuccess = () => {
    setSuccess(true);
    setRefreshTrigger(prev => prev + 1);
  };

  return (
    <div className="image-upload">
      {success && (
        <Toast toasts={[{ id: "success1", message: "Upload successful", type: "success" }]} />
      )}

      <UploadBox onUploadSuccess={handleUploadSuccess} />

      <h3>Your Images</h3>
      <ImageList refreshTrigger={refreshTrigger} />
    </div>
  );
}
