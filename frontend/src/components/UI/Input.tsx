// Input.tsx
import React from "react";

type Props = React.InputHTMLAttributes<HTMLInputElement>;

export default function Input(props: Props) {
  return (
    <input
      {...props}
      style={{
        padding: "10px 12px",
        borderRadius: 6,
        border: "1px solid #ccc",
        width: "100%",
        boxSizing: "border-box",
        marginBottom: 12,
      }}
    />
  );
}