import { useState } from "react";
import InternForm from "./features/interns/components/InternForm";
import ApprovalListPage from "./features/approval/pages/ApprovalListPage";
import InternDetailPage from "./features/approval/pages/InternDetailPage";
import ContractManagement from "./features/contracts/components/ContractManagement";
import { FilePlus, ClipboardCheck, FileText, Bell } from "lucide-react";

type View = "form" | "approval" | "detail" | "contracts";

function App() {
  const [view, setView] = useState<View>("form");
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const menuItems = [
    { key: "form" as const, label: "Thêm hồ sơ", icon: FilePlus },
    { key: "approval" as const, label: "Duyệt hồ sơ", icon: ClipboardCheck },
    { key: "contracts" as const, label: "Quản lý hợp đồng", icon: FileText },
  ];

  const pageTitle: Record<View, string> = {
    form: "Thêm mới hồ sơ",
    approval: "Duyệt hồ sơ thực tập sinh",
    detail: "Chi tiết ứng viên",
    contracts: "Quản lý hợp đồng",
  };

  return (
    <div className="min-h-screen bg-slate-100 flex">
      {/* ===== SIDEBAR XANH NAVY (theo mockup nhóm) ===== */}
      <aside className="w-56 bg-[#0f172a] flex flex-col sticky top-0 h-screen shrink-0">
        <div className="px-5 py-5 border-b border-slate-700/50">
          <h1 className="text-sm font-bold text-white tracking-wide">TTS MANAGER</h1>
          <p className="text-[11px] text-slate-400 mt-0.5 uppercase tracking-wider">
            Thực tập sinh
          </p>
        </div>

        <nav className="flex-1 p-3 space-y-0.5">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive =
              view === item.key || (item.key === "approval" && view === "detail");

            return (
              <button
                key={item.key}
                onClick={() => setView(item.key)}
                className={`w-full flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition ${
                  isActive
                    ? "bg-[#1e3a5f] text-white"
                    : "text-slate-300 hover:bg-slate-800 hover:text-white"
                }`}
              >
                <Icon className="w-4 h-4 flex-shrink-0" />
                {item.label}
              </button>
            );
          })}
        </nav>
      </aside>

      {/* ===== TOPBAR + CONTENT ===== */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-14 bg-white border-b border-slate-200 flex items-center justify-between px-6 sticky top-0 z-10 shrink-0">
          <div>
            <h2 className="text-sm font-semibold text-slate-800">{pageTitle[view]}</h2>
            <p className="text-xs text-slate-400">Sprint 1 – Frontend</p>
          </div>
          <div className="flex items-center gap-3">
            <button
              type="button"
              className="p-2 rounded-lg text-slate-400 hover:bg-slate-100 transition"
              title="Thông báo"
            >
              <Bell className="w-4 h-4" />
            </button>
            <span className="text-xs font-medium text-slate-600 bg-slate-100 px-3 py-1.5 rounded-full">
              HR
            </span>
          </div>
        </header>

        <main className="flex-1 py-8 px-6 overflow-auto">
          {view === "form" && <InternForm />}

          {view === "approval" && (
            <ApprovalListPage
              onViewDetail={(id) => {
                setSelectedId(id);
                setView("detail");
              }}
            />
          )}

          {view === "detail" && (
            <InternDetailPage
              internId={selectedId ?? undefined}
              onBack={() => setView("approval")}
            />
          )}

          {view === "contracts" && (
            <ContractManagement internId={1} internName="Nguyễn Văn A" />
          )}
        </main>
      </div>
    </div>
  );
}

export default App;