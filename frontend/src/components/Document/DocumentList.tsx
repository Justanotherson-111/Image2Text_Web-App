import { useEffect, useState } from "react";
import api from "../../api/axios";
import { Button } from "@/components/ui/button";
import SectionList from "./SectionList";
import { FileText, Trash2, Plus } from "lucide-react";

interface Document {
  id: string;
  title: string;
  sections: Section[];
}

interface Section {
  id: string;
  title: string;
}

export default function DocumentList({
  refreshTrigger,
  onSectionSelect,
  selectedSectionId,
  onDocumentSelect,
  selectedDocumentId,
  selectMode = "section",
}: {
  refreshTrigger: number;
  onSectionSelect?: (sectionId: string | null) => void;
  selectedSectionId?: string | null;
  onDocumentSelect?: (documentId: string | null) => void;
  selectedDocumentId?: string | null;
  selectMode?: "section" | "document";
}) {
  const [documents, setDocuments] = useState<Document[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedSection, setSelectedSection] = useState<string | null>(null);
  const [selectedDocument, setSelectedDocument] = useState<string | null>(null);

  useEffect(() => setSelectedSection(selectedSectionId ?? null), [selectedSectionId]);
  useEffect(() => setSelectedDocument(selectedDocumentId ?? null), [selectedDocumentId]);

  const fetchDocuments = async () => {
    setLoading(true);
    try {
      const { data } = await api.get<Document[]>("/document");
      setDocuments(data);
    } catch (err) {
      console.error("Failed to fetch documents", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, [refreshTrigger]);

  const createDocument = async () => {
    const title = prompt("Enter document title");
    if (!title) return;
    try {
      setLoading(true);
      await api.post("/document", { title });
      fetchDocuments();
    } catch (err) {
      console.error("Failed to create document", err);
    } finally {
      setLoading(false);
    }
  };

  const deleteDocument = async (documentId: string) => {
    if (!confirm("Delete this document and all its data?")) return;
    try {
      setLoading(true);
      await api.delete(`/document/${documentId}`);
      fetchDocuments();

      if (selectedDocument === documentId) {
        setSelectedDocument(null);
        setSelectedSection(null);
        onDocumentSelect?.(null);
        onSectionSelect?.(null);
      }
    } catch (err) {
      console.error("Failed to delete document", err);
    } finally {
      setLoading(false);
    }
  };

  const handleSectionSelect = (docId: string, sectionId: string | null) => {
    if (selectMode !== "section") return;
    setSelectedDocument(docId);
    setSelectedSection(sectionId);
    onSectionSelect?.(sectionId);
    onDocumentSelect?.(docId);
  };

  return (
    <div className="space-y-6">
      <Button
        className="w-full gap-2"
        onClick={createDocument}
        disabled={loading}
      >
        <Plus size={16} />
        New Document
      </Button>

      {loading && <div className="text-sm text-muted-foreground">Loading documents…</div>}

      {documents.map((doc) => {
        const isSelected = selectedDocument === doc.id;
        return (
          <div
            key={doc.id}
            className={`
              rounded-2xl border p-5
              transition-all
              ${isSelected ? "border-primary bg-primary/5 shadow-md" : "bg-background hover:shadow-sm"}
            `}
          >
            {/* Header */}
            <div className="flex items-center justify-between">
              <div
                className="flex items-center gap-2 cursor-pointer"
                onClick={() => onDocumentSelect?.(doc.id)}
              >
                <FileText className="text-muted-foreground" size={18} />
                <span className="font-semibold truncate">{doc.title}</span>
              </div>

              <Button
                size="icon"
                variant="ghost"
                className="text-destructive"
                onClick={() => deleteDocument(doc.id)}
                disabled={loading}
              >
                <Trash2 size={16} />
              </Button>
            </div>

            {/* Sections */}
            <div className="mt-4">
              <SectionList
                sections={doc.sections}
                selectedSection={selectedSection}
                onSectionSelect={(sectionId) => handleSectionSelect(doc.id, sectionId)}
                documentId={doc.id}
                refreshDocuments={fetchDocuments}
              />
            </div>
          </div>
        );
      })}
    </div>
  );
}
