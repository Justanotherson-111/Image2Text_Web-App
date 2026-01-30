import { useState } from "react";
import DashboardLayout from "@/components/Layout/DashboardLayout";
import DocumentList from "@/components/Document/DocumentList";
import UploadBox from "@/components/Image/UploadBox";
import ImageList from "@/components/Image/ImageList";
import Toast, { type ToastItem } from "@/components/UI/Toast";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Upload, Image as ImageIcon } from "lucide-react";
import { motion } from "framer-motion";
import { type OcrLanguage, type OcrModel } from "@/components/Image/OcrModel";

export default function ImageUpload() {
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [selectedSection, setSelectedSection] = useState<string | null>(null);
  const [selectedDocument, setSelectedDocument] = useState<string | null>(null);
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const [newImages, setNewImages] = useState<any[]>([]);
  const [OcrLanguage, setOcrLanguage] = useState<OcrLanguage>("eng");
  const [ocrModel, setOcrModel] = useState<OcrModel>("tesseract");

  const notify = (message: string, type: ToastItem["type"]) =>
    setToasts(t => [...t, { id: Date.now().toString(), message, type }]);

  const handleUploadSuccess = (imgs: any[]) => {
    setNewImages(imgs);
    setRefreshTrigger(v => v + 1);
    notify("Images uploaded successfully", "success");
  };

  return (
    <DashboardLayout
      sidebar={
        <Card>
          <CardHeader>
            <CardTitle>Documents</CardTitle>
          </CardHeader>
          <CardContent>
            <DocumentList
              refreshTrigger={refreshTrigger}
              onSectionSelect={setSelectedSection}
              onDocumentSelect={setSelectedDocument}
              selectedSectionId={selectedSection}
              selectedDocumentId={selectedDocument}
            />
          </CardContent>
        </Card>
      }
    >
      {!selectedSection ? (
        <Card className="h-[50vh] flex items-center justify-center">
          <CardContent className="text-center text-muted-foreground space-y-2">
            <Upload className="mx-auto h-8 w-8 opacity-40" />
            <p>Select a section to upload images</p>
          </CardContent>
        </Card>
      ) : (
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} className="space-y-6">
          {/* Upload */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Upload className="h-4 w-4 text-primary" />
                Upload Images
              </CardTitle>
            </CardHeader>
            <CardContent>
              <UploadBox
                selectedSection={selectedSection}
                onUploadSuccess={handleUploadSuccess}
                ocrLanguage={OcrLanguage}
                onLanguageChange={setOcrLanguage}
                ocrModel={ocrModel}
                onModelChange={setOcrModel}
              />
            </CardContent>
          </Card>

          {/* Uploaded Images */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ImageIcon className="h-4 w-4" />
                Uploaded Images
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ImageList 
              sectionId={selectedSection} 
              refreshTrigger={refreshTrigger} 
              newImages={newImages} 
              ocrLanguage={OcrLanguage} 
              ocrModel={ocrModel}
              />
            </CardContent>
          </Card>
        </motion.div>
      )}

      <Toast toasts={toasts} />
    </DashboardLayout>
  );
}
