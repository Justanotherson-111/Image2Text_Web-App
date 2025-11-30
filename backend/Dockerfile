# ============================
# STAGE 1 — BUILD .NET APP
# ============================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy everything and restore & publish
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ============================
# STAGE 2 — RUNTIME IMAGE
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install Tesseract OCR + English traineddata
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    tesseract-ocr \
    tesseract-ocr-eng \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

# Ensure tessdata exists with eng.traineddata
RUN mkdir -p /usr/share/tesseract-ocr/5/tessdata && \
    curl -fSL -o /usr/share/tesseract-ocr/5/tessdata/eng.traineddata \
    https://github.com/tesseract-ocr/tessdata_best/raw/main/eng.traineddata && \
    chmod -R 755 /usr/share/tesseract-ocr/5/tessdata && \
    ls -l /usr/share/tesseract-ocr/5/tessdata

# Create a proper non-root user for the container
RUN useradd -m -u 1000 appuser

# Copy published .NET app
COPY --from=build /app/publish ./

# Create storage folders with correct ownership
RUN mkdir -p /app/Uploads /app/ExtractedText && \
    chown -R appuser:appuser /app/Uploads /app/ExtractedText /app

# Set tessdata environment variable globally
ENV TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata

# Switch to non-root user
USER appuser
WORKDIR /app

# Expose port
EXPOSE 8080

# Healthcheck
HEALTHCHECK --interval=10s --timeout=5s --retries=10 \
    CMD curl -f http://localhost:8080/health || exit 1

# Start the app
ENTRYPOINT ["dotnet", "backend.dll"]
