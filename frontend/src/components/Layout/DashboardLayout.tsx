export default function DashboardLayout({
    sidebar,
    children,
}: {
    sidebar: React.ReactNode;
    children: React.ReactNode;
}) {
    return (
        <div className="mx-auto max-w-[1600px] px-6 py-8">
            <div className="grid grid-cols-12 gap-8 min-h-[calc(100vh-4rem)]">

                {/* LEFT SIDEBAR */}
                <aside className="col-span-12 lg:col-span-3">
                    {sidebar}
                </aside>

                {/* MAIN PAGE */}
                <main className="col-span-12 lg:col-span-9">
                    {children}
                </main>

            </div>
        </div>
    );
}