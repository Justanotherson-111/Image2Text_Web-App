import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { getTextFileContent, updateTextFileContent } from "./textfile";

export default function TextEditorModal({
    textFileId,
    fileName,
    onClose,
    onSaved,
}: {
    textFileId: string;
    fileName: string;
    onClose: () => void;
    onSaved: () => void;
}) {
    const [content, setContent] = useState("");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        getTextFileContent(textFileId).then((d) => {
            setContent(d.content);
            setLoading(false);
        });
    }, [textFileId]);

    const save = async () => {
        setSaving(true);
        await updateTextFileContent(textFileId, content);
        setSaving(false);
        onSaved();
        onClose();
    };

    if (loading) return null;

    return (
        <div className="fixed inset-0 flex items-center justify-center z-50 p-4">
            {/* Overlay only */}
            <div className="absolute inset-0 bg-black/50"></div>

            {/* Modal content */}
            <Card className="relative w-[95%] max-w-[1200px] max-h-[95vh] flex flex-col bg-white">
                <CardHeader className="flex flex-row justify-between items-center">
                    <CardTitle>Edit OCR Text — {fileName}</CardTitle>
                    <Button variant="ghost" onClick={onClose}>✕</Button>
                </CardHeader>

                <CardContent className="flex-1 flex flex-col gap-4 overflow-hidden">
                    <textarea
                        value={content}
                        onChange={(e) => setContent(e.target.value)}
                        className="flex-1 w-full h-full resize-none border rounded p-4 font-mono text-sm overflow-auto bg-white"
                        style={{ minHeight: "500px" }}
                    />

                    <div className="flex justify-end gap-2 mt-2">
                        <Button variant="outline" onClick={onClose}>
                            Cancel
                        </Button>
                        <Button onClick={save} disabled={saving}>
                            {saving ? "Saving..." : "Save"}
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
