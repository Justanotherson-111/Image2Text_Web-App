
export const OCR_MODELS = [
  { value: "tesseract", label: "Tesseract (Default)" },
  { value: "paddleocr", label: "PaddleOCR" },
] as const;

export type OcrModel = typeof OCR_MODELS[number]["value"];

export const OCR_LANGUAGES = [
  { value: "eng", label: "English" },
  { value: "vie", label: "Vietnamese" },
  { value: "jpn", label: "Japanese" },
  { value: "kor", label: "Korean" },
] as const;

export type OcrLanguage = typeof OCR_LANGUAGES[number]["value"];
