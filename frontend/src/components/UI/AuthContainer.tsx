// components/UI/AuthContainer.tsx
import type { ReactNode } from "react";

export default function AuthContainer({ children }: { children: ReactNode }) {
  return (
    <div
      style={{
        display: "flex",
        justifyContent: "center",
        alignItems: "center",
        height: "100vh",
        background: "#f0f2f5",
      }}
    >
      <div
        style={{
          width: "100%",
          maxWidth: 400,
          padding: 24,
          background: "white",
          borderRadius: 8,
          boxShadow: "0 4px 12px rgba(0,0,0,0.1)",
        }}
      >
        {children}
      </div>
    </div>
  );
}
