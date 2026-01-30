import { useCallback, useEffect, useRef, useState } from "react";
import api from "../../api/axios";
import { Button } from "@/components/ui/button";
import Toast from "../UI/Toast";
import { ImageCard } from "./ImageCard";
import type { OcrLanguage, OcrModel } from "./OcrModel";


export default function ImageList({
  sectionId,
  refreshTrigger,
  newImages,
  onImagesChanged,
  onOcrRerun,
  ocrLanguage,
  ocrModel,
}: {
  sectionId: string | null;
  refreshTrigger: number;
  newImages: any[];
  onImagesChanged?: () => void;
  onOcrRerun?: () => void;
  ocrLanguage: OcrLanguage;
  ocrModel: OcrModel;
}) {
  const [images, setImages] = useState<any[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const pollingRef = useRef<NodeJS.Timeout | null>(null);

  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [total, setTotal] = useState(0);

  const fetchImages = useCallback(async () => {
    if (!sectionId) return;
    try {
      const { data } = await api.get(`/image/section/${sectionId}?page=${page}&pageSize=${pageSize}`);
      setImages(data.images);
      setTotal(data.total);
    } catch {
      setError("Failed to load images");
    }
  }, [sectionId, page, pageSize]);

  useEffect(() => {
    fetchImages();
  }, [refreshTrigger, fetchImages, page]);

  useEffect(() => {
    if (!images.length) return;

    const hasRunningOcr = images.some(
      img =>
        !img.ocrProcessed &&
        img.ocrJob &&
        (img.ocrJob.status === "Pending" || img.ocrJob.status === "Running")
    );

    if (!hasRunningOcr) {
      if (pollingRef.current) {
        clearInterval(pollingRef.current);
        pollingRef.current = null;
      }
      return;
    }

    if (!pollingRef.current) {
      pollingRef.current = setInterval(fetchImages, 3000);
    }

    return () => {
      if (pollingRef.current) {
        clearInterval(pollingRef.current);
        pollingRef.current = null;
      }
    };
  }, [images, fetchImages]);

  // Append uploaded images instantly
  useEffect(() => {
    if (newImages.length > 0) {
      setImages(prev => [...newImages, ...prev]);
    }
  }, [newImages]);

  const deleteImage = async (id: string) => {
    if (!confirm("Delete this image?")) return;
    try {
      setLoading(true);
      await api.delete(`/image/${id}`);
      //fetchImages();
      setImages(prev => prev.filter(img => img.id !== id));
      onImagesChanged?.();
    } catch {
      setError("Failed to delete image");
    } finally {
      setLoading(false);
    }
  };

  const deleteSectionImages = async () => {
    if (!sectionId) return;
    if (!confirm("Delete all images in this section?")) return;
    try {
      setLoading(true);
      await api.delete(`/image/section/${sectionId}`);
      setImages([]);
      onImagesChanged?.();
      //fetchImages();
    } catch {
      setError("Failed to delete section images");
    } finally {
      setLoading(false);
    }
  };

  if (!sectionId) return null;
  return (
    <div className="space-y-4">
      {error && <Toast toasts={[{ id: "err", message: error, type: "error" }]} />}

      <div className="flex justify-end mb-2">
        <Button
          size="sm"
          variant="destructive"
          onClick={deleteSectionImages}
          disabled={loading || images.length === 0}
        >
          Delete All Images
        </Button>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
        {images.map((img) => (
          <ImageCard
            key={img.id}
            img={img}
            loading={loading}
            ocrLanguage={ocrLanguage}
            ocrModel={ocrModel}
            onDelete={deleteImage}
            onOcrRerun={onOcrRerun}
          />
        ))}
      </div>
      <div className="flex justify-between mt-2">
        <Button disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Previous</Button>
        <span>{page} / {Math.ceil(total / pageSize)}</span>
        <Button disabled={page >= Math.ceil(total / pageSize)} onClick={() => setPage(p => p + 1)}>Next</Button>
      </div>
    </div>
  );
}
