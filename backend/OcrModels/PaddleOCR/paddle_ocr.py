from fastapi import FastAPI, UploadFile, File, Form, HTTPException
from paddleocr import PaddleOCR
from asyncio import Semaphore
import shutil, uuid, os, asyncio, re, logging

# -------------------------------------------------
# App setup
# -------------------------------------------------
app = FastAPI()
logging.basicConfig(level=logging.INFO)

@app.get("/health")
def health():
    return {"status": "ok"}

# -------------------------------------------------
# OCR pool (1 model per language)
# -------------------------------------------------
ocr_instances = {}
semaphores = {}

def get_ocr(lang: str):
    if lang not in ocr_instances:
        logging.info(f"Initializing PaddleOCR lang={lang}")
        ocr_instances[lang] = PaddleOCR(
            lang=lang,
            use_angle_cls=False,
            show_log=False,
            det_db_score_mode="slow"
        )
        semaphores[lang] = Semaphore(1)

    return ocr_instances[lang], semaphores[lang]

# -------------------------------------------------
# Text cleanup (SAFE, no NLP)
# -------------------------------------------------
def clean_text(text: str) -> str:
    text = re.sub(r"[‐-–—]", "-", text)
    text = re.sub(r"\s+", " ", text)
    text = text.replace(" .", ".").replace(" ,", ",")
    return text.strip()

# -------------------------------------------------
# OCR endpoint
# -------------------------------------------------
@app.post("/ocr")
async def ocr_image(
    file: UploadFile = File(...),
    lang: str = Form("en"),
    cls: str = Form("false")
):
    use_cls = cls.lower() == "true"

    SUPPORTED_LANGS = {"en", "vi", "japan", "korean"}
    if lang not in SUPPORTED_LANGS:
        raise HTTPException(400, f"Unsupported language: {lang}")

    temp_path = f"/tmp/{uuid.uuid4().hex}_{file.filename}"

    try:
        with open(temp_path, "wb") as f:
            shutil.copyfileobj(file.file, f)

        ocr, sem = get_ocr(lang)
        loop = asyncio.get_running_loop()

        async with sem:
            result = await loop.run_in_executor(
                None,
                lambda: ocr.ocr(temp_path, cls=use_cls)
            )

        if not result:
            return {"text": ""}

        lines = [
            line_info[1][0]
            for block in result
            for line_info in block
            if line_info[1][1] >= 0.6
        ]

        text = clean_text("\n".join(lines))
        return {"text": text}

    except Exception as e:
        logging.exception("OCR failed")
        raise HTTPException(500, str(e))

    finally:
        if os.path.exists(temp_path):
            os.remove(temp_path)

