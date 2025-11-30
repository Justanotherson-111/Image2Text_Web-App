import { useEffect, useState } from "react";
import api from "../../api/axios";
import Button from "../UI/Button";
import Toast from "../UI/Toast";
import {
  onOcrProgress,
  onOcrCompleted,
  startSignalR,
  joinImageRoom,
  leaveImageRoom
} from "../../services/signalr";

interface ImageItem {
  id: string;
  fileName: string;
  uploadedById: string;
  ocrProcessed?: boolean;
}

interface Props {
  refreshTrigger: number;
}

export default function ImageList({ refreshTrigger }: Props) {
  const [images, setImages] = useState<ImageItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [progressMap, setProgressMap] = useState<Record<string, number>>({});
  const [completedMap, setCompletedMap] = useState<Record<string, boolean>>({});

  // Fetch images from API
  const fetchImages = async () => {
    setLoading(true);
    try {
      const { data } = await api.get("/image/list");
      setImages(data);

      const initialProgress: Record<string, number> = {};
      const initialCompleted: Record<string, boolean> = {};
      data.forEach((img: ImageItem) => {
        initialProgress[img.id] = img.ocrProcessed ? 100 : 0;
        initialCompleted[img.id] = img.ocrProcessed || false;
      });
      setProgressMap(prev => ({ ...initialProgress, ...prev }));
      setCompletedMap(prev => ({ ...initialCompleted, ...prev }));
    } catch (err: any) {
      setError(err.response?.data || "Failed to fetch images");
    } finally {
      setLoading(false);
    }
  };

  // Delete image
  const deleteImage = async (id: string) => {
    if (!window.confirm("Are you sure you want to delete this image?")) return;
    try {
      await api.delete(`/image/delete/${id}`);
      fetchImages();
    } catch (err: any) {
      setError(err.response?.data || "Delete failed");
    }
  };

  // Download OCR result
  const downloadTextFile = (imageId: string) => {
    window.open(`/api/image/text/${imageId}`, "_blank");
  };

  // Fetch images on mount or when refreshTrigger changes
  useEffect(() => {
    fetchImages();
  }, [refreshTrigger]);

  // SignalR setup + polling fallback
  useEffect(() => {
    let timerId: number;

    const setupSignalR = async () => {
      await startSignalR();

      // SignalR listeners
      onOcrProgress((imageId, progress) => {
        setProgressMap(prev => ({ ...prev, [imageId]: progress }));
      });

      onOcrCompleted((imageId) => {
        setCompletedMap(prev => ({ ...prev, [imageId]: true }));
        setProgressMap(prev => ({ ...prev, [imageId]: 100 }));
      });

      // Join rooms for all current images
      images.forEach(img => joinImageRoom(img.id));
    };

    setupSignalR();

    // Polling fallback every 300ms
    timerId = window.setInterval(async () => {
      const incompleteImages = images.filter(img => !completedMap[img.id]);
      if (incompleteImages.length === 0) return;

      try {
        const { data } = await api.get("/image/list");
        const newProgress: Record<string, number> = {};
        const newCompleted: Record<string, boolean> = {};
        data.forEach((img: ImageItem) => {
          newProgress[img.id] = img.ocrProcessed ? 100 : progressMap[img.id] ?? 0;
          newCompleted[img.id] = img.ocrProcessed || completedMap[img.id] || false;
        });
        setProgressMap(prev => ({ ...prev, ...newProgress }));
        setCompletedMap(prev => ({ ...prev, ...newCompleted }));
      } catch (err: any) {
        console.error("Progress polling failed:", err);
      }
    }, 300);

    // Cleanup on unmount
    return () => {
      clearInterval(timerId);
      images.forEach(img => leaveImageRoom(img.id));
    };
  }, [images, completedMap, progressMap]);

  return (
    <div className="image-list">
      {error && <Toast toasts={[{ id: "error1", message: error, type: "error" }]} />}
      {loading && <p>Loading images...</p>}

      <ul className="space-y-4">
        {images.map(img => {
          const progress = progressMap[img.id] ?? 0;
          const completed = completedMap[img.id] || img.ocrProcessed;

          return (
            <li key={img.id} className="p-4 rounded-xl border bg-gray-900 text-white shadow-md">
              <div className="flex justify-between items-center">
                <strong>{img.fileName}</strong>
                <Button onClick={() => deleteImage(img.id)} variant="danger">Delete</Button>
              </div>

              {/* Animated Progress Bar */}
              <div className="mt-3">
                <div className="w-full bg-gray-700 h-4 rounded-full overflow-hidden relative">
                  <div
                    className={`h-4 rounded-full ${completed ? "bg-green-500" : "bg-blue-500 animate-progress"}`}
                    style={{
                      width: `${progress}%`,
                      transition: "width 0.3s ease-in-out"
                    }}
                  ></div>

                  {/* Optional animated stripes for “in-progress” */}
                  {!completed && (
                    <div className="absolute top-0 left-0 w-full h-4 bg-blue-400 bg-opacity-30 animate-pulse-stripes rounded-full"></div>
                  )}
                </div>
                <p className="mt-1 text-sm">
                  {completed ? "✅ OCR Completed" : `Processing... ${progress}%`}
                </p>
              </div>

              {/* Download button */}
              {completed && (
                <div className="mt-3">
                  <Button onClick={() => downloadTextFile(img.id)}>Download Text</Button>
                </div>
              )}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
