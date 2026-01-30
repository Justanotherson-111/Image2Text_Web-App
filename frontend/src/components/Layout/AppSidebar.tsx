// components/Layout/AppSidebar.tsx
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuButton,
} from "@/components/ui/sidebar";
import { NavLink } from "react-router-dom";
import {
  LayoutDashboard,
  Upload,
  FileText,
  User,
  Shield,
  LogOut,
} from "lucide-react";

export default function AppSidebar() {
  return (
    // 🔥 THIS IS THE FIX
    <Sidebar variant="inset" side="left">
      <SidebarContent>
        <div className="px-4 py-4">
          <div className="text-lg font-semibold">OCR Manager</div>
          <p className="text-xs text-muted-foreground">Image → Text</p>
        </div>

        <SidebarGroup>
          <SidebarGroupLabel>Menu</SidebarGroupLabel>

          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton asChild>
                <NavLink to="/dashboard">
                  <LayoutDashboard />
                  Dashboard
                </NavLink>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem>
              <SidebarMenuButton asChild>
                <NavLink to="/image-upload">
                  <Upload />
                  Upload
                </NavLink>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem>
              <SidebarMenuButton asChild>
                <NavLink to="/extracted-text">
                  <FileText />
                  Text Files
                </NavLink>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem>
              <SidebarMenuButton asChild>
                <NavLink to="/profile">
                  <User />
                  Profile
                </NavLink>
              </SidebarMenuButton>
            </SidebarMenuItem>

            <SidebarMenuItem>
              <SidebarMenuButton asChild>
                <NavLink to="/admin">
                  <Shield />
                  Admin
                </NavLink>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>

      <SidebarFooter>
        <SidebarMenuButton>
          <LogOut />
          Logout
        </SidebarMenuButton>
      </SidebarFooter>
    </Sidebar>
  );
}
