import { useEffect, useRef, useState } from "react";
import api from "../../api/axios";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import Toast from "../UI/Toast";
import TextEditorModal from "./TextEditorModal";

export default function TextFileList({
  documentId,
  sectionId,
  refreshTrigger,
}: {
  documentId: string | null;
  sectionId: string | null;
  refreshTrigger: number;
}) {
  const [files, setFiles] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);
  const pollingRef = useRef<NodeJS.Timeout | null>(null);
  const [editing, setEditing] = useState<any | null>(null);

  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);

  const fetchFiles = async () => {
    if (!documentId || !sectionId) return;

    try {
      const { data } = await api.get(
        `/textfile/document/${documentId}?sectionId=${sectionId}&page=${page}&pageSize=${pageSize}`
      );
      setFiles(data.files);
      setTotal(data.total);
    } catch {
      setError("Failed to load text files");
    }
  };

  useEffect(() => {
    fetchFiles();
  }, [documentId, sectionId, refreshTrigger, page]);

  // Correct OCR polling logic
  useEffect(() => {
    if (!files.length) return;

    const hasPendingOcr = files.some(f => !f.previewText);

    if (!hasPendingOcr) {
      pollingRef.current && clearInterval(pollingRef.current);
      pollingRef.current = null;
      return;
    }

    if (!pollingRef.current) {
      pollingRef.current = setInterval(fetchFiles, 3000);
    }

    return () => {
      pollingRef.current && clearInterval(pollingRef.current);
      pollingRef.current = null;
    };
  }, [files]);

  const download = async (id: string, name: string) => {
    try {
      const res = await api.get(`/textfile/${id}`, { responseType: "blob" });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement("a");
      a.href = url;
      a.download = name;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setError("Failed to download file");
    }
  };

  if (!sectionId) return null;

  return (
    <>
      <div className="space-y-6">
        {error && (
          <Toast toasts={[{ id: "terr", message: error, type: "error" }]} />
        )}

        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>File</TableHead>
              <TableHead>Preview</TableHead>
              <TableHead className="text-right">Download</TableHead>
            </TableRow>
          </TableHeader>

          <TableBody>
            {files.map((f) => (
              <TableRow key={f.id}>
                <TableCell className="font-medium truncate max-w-[200px]">
                  {f.fileName}
                </TableCell>

                <TableCell
                  className="max-w-[320px] cursor-pointer"
                  onClick={() => f.previewText && setEditing(f)}
                >
                  {f.previewText ? (
                    <p className="text-sm text-muted-foreground line-clamp-3 whitespace-pre-wrap hover:underline">
                      {f.previewText}
                    </p>
                  ) : (
                    <span className="text-xs italic text-muted-foreground">
                      OCR processing…
                    </span>
                  )}
                </TableCell>

                <TableCell className="text-right">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => download(f.id, f.fileName)}
                    disabled={!f.previewText}
                  >
                    Download
                  </Button>
                </TableCell>
              </TableRow>
            ))}

            {files.length === 0 && (
              <TableRow>
                <TableCell colSpan={3} className="text-center text-muted-foreground">
                  No extracted text for this section
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
        <div className="flex justify-between mt-2">
          <Button disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
          <span>{page} / {Math.ceil(total / pageSize)}</span>
          <Button disabled={page >= Math.ceil(total / pageSize)} onClick={() => setPage(p => p + 1)}>Next</Button>
        </div>
      </div>

      {/* Modal rendered OUTSIDE table */}
      {editing && (
        <TextEditorModal
          textFileId={editing.id}
          fileName={editing.fileName}
          onClose={() => setEditing(null)}
          onSaved={fetchFiles}
        />
      )}
    </>
  );
}
