import api from "@/api/axios";
import { useEffect, useState } from "react";

export type OcrJobStatus = "Pending" | "Running" | "Completed" | "Failed";

export type OcrErrorCode =
    | "OCR_UNSUPPORTED_LANGUAGE"
    | "OCR_MISSING_TESSDATA"
    | "OCR_ENGINE_FAILURE"
    | "OCR_IMAGE_NOT_FOUND";

export interface OcrJobDto {
    status: OcrJobStatus;
    progress: number;
    errorCode?: OcrErrorCode;
    errorMessage?: string;
    language?: string;
}

export function useOcrJob(
    imageId: string | null,
    enabled: boolean = true
) {
    const [job, setJob] = useState<OcrJobDto | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!imageId || !enabled) {
            return;
        }

        let stopped = false;
        let intervalId: number | null = null;

        const fetchJob = async () => {
            try {
                const res = await api.get<OcrJobDto>(
                    `/image/ocr-jobs/by-image/${imageId}`
                );

                if (stopped) return;

                setJob(res.data);

                // Stop polling on terminal state
                if (
                    res.data.status === "Completed" ||
                    res.data.status === "Failed"
                ) {
                    if (intervalId) window.clearInterval(intervalId);
                }
            } catch (err) {
                if (!stopped) {
                    setError("Failed to fetch OCR status");
                }
            }
        };

        // Initial fetch
        fetchJob();

        intervalId = window.setInterval(fetchJob, 2000);

        return () => {
            stopped = true;
            if (intervalId) window.clearInterval(intervalId);
        };
    }, [imageId, enabled]);

    return { job, error };
}

export const OCR_ERROR_LABELS: Record<OcrErrorCode, string> = {
    OCR_UNSUPPORTED_LANGUAGE: "Unsupported OCR language",
    OCR_MISSING_TESSDATA: "OCR language data not installed on server",
    OCR_ENGINE_FAILURE: "OCR engine failed",
    OCR_IMAGE_NOT_FOUND: "Image file not found",
};

export function getOcrErrorMessage(
    code?: OcrErrorCode,
    fallback?: string
) {
    if (!code) return fallback ?? "OCR failed";
    return OCR_ERROR_LABELS[code] ?? fallback ?? "OCR failed";
}

