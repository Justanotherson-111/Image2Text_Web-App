FROM python:3.10-slim

WORKDIR /app

# -------------------------------------------------
# System dependencies (minimal + required)
# -------------------------------------------------
RUN apt-get update && apt-get install -y \
    libgl1 \
    libglib2.0-0 \
    libsm6 \
    libxext6 \
    libxrender1 \
    libgomp1 \
    curl \
    && rm -rf /var/lib/apt/lists/*

# -------------------------------------------------
# Runtime safety flags
# -------------------------------------------------
ENV FLAGS_use_mkldnn=0
ENV DISABLE_MODEL_SOURCE_CHECK=True
ENV PADDLEOCR_HOME=/app/.paddleocr
ENV OMP_NUM_THREADS=1
ENV OPENBLAS_NUM_THREADS=1
ENV MKL_NUM_THREADS=1
ENV NUMEXPR_NUM_THREADS=1

# -------------------------------------------------
# Python dependencies (NO vnspell)
# -------------------------------------------------
RUN pip install --no-cache-dir \
    numpy==1.26.4 \
    paddlepaddle==2.6.2 \
    paddleocr==2.7.0.3 \
    fastapi \
    uvicorn \
    python-multipart \
    pillow \
    opencv-python-headless

# -------------------------------------------------
# App code
# -------------------------------------------------
COPY paddle_ocr.py .

EXPOSE 8000

ENTRYPOINT ["uvicorn", "paddle_ocr:app", "--host", "0.0.0.0", "--port", "8000"]
