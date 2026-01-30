import { useEffect, useState } from "react";
import { cn } from "@/lib/utils";

export type ToastItem = {
  id: string;
  message: string;
  type: "error" | "success" | "info"; // only these 3 allowed
  duration?: number;
  onClose?: () => void;
};

export default function Toast({ toasts }: { toasts: ToastItem[] }) {
  const [visibleToasts, setVisibleToasts] = useState<ToastItem[]>([]);

  useEffect(() => {
    setVisibleToasts(toasts);
  }, [toasts]);

  useEffect(() => {
    visibleToasts.forEach((toast) => {
      const timeout = setTimeout(() => {
        toast.onClose?.();
        setVisibleToasts((prev) => prev.filter((t) => t.id !== toast.id));
      }, toast.duration ?? 5000);

      return () => clearTimeout(timeout);
    });
  }, [visibleToasts]);

  return (
    <div className="fixed top-4 right-4 z-50 space-y-2">
      {visibleToasts.map((t) => (
        <div
          key={t.id}
          className={cn(
            "min-w-[220px] rounded-lg px-4 py-3 text-sm shadow-lg border animate-in fade-in slide-in-from-top-2",
            t.type === "success" &&
              "bg-emerald-50 border-emerald-200 text-emerald-900",
            t.type === "error" &&
              "bg-red-50 border-red-200 text-red-900",
            t.type === "info" &&
              "bg-blue-50 border-blue-200 text-blue-900"
          )}
        >
          {t.message}
        </div>
      ))}
    </div>
  );
}
