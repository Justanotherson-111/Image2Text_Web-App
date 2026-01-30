namespace backend.OcrModels.Helpers
{
    public static class OcrLanguage
    {
        private static readonly Dictionary<string, string> PaddleMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["eng"] = "en",
                ["en"] = "en",
                ["vie"] = "vi",
                ["vi"] = "vi",
                ["jpn"] = "japan",
                ["ja"] = "japan",
                ["japan"] = "japan",
                ["kor"] = "korean",
                ["ko"] = "korean",
                ["korean"] = "korean"
            };

        private static readonly HashSet<string> TesseractLangs =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "eng", "vie", "jpn", "kor"
            };

        public static string NormalizeForPaddle(string lang)
        {
            if (!PaddleMap.TryGetValue(lang, out var normalized))
                throw new ArgumentException($"Unsupported PaddleOCR language: {lang}");
            return normalized;
        }

        public static string NormalizeForTesseract(string lang)
        {
            if (!TesseractLangs.Contains(lang))
                throw new ArgumentException($"Unsupported Tesseract language: {lang}");
            return lang;
        }
    }

}