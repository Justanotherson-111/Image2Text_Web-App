import { useEffect, useState } from "react";

export type ToastItem = {
  id: string;
  message: string;
  type: "error" | "success" | "info";
  duration?: number; // optional, defaults to 5000ms
  onClose?: () => void;
};

export default function ToastList({ toasts }: { toasts: ToastItem[] }) {
  const [visibleToasts, setVisibleToasts] = useState<ToastItem[]>([]);

  useEffect(() => {
    setVisibleToasts(toasts);
  }, [toasts]);

  useEffect(() => {
    visibleToasts.forEach((toast) => {
      if (!toast.duration) toast.duration = 5000; // default 5s
      const timeout = setTimeout(() => {
        toast.onClose?.();
        setVisibleToasts((prev) => prev.filter((t) => t.id !== toast.id));
      }, toast.duration);

      return () => clearTimeout(timeout);
    });
  }, [visibleToasts]);

  return (
    <div style={{ position: "fixed", top: 12, right: 12, zIndex: 9999 }}>
      {visibleToasts.map((t) => (
        <div
          key={t.id}
          style={{
            marginBottom: 8,
            padding: 10,
            minWidth: 200,
            borderRadius: 8,
            background:
              t.type === "error"
                ? "#fee2e2"
                : t.type === "success"
                ? "#dcfce7"
                : "#eef2ff",
            color: "#111827",
            boxShadow: "0 2px 8px rgba(0,0,0,0.08)",
            opacity: 1,
            transition: "opacity 0.5s",
          }}
        >
          {t.message}
        </div>
      ))}
    </div>
  );
}
