import { useEffect, useState } from "react";
import api from "../../api/axios";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import Toast from "../UI/Toast";
import { OCR_LANGUAGES, OCR_MODELS, type OcrLanguage, type OcrModel } from "./OcrModel";

export default function UploadBox({
  selectedSection,
  onUploadSuccess,
  ocrLanguage,
  onLanguageChange,
  ocrModel,
  onModelChange,
}: {
  selectedSection: string | null;
  onUploadSuccess: (newImages: any[]) => void;
  ocrLanguage: OcrLanguage;
  onLanguageChange: (l: OcrLanguage) => void;
  ocrModel: OcrModel;
  onModelChange: (m: OcrModel) => void;
}) {
  const [files, setFiles] = useState<File[]>([]);
  const [previews, setPreviews] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const urls = files.map((f) => URL.createObjectURL(f));
    setPreviews(urls);
    return () => urls.forEach(URL.revokeObjectURL);
  }, [files]);

  const handleUpload = async () => {
    if (!files.length) return setError("Select files first");
    if (!selectedSection) return setError("Select a section first");

    setLoading(true);
    setError(null);

    try {
      const formData = new FormData();
      files.forEach((f) => formData.append("files", f));
      formData.append("sectionId", selectedSection);
      formData.append("language", ocrLanguage);
      formData.append("model", ocrModel);

      const res = await api.post("/image/upload", formData);
      setFiles([]);
      setPreviews([]);
      onUploadSuccess(res.data);
    } catch {
      setError("Upload failed");
    } finally {
      setLoading(false);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(e.target.files ?? []);
    // Optional: limit max files
    if (selectedFiles.length > 10) {
      setError("Maximum 10 files allowed");
      return;
    }
    setFiles(selectedFiles);
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Upload Images</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          {/* OCR Language */}
          <div className="space-y-1">
            <p className="text-sm font-medium">OCR Language</p>
            <select
              className="w-full border rounded px-2 py-1 text-sm"
              value={ocrLanguage}
              onChange={(e) => onLanguageChange(e.target.value as OcrLanguage)}
            >
              {OCR_LANGUAGES.map((lang) => (
                <option key={lang.value} value={lang.value}>
                  {lang.label}
                </option>
              ))}
            </select>
          </div>

          {/* OCR Model */}
          <div className="space-y-1">
            <p className="text-sm font-medium">OCR Model</p>
            <select
              className="w-full border rounded px-2 py-1 text-sm"
              value={ocrModel}
              onChange={(e) => onModelChange(e.target.value as OcrModel)}
            >
              {OCR_MODELS.map((model) => (
                <option key={model.value} value={model.value}>
                  {model.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {error && <Toast toasts={[{ id: "uerr", message: error, type: "error" }]} />}

        <input type="file" multiple onChange={handleFileChange} />

        {previews.length > 0 && (
          <div className="grid grid-cols-5 gap-2">
            {previews.map((src, i) => (
              <img key={i} src={src} className="h-16 w-full object-cover rounded border" alt={`Preview ${i}`} />
            ))}
          </div>
        )}

        <div className="flex justify-end">
          <Button onClick={handleUpload} disabled={loading || !selectedSection || !files.length}>
            {loading ? "Uploading..." : "Upload"}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
