import { useState } from "react";
import UploadBox from "../../components/Image/UploadBox";
import ImageList from "../../components/Image/ImageList";

export default function Dashboard() {
  const [refreshTrigger, setRefreshTrigger] = useState(0);

  // Trigger ImageList refresh after upload
  const handleUploadSuccess = () => {
    setRefreshTrigger((prev) => prev + 1);
  };

  return (
    <div className="dashboard-page">
      <h1>Dashboard</h1>
      <UploadBox onUploadSuccess={handleUploadSuccess} />
      <ImageList refreshTrigger={refreshTrigger} />
    </div>
  );
}
