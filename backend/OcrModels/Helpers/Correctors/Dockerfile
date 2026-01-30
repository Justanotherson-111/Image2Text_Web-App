FROM python:3.10-slim

WORKDIR /app

# -----------------------------
# System dependencies
# -----------------------------
RUN apt-get update && apt-get install -y \
    git \
    build-essential \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# -----------------------------
# Runtime environment
# -----------------------------
ENV OMP_NUM_THREADS=1 \
    MKL_NUM_THREADS=1 \
    OPENBLAS_NUM_THREADS=1 \
    NUMEXPR_NUM_THREADS=1 \
    TOKENIZERS_PARALLELISM=false \
    PYTHONUNBUFFERED=1

# -----------------------------
# Python dependencies
# -----------------------------
RUN pip install --no-cache-dir \
    torch \
    --extra-index-url https://download.pytorch.org/whl/cpu \
    transformers \
    sentencepiece \
    protobuf \
    fastapi \
    uvicorn \
    langdetect

# -----------------------------
# App
# -----------------------------
COPY corrector.py .

EXPOSE 8001

ENTRYPOINT ["uvicorn", "corrector:app", "--host", "0.0.0.0", "--port", "8001"]
