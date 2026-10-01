import { useState } from "react";
import InternForm from "./features/interns/components/InternForm";
import ApprovalListPage from "./features/approval/pages/ApprovalListPage";
import InternDetailPage from "./features/approval/pages/InternDetailPage";
import ContractManagement from "./features/contracts/components/ContractManagement";
import { FilePlus, ClipboardCheck, FileText } from "lucide-react";

type View = "form" | "approval" | "detail" | "contracts";

function App() {
  const [view, setView] = useState<View>("form");
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const menuItems = [
    { key: "form" as const, label: "Thêm hồ sơ", icon: FilePlus },
    { key: "approval" as const, label: "Duyệt hồ sơ", icon: ClipboardCheck },
    { key: "contracts" as const, label: "Tài liệu & hợp đồng", icon: FileText },
  ];

  const breadcrumb: Record<View, string> = {
    form: "Thêm hồ sơ",
    approval: "Duyệt hồ sơ",
    detail: "Chi tiết ứng viên",
    contracts: "Tài liệu & hợp đồng",
  };

  return (
    <div className="min-h-screen bg-[#f3f4ef] flex">
      {/* ===== SIDEBAR FOREST (giống demo hr.html) ===== */}
      <aside className="w-[238px] bg-[#173e34] flex flex-col sticky top-0 h-screen shrink-0 text-[#f5f4ed]">
        {/* Brand */}
        <div className="flex items-center gap-3 px-4 pt-6 pb-7">
          <div className="w-12 h-12 rounded-full bg-[#2d327f] flex items-center justify-center text-white text-[10px] font-bold tracking-wide shrink-0">
            CG
          </div>
          <div>
            <p className="text-[10px] font-bold uppercase tracking-wide leading-tight">
              CODEGYM
            </p>
            <p className="text-[9px] text-[#d5aa65] mt-0.5 uppercase tracking-wider">
              Career Portal
            </p>
          </div>
        </div>

        <p className="mx-4 mb-2 text-[9px] font-bold tracking-[0.11em] uppercase text-[#f5f4ed]/80">
          Không gian nhân sự
        </p>

        <nav className="flex-1 px-3 space-y-1">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive =
              view === item.key ||
              (item.key === "approval" && view === "detail");

            return (
              <button
                key={item.key}
                type="button"
                onClick={() => setView(item.key)}
                className={`w-full flex items-center gap-2.5 px-3 min-h-[42px] rounded text-left text-xs transition ${
                  isActive
                    ? "bg-white/12 text-white font-bold border border-white/10"
                    : "text-[#f5f4ed]/70 hover:bg-white/7 hover:text-white border border-transparent"
                }`}
              >
                <Icon className="w-4 h-4 shrink-0 text-[#d5aa65]" />
                {item.label}
              </button>
            );
          })}
        </nav>

        <p className="mx-4 mb-4 text-[10px] leading-relaxed text-[#f5f4ed]/50">
          Quản lý hồ sơ và chương trình thực tập tại một nơi.
        </p>

        {/* Account card */}
        <div className="mx-3 pt-3 border-t border-white/15 flex items-center gap-2.5 px-2 pb-4">
          <span className="w-8 h-8 rounded-full bg-[#d1a15b] text-[#24352e] text-[11px] font-bold flex items-center justify-center shrink-0">
            HR
          </span>
          <div className="min-w-0">
            <p className="text-[11px] font-bold text-white truncate">Quản trị nhân sự</p>
            <p className="text-[10px] text-[#f5f4ed]/60">Tài khoản HR</p>
          </div>
        </div>
      </aside>

      {/* ===== MAIN ===== */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white/76 border-b border-[#e1e5df] flex items-center justify-between px-6 sticky top-0 z-10 shrink-0 backdrop-blur-sm">
          <p className="text-[11px] text-[#78827c]">
            Career Portal <span className="mx-1">/</span>{" "}
            <strong className="text-[#25332d] font-semibold">{breadcrumb[view]}</strong>
          </p>
          <span className="inline-flex items-center min-h-8 px-3 rounded bg-[#173e34] text-white text-[11px] font-bold">
            Sprint 1 – FE
          </span>
        </header>

        <main className="flex-1 py-8 px-6 overflow-auto">
          <div className="max-w-[1500px] mx-auto">
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
          </div>
        </main>
      </div>
    </div>
  );
}

export default App;