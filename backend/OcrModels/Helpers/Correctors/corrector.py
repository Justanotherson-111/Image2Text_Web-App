from fastapi import FastAPI
from pydantic import BaseModel
from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
from langdetect import detect, DetectorFactory
import torch
import re
from threading import Lock

# ------------------------------------
# App
# ------------------------------------
app = FastAPI()
torch.set_num_threads(1)
DetectorFactory.seed = 0

# ------------------------------------
# Models config
# ------------------------------------
MODELS = {
    "en": {
        "name": "prithivida/grammar_error_correcter_v1",
        "use_prompt": True,
        "prompt": lambda t: f"Correct the grammar of the following English text:\n{t}"
    },
    "vi": {
        # BEST free Vietnamese correction model
        "name": "bmd1905/vietnamese-correction",
        "use_prompt": False
    },
    "ja": {
        "name": "sonoisa/t5-base-japanese",
        "use_prompt": True,
        "prompt": lambda t: "次の文章の誤りを修正してください:\n" + t
    },
    "ko": {
        "name": "psyche/kor-grammar-corrector",
        "use_prompt": True,
        "prompt": lambda t: "다음 문장의 문법 오류를 수정하세요:\n" + t
    }
}

# ------------------------------------
# Model cache (thread-safe)
# ------------------------------------
_loaded_models = {}
_model_lock = Lock()

def load_model(lang: str):
    with _model_lock:
        if lang in _loaded_models:
            return _loaded_models[lang]

        model_name = MODELS[lang]["name"]
        tokenizer = AutoTokenizer.from_pretrained(model_name)
        model = AutoModelForSeq2SeqLM.from_pretrained(model_name)
        model.eval()

        _loaded_models[lang] = (tokenizer, model)
        return tokenizer, model

# ------------------------------------
# Request schema
# ------------------------------------
class CorrectRequest(BaseModel):
    text: str

# ------------------------------------
# Language detection (OCR-safe)
# ------------------------------------
def detect_language(text: str) -> str:
    if len(text) < 10:
        return "unknown"

    try:
        lang = detect(text)
    except Exception:
        return "unknown"

    if lang.startswith("vi"):
        return "vi"
    if lang.startswith("en"):
        return "en"
    if lang.startswith("ja"):
        return "ja"
    if lang.startswith("ko"):
        return "ko"

    return "unknown"

# ------------------------------------
# Text normalization (very light)
# ------------------------------------
def normalize_text(text: str) -> str:
    text = text.replace("\x00", "")
    text = re.sub(r"\s+", " ", text)
    return text.strip()

# ------------------------------------
# Correction logic
# ------------------------------------
def correct_text_by_lang(text: str, lang: str) -> str:
    if lang not in MODELS:
        return text

    tokenizer, model = load_model(lang)
    cfg = MODELS[lang]

    input_text = (
        cfg["prompt"](text)
        if cfg.get("use_prompt", False)
        else text
    )

    inputs = tokenizer(
        input_text,
        return_tensors="pt",
        truncation=True,
        max_length=512
    )

    with torch.no_grad():
        outputs = model.generate(
            **inputs,
            max_new_tokens=256,
            num_beams=4,
            do_sample=False,
            early_stopping=True
        )

    return tokenizer.decode(outputs[0], skip_special_tokens=True)

# ------------------------------------
# API endpoint
# ------------------------------------
@app.post("/correct")
def correct(req: CorrectRequest):
    text = normalize_text(req.text)
    if not text:
        return {"lang": "unknown", "text": ""}

    lang = detect_language(text)
    corrected = correct_text_by_lang(text, lang)

    return {
        "lang": lang,
        "text": corrected
    }
