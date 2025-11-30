import { Outlet } from "react-router-dom";
import Sidebar from "./Sidebar";

export default function Layout() {
  return (
    <div style={{ display: "flex", minHeight: "100vh", fontFamily: "Arial, sans-serif" }}>
      <Sidebar />
      <main style={{ flex: 1, padding: "20px" }}>
        <Outlet /> {/* This will render nested route elements */}
      </main>
    </div>
  );
}
