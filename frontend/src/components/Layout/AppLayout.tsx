import { Outlet } from "react-router-dom";
import AppNavbar from "./AppNavbar";

export default function AppLayout() {
  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* App name + nav */}
      <AppNavbar />

      {/* PAGE AREA */}
      <div className="flex-1 w-full">
        <Outlet />
      </div>
    </div>
  );
}
