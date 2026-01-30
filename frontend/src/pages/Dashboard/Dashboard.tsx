import { useEffect, useState } from "react";
import DashboardLayout from "@/components/Layout/DashboardLayout";
import DocumentList from "@/components/Document/DocumentList";
import UploadBox from "@/components/Image/UploadBox";
import ImageList from "@/components/Image/ImageList";
import TextFileList from "@/components/TextFile/TextFileList";
import Toast, { type ToastItem } from "@/components/UI/Toast";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Upload, Image as ImageIcon, FileText } from "lucide-react";
import { motion } from "framer-motion";
import DocumentSearch from "@/components/Search/DocumentSearch";
import { type OcrSummary, getOcrSummary } from "@/components/DashboardSummary/dashboard";
import OcrSummaryCard from "@/components/DashboardSummary/OcrSummaryCard";
import type { OcrModel, OcrLanguage } from "@/components/Image/OcrModel";

const EMPTY_SUMMARY: OcrSummary = {
  total: 0,
  completed: 0,
  processing: 0,
  failed: 0,
};
export default function Dashboard() {
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [textRefreshTrigger, setTextRefreshTrigger] = useState(0);
  const [selectedSection, setSelectedSection] = useState<string | null>(null);
  const [selectedDocument, setSelectedDocument] = useState<string | null>(null);
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const [newImages, setNewImages] = useState<any[]>([]);
  const [OcrLanguage, setOcrLanguage] = useState<OcrLanguage>("eng");
  const [ocrModel, setOcrModel] = useState<OcrModel>("tesseract");

  const [ocrSummary, setOcrSummary] = useState<OcrSummary>(EMPTY_SUMMARY);
  const [loadingSummary, setLoadingSummary] = useState(false);

  const notify = (message: string, type: ToastItem["type"]) =>
    setToasts(t => [...t, { id: Date.now().toString(), message, type }]);
  const handleOcrRerun = () => {
    setTextRefreshTrigger(v => v + 1); //Text
    setRefreshTrigger(v => v + 1); //Image
  };
  const handleUploadSuccess = (imgs: any[]) => {
    setNewImages(imgs);
    setRefreshTrigger(v => v + 1);
    setTextRefreshTrigger(v => v + 1);
    notify("Images uploaded successfully", "success");

    // Clear after next tick
    setTimeout(() => setNewImages([]), 0);
  };
  useEffect(() => {
    if (!selectedDocument) {
      setOcrSummary(EMPTY_SUMMARY);
      return;
    }

    let cancelled = false;
    let interval: ReturnType<typeof setInterval> | null = null;

    const fetchSummary = async () => {
      try {
        setLoadingSummary(true);
        const data = await getOcrSummary(selectedDocument);
        if (!cancelled) {
          setOcrSummary(data);

          // keep polling if still processing
          if (data?.processing && data.processing > 0 && !interval) {
            interval = setInterval(fetchSummary, 3000);
          }

          // stop polling when done
          if ((data?.processing ?? 0) === 0 && interval) {
            clearInterval(interval);
            interval = null;
          }
        }
      } finally {
        if (!cancelled) {
          setLoadingSummary(false);
        }
      }
    };

    fetchSummary();

    return () => {
      cancelled = true;
      if (interval) clearInterval(interval);
    };
  }, [selectedDocument, refreshTrigger, textRefreshTrigger]);
  return (
    <DashboardLayout
      sidebar={
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileText className="h-5 w-5 opacity-70" />
              Documents
            </CardTitle>
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
          <CardContent className="text-muted-foreground text-center space-y-2">
            <FileText className="mx-auto h-8 w-8 opacity-40" />
            <p>Select a section to start working</p>
          </CardContent>
        </Card>
      ) : (
        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          className="grid grid-cols-12 gap-6"
        >
          {/* LEFT: Main workspace */}
          <div className="col-span-12 xl:col-span-8 space-y-6">
            {/* Upload */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Upload className="h-5 w-5 opacity-70" />
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

            {/* Images */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <ImageIcon className="h-5 w-5 opacity-70" />
                  Images
                </CardTitle>
              </CardHeader>
              <CardContent>
                <ImageList
                  sectionId={selectedSection}
                  refreshTrigger={refreshTrigger}
                  newImages={newImages}
                  onImagesChanged={() => setTextRefreshTrigger(v => v + 1)}
                  onOcrRerun={handleOcrRerun}
                  ocrLanguage={OcrLanguage}
                  ocrModel={ocrModel}
                />
              </CardContent>
            </Card>

            {/* Extracted Text */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <FileText className="h-5 w-5 opacity-70" />
                  Extracted Text
                </CardTitle>
              </CardHeader>
              <CardContent>
                <TextFileList
                  documentId={selectedDocument}
                  sectionId={selectedSection}
                  refreshTrigger={textRefreshTrigger}
                />
              </CardContent>
            </Card>
          </div>

          {/* RIGHT: Search panel (always exists) */}
          <aside className="col-span-12 xl:col-span-4 space-y-6">
            {selectedDocument && (
              <OcrSummaryCard
                summary={ocrSummary}
                loading={loadingSummary}
              />
            )}

            {selectedDocument ? (
              <Card className="sticky top-24">
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <FileText className="h-5 w-5 opacity-70" />
                    Search OCR Text
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <DocumentSearch documentId={selectedDocument} />
                </CardContent>
              </Card>
            ) : (
              <Card className="h-[200px] flex items-center justify-center">
                <CardContent className="text-muted-foreground text-center">
                  Select a document to enable search
                </CardContent>
              </Card>
            )}
          </aside>
        </motion.div>

      )}
      <Toast toasts={toasts} />
    </DashboardLayout>
  );
}
