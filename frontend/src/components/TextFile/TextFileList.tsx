import { useEffect, useState } from "react";
import api from "../../api/axios";
import Button from "../UI/Button";
import Toast from "../UI/Toast";

export interface TextFileItem {
  id: string;
  fileName: string;
  imageId: string;
  createdAt: string;
  textStatus: string; // "Processing" or "Available"
}

export default function TextFileList() {
  const [files, setFiles] = useState<TextFileItem[]>([]);
  const [error, setError] = useState<string | null>(null);

  const fetchFiles = async () => {
    try {
      const { data } = await api.get("/textfile");
      setFiles(data);
    } catch (err: any) {
      setError(err.response?.data || "Failed to fetch files");
    }
  };

  const downloadFile = async (id: string, fileName: string) => {
    try {
      const res = await api.get(`/textfile/${id}`, { responseType: "blob" });
      const url = window.URL.createObjectURL(new Blob([res.data]));
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (err: any) {
      setError(err.response?.data || "Download failed");
    }
  };

  useEffect(() => {
    fetchFiles();
  }, []);

  return (
    <div className="textfile-list">
      {error && <Toast toasts={[{ id: "error1", message: error, type: "error" }]} />}
      <ul>
        {files.map((f) => (
          <li key={f.id}>
            {f.fileName} -{" "}
            <span style={{ fontWeight: "bold", color: f.textStatus === "Available" ? "green" : "orange" }}>
              {f.textStatus === "Available" ? "Ready" : "Processing"}
            </span>
            <Button
              onClick={() => downloadFile(f.id, f.fileName)}
              disabled={f.textStatus !== "Available"}
              style={{ marginLeft: "10px" }}
            >
              Download
            </Button>
          </li>
        ))}
      </ul>
    </div>
  );
}
