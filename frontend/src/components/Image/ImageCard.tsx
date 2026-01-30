import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { getOcrErrorMessage, useOcrJob } from "../Layout/OcrPolling";
import { useEffect, useState } from "react";
import rerunOcr, { type ImageDto } from "./ReRunOcr";
import Toast from "../UI/Toast";
import api from "@/api/axios";
import type { OcrLanguage, OcrModel } from "./OcrModel";

export function ImageCard({
    img,
    onDelete,
    loading,
    ocrLanguage,
    ocrModel,
    onOcrRerun,
}: {
    img: ImageDto;
    onDelete: (id: string) => void;
    loading: boolean;
    ocrLanguage: OcrLanguage;
    ocrModel: OcrModel;
    onOcrRerun?: () => void;
}) {
    const [rerunLoading, setRerunLoading] = useState(false);
    const [errorMsg, setErrorMsg] = useState<string | null>(null);
    const shouldPollOcr = !img.ocrProcessed;
    const { job, error } = useOcrJob(img.id,shouldPollOcr);
    const [src, setSrc] = useState<string | null>(null);

    const handleRerun = async () => {
        try {
            setRerunLoading(true);
            setErrorMsg(null);
            await rerunOcr(img.id, ocrLanguage, ocrModel);
            onOcrRerun?.();
        } catch (err: any) {
            setErrorMsg(err?.message ?? "Failed to re-run OCR");
        } finally {
            setRerunLoading(false);
        }
    };
    useEffect(() => {
        let url: string | null = null;
        let cancelled = false;

        api
            .get(`/image/raw/${img.id}`, { responseType: "blob" })
            .then(res => {
                if (cancelled) return;
                url = URL.createObjectURL(res.data);
                setSrc(url);
            })
            .catch(err => {
                console.error("Failed to load image preview", err);
                setSrc(null);
            });

        return () => {
            cancelled = true;
            if (url) {
                URL.revokeObjectURL(url);
            }
        };
    }, [img.id]);

    return (
        <Card className="hover:shadow-md transition">
            <CardHeader className="p-2 pb-0">
                <CardTitle className="text-xs truncate">{img.fileName}</CardTitle>
            </CardHeader>

            <CardContent className="p-2 space-y-2">
                {src ? (
                    <img
                        src={src}
                        alt={img.fileName}
                        className="h-20 mx-auto object-contain rounded border"
                    />
                ) : (
                    <div className="h-20 flex items-center justify-center text-xs text-muted-foreground border rounded">
                        No preview
                    </div>
                )}

                {img.previewText && (
                    <p className="text-[11px] text-muted-foreground truncate">
                        {img.previewText}
                    </p>
                )}

                {job && (
                    <div className="space-y-1">
                        <p className="text-[10px] text-muted-foreground">
                            OCR ({job.language ?? "unknown"})
                        </p>

                        <p className="text-[10px] font-medium">
                            Status: {job.status}
                        </p>

                        {["Pending", "Running"].includes(job.status) && (
                            <>
                                <progress
                                    value={job.progress}
                                    max={100}
                                    className="w-full h-2"
                                />
                                <p className="text-[10px] text-right">{job.progress}%</p>
                            </>
                        )}

                        {job.status === "Failed" && (
                            <p className="text-[10px] text-red-500">
                                {getOcrErrorMessage(job.errorCode, job.errorMessage)}
                            </p>
                        )}
                    </div>
                )}

                {errorMsg && <Toast toasts={[{ id: `ocr-${img.id}`, message: errorMsg, type: "error" }]} />}
                {error && <Toast toasts={[{ id: `ocr-${img.id}-net`, message: error, type: "error" }]} />}

                <Button
                    size="sm"
                    variant="ghost"
                    className="w-full text-destructive"
                    onClick={() => onDelete(img.id)}
                    disabled={loading}
                >
                    Delete
                </Button>

                <Button
                    size="sm"
                    onClick={handleRerun}
                    disabled={rerunLoading || ["Pending", "Running"].includes(job?.status ?? "")}
                    className="w-full"
                >
                    {rerunLoading ? "Re-running..." : "Re-run OCR"}
                </Button>
            </CardContent>
        </Card>
    );
}
