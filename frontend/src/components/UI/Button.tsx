import React from "react";

type ButtonVariant = "primary" | "danger" | "secondary";

type Props = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  children: React.ReactNode;
  variant?: ButtonVariant;
};

export default function Button({
  children,
  variant = "primary",
  ...props
}: Props) {
  const getBackground = () => {
    switch (variant) {
      case "danger":
        return "#dc2626";
      case "secondary":
        return "#6b7280";
      default:
        return "#2563eb";
    }
  };

  return (
    <button
      {...props}
      style={{
        padding: "10px 16px",
        borderRadius: 6,
        border: "none",
        background: getBackground(),
        color: "white",
        cursor: props.disabled ? "not-allowed" : "pointer",
        fontWeight: 600,
        width: "100%",
        opacity: props.disabled ? 0.6 : 1,
      }}
    >
      {children}
    </button>
  );
}
