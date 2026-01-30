import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import {
    LayoutDashboard,
    Upload,
    FileText,
    Shield,
    User,
    LogOut,
} from "lucide-react"
import { Link, useLocation, useNavigate } from "react-router-dom"
import { logout } from "@/auth/AuthService"
import { useAuth } from "@/auth/AuthContext"

const navItemClass =
    "flex h-9 items-center gap-2 rounded-md px-3 text-sm font-medium transition-colors " +
    "hover:bg-accent hover:text-accent-foreground"

export default function AppNavbar() {
    const location = useLocation()
    const navigate = useNavigate()
    const { user } = useAuth()

    const isActive = (path: string) =>
        location.pathname.startsWith(path)

    const handleLogout = async () => {
        await logout()          
        navigate("/")        
    }

    return (
        <header className="border-b bg-background">
            <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-6">

                {/* LEFT */}
                <Link to="/dashboard" className="font-semibold">
                    OCR Manager
                </Link>

                {/* CENTER */}
                <nav className="flex items-center gap-1">
                    <NavLink to="/dashboard" active={isActive("/dashboard")}>
                        <LayoutDashboard className="h-4 w-4" />
                        Dashboard
                    </NavLink>

                    <NavLink to="/image-upload" active={isActive("/image-upload")}>
                        <Upload className="h-4 w-4" />
                        Upload
                    </NavLink>

                    <NavLink to="/extracted-text" active={isActive("/extracted-text")}>
                        <FileText className="h-4 w-4" />
                        Text Files
                    </NavLink>

                    {user?.role?.includes("Admin") && (
                        <NavLink to="/admin" active={isActive("/admin")}>
                            <Shield className="h-4 w-4" />
                            Admin
                        </NavLink>
                    )}

                    {/* ACCOUNT – SAME BAR, SAME HEIGHT */}
                    <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                            <button className={navItemClass}>
                                <Avatar className="h-6 w-6">
                                    <AvatarFallback>
                                        {"Profile:"}
                                    </AvatarFallback>
                                </Avatar>
                                <span className="hidden sm:block">
                                    {user?.username}
                                </span>
                            </button>
                        </DropdownMenuTrigger>

                        <DropdownMenuContent align="end" className="w-40">
                            <DropdownMenuItem asChild>
                                <Link to="/profile" className="flex items-center">
                                    <User className="mr-2 h-4 w-4" />
                                    Profile
                                </Link>
                            </DropdownMenuItem>

                            <DropdownMenuSeparator />

                            <DropdownMenuItem
                                onClick={handleLogout}
                                className="text-red-600 cursor-pointer"
                            >
                                <LogOut className="mr-2 h-4 w-4" />
                                Logout
                            </DropdownMenuItem>
                        </DropdownMenuContent>
                    </DropdownMenu>
                </nav>
            </div>
        </header>
    )
}

function NavLink({
    to,
    active,
    children,
}: {
    to: string
    active: boolean
    children: React.ReactNode
}) {
    return (
        <Link
            to={to}
            className={[
                navItemClass,
                active && "bg-accent text-accent-foreground",
            ]
                .filter(Boolean)
                .join(" ")}
        >
            {children}
        </Link>
    )
}
