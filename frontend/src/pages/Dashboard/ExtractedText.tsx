import { useState } from "react";
import DashboardLayout from "@/components/Layout/DashboardLayout";
import DocumentList from "@/components/Document/DocumentList";
import TextFileList from "@/components/TextFile/TextFileList";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { FileText } from "lucide-react";
import { motion } from "framer-motion";
import { Button } from "@/components/ui/button";
import api from "@/api/axios";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export default function ExtractedText() {
  const [selectedSection, setSelectedSection] = useState<string | null>(null);
  const [selectedDocument, setSelectedDocument] = useState<string | null>(null);
  const [downloading, setDownloading] = useState(false);

  type ExportFormat = "docx" | "pdf";

  const downloadCombined = async (
    documentId: string,
    format: ExportFormat
  ) => {
    const endpoint =
      format === "pdf"
        ? `/textfile/document/${documentId}/combine-pdf`
        : `/textfile/document/${documentId}/combine`;

    const res = await api.get(endpoint, { responseType: "blob" });

    const contentDisposition = res.headers["content-disposition"];
    let fileName = `document.${format}`;

    if (contentDisposition) {
      const match = contentDisposition.match(
        /filename\*?=(?:UTF-8'')?"?([^"]+)"?/i
      );
      if (match?.[1]) {
        fileName = decodeURIComponent(match[1]);
      }
    }

    const blobUrl = URL.createObjectURL(res.data);
    const a = document.createElement("a");
    a.href = blobUrl;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();

    setTimeout(() => {
      URL.revokeObjectURL(blobUrl);
      document.body.removeChild(a);
    }, 100);
  };

  const combineAndDownload = async (format: ExportFormat) => {
    if (!selectedDocument) return;

    try {
      setDownloading(true);
      await downloadCombined(selectedDocument, format);
    } catch (err: any) {
      if (err.response?.status === 404) {
        alert("Document not found or access denied");
      } else {
        alert("Failed to download document");
      }
    } finally {
      setDownloading(false);
    }
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
              refreshTrigger={0}
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
            <FileText className="mx-auto h-8 w-8 opacity-40" />
            <p>Select a section to view OCR results</p>
          </CardContent>
        </Card>
      ) : (
        <motion.div initial={{ opacity: 0, y: 12 }} animate={{ opacity: 1, y: 0 }} className="space-y-6">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <CardTitle className="flex items-center gap-2">
                <FileText className="h-4 w-4 text-primary" />
                Extracted Text
              </CardTitle>

              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button
                    size="sm"
                    disabled={!selectedDocument || downloading}
                    className="flex items-center gap-2"
                  >
                    {downloading ? "Exporting..." : "Export"}
                  </Button>
                </DropdownMenuTrigger>

                <DropdownMenuContent align="end">
                  <DropdownMenuItem
                    onClick={() => combineAndDownload("docx")}
                    disabled={downloading}
                  >
                    Export as DOCX
                  </DropdownMenuItem>

                  <DropdownMenuItem
                    onClick={() => combineAndDownload("pdf")}
                    disabled={downloading}
                  >
                    Export as PDF
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
              
            </CardHeader>
            <CardContent>
              <TextFileList
                documentId={selectedDocument}
                sectionId={selectedSection}
                refreshTrigger={0}
              />
            </CardContent>
          </Card>
        </motion.div>
      )}
    </DashboardLayout>
  );
}
