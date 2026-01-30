import api from "@/api/axios";
import type { OcrLanguage, OcrModel } from "./OcrModel";

export default async function rerunOcr(
    imageId: string,
    language: OcrLanguage,
    model: OcrModel
) {
    try {
        const res = await api.post(`/image/${imageId}/rerun-ocr`, {
            language,
            model,
        });
        return res.data;
    } catch (err: any) {
        // Axios error
        if (err.response?.data) {
            // Backend string OR object
            const msg =
                typeof err.response.data === "string"
                    ? err.response.data
                    : err.response.data.message ?? "OCR failed";

            throw new Error(msg);
        }

        throw new Error("Network error");
    }
}


export interface ImageDto {
    id: string;
    fileName: string;
    uploadedAt: string;
    ocrProcessed: boolean;
    previewText: string;
    previewUrl: string;
}
