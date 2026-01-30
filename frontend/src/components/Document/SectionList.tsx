import { Button } from "@/components/ui/button";
import api from "../../api/axios";
import { Folder, Check, Trash2, Plus } from "lucide-react";
import { useState } from "react";

interface Section {
  id: string;
  title: string;
}

export default function SectionList({
  sections,
  selectedSection,
  onSectionSelect,
  documentId,
  refreshDocuments,
}: {
  sections: Section[];
  selectedSection: string | null;
  onSectionSelect: (sectionId: string | null) => void;
  documentId: string;
  refreshDocuments: () => void;
}) {
  const [loading, setLoading] = useState(false);

  const createSection = async () => {
    const title = prompt("Enter section title");
    if (!title) return;
    try {
      setLoading(true);
      await api.post(`/document/${documentId}/section`, { title });
      refreshDocuments();
    } catch (err) {
      console.error("Failed to create section", err);
    } finally {
      setLoading(false);
    }
  };

  const deleteSection = async (sectionId: string) => {
    if (!confirm("Delete this section and all images inside?")) return;
    try {
      setLoading(true);
      await api.delete(`/document/section/${sectionId}`);
      refreshDocuments();
      if (selectedSection === sectionId) onSectionSelect(null);
    } catch (err) {
      console.error("Failed to delete section", err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-3">
      <Button
        size="sm"
        variant="outline"
        className="w-full gap-2"
        onClick={createSection}
        disabled={loading}
      >
        <Plus size={14} />
        Add Section
      </Button>

      {sections.map((sec) => {
        const isSelected = selectedSection === sec.id;
        return (
          <div
            key={sec.id}
            className={`
              flex items-center justify-between
              px-4 py-3 rounded-xl border
              transition-all
              ${isSelected ? "border-primary bg-primary/10" : "hover:bg-muted/60"}
            `}
          >
            <div
              className="flex items-center gap-3 cursor-pointer min-w-0"
              onClick={() => onSectionSelect(sec.id)}
            >
              <Folder size={16} className="text-muted-foreground" />
              <span className="text-sm font-medium truncate">{sec.title}</span>
            </div>

            <div className="flex items-center gap-1">
              {isSelected && <Check size={16} className="text-primary" />}
              <Button
                size="icon"
                variant="ghost"
                className="text-destructive"
                onClick={() => deleteSection(sec.id)}
                disabled={loading}
              >
                <Trash2 size={15} />
              </Button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
