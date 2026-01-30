import api from "@/api/axios";

export interface OcrSummary {
  total: number;
  completed: number;
  processing: number;
  failed: number;
}

const EMPTY_SUMMARY: OcrSummary = {
  total: 0,
  completed: 0,
  processing: 0,
  failed: 0,
};

export async function getOcrSummary(
  documentId: string
): Promise<OcrSummary> {
  try {
    const res = await api.get<OcrSummary>("/dashboard/ocr-summary", {
      params: { documentId },
    });
    return res.data ?? EMPTY_SUMMARY;
  } catch (err) {
    console.error("Failed to fetch OCR summary:", err);
    return EMPTY_SUMMARY;
  }
}

