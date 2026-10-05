import { useState } from "react";
import {
  BookOpen,
  CalendarDays,
  ListTodo,
  BarChart3,
} from "lucide-react";
import ProgramPage from "./features/programs/pages/ProgramPage";
import SchedulePage from "./features/shifts/pages/SchedulePage";
import TaskProgressPage from "./features/tasks/pages/TaskProgressPage";
import EvaluationSummaryPage from "./features/statistics/pages/EvaluationSummaryPage";

type View = "programs" | "shifts" | "tasks" | "statistics";

const menuItems: {
  key: View;
  label: string;
  icon: typeof BookOpen;
}[] = [
  { key: "programs", label: "Chương trình", icon: BookOpen },
  { key: "shifts", label: "Lịch làm việc", icon: CalendarDays },
  { key: "tasks", label: "Công việc", icon: ListTodo },
  { key: "statistics", label: "Thống kê", icon: BarChart3 },
];

const breadcrumb: Record<View, string> = {
  programs: "Chương trình",
  shifts: "Lịch làm việc",
  tasks: "Công việc",
  statistics: "Thống kê",
};

export default function App() {
  const [view, setView] = useState<View>("programs");

  return (
    <div className="min-h-screen bg-[#f3f4ef] flex">
      {/* Sidebar – màu forest hệ thống CODEGYM */}
      <aside className="w-[238px] bg-[#173e34] flex flex-col sticky top-0 h-screen shrink-0 text-[#f5f4ed]">
        <div className="flex items-center gap-3 px-4 pt-6 pb-7">
          <img
            src="/codegym-logo.svg"
            alt="CODEGYM"
            className="w-12 h-12 rounded-full object-contain shrink-0"
          />
          <div>
            <p className="text-[10px] font-bold uppercase tracking-wide leading-tight m-0">
              CODEGYM
            </p>
            <p className="text-[9px] text-[#d5aa65] mt-0.5 uppercase tracking-wider m-0">
              Career Portal
            </p>
          </div>
        </div>

        <p className="mx-4 mb-2 text-[9px] font-bold tracking-[0.11em] uppercase text-[#f5f4ed]/50 m-0">
          Không gian nhân sự
        </p>

        <nav className="flex-1 px-3 space-y-1 overflow-y-auto">
          {menuItems.map((item) => {
            const Icon = item.icon;
            const isActive = view === item.key;
            return (
              <button
                key={item.key}
                type="button"
                onClick={() => setView(item.key)}
                className={`w-full flex items-center gap-2.5 px-3 min-h-[42px] rounded text-left text-xs transition border ${
                  isActive
                    ? "bg-white/12 text-white font-bold border-white/10"
                    : "text-[#f5f4ed]/70 hover:bg-white/7 hover:text-white border-transparent"
                }`}
              >
                <Icon className="w-4 h-4 shrink-0 text-[#d5aa65]" />
                {item.label}
              </button>
            );
          })}
        </nav>

        <p className="mx-4 mb-4 text-[10px] leading-relaxed text-[#f5f4ed]/50 m-0">
          Quản lý chương trình, lịch, công việc và đánh giá thực tập.
        </p>

        <div className="mx-3 pt-3 border-t border-white/15 flex items-center gap-2.5 px-2 pb-4">
          <span className="w-8 h-8 rounded-full bg-[#d1a15b] text-[#24352e] text-[11px] font-bold flex items-center justify-center shrink-0">
            HR
          </span>
          <div className="min-w-0">
            <p className="text-[11px] font-bold text-white truncate m-0">
              Quản trị nhân sự
            </p>
            <p className="text-[10px] text-[#f5f4ed]/60 m-0">Tài khoản HR</p>
          </div>
        </div>
      </aside>

      {/* Main */}
      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white/76 border-b border-[#e1e5df] flex items-center justify-between px-6 sticky top-0 z-10 shrink-0 backdrop-blur-sm">
          <p className="text-[11px] text-[#78827c] m-0">
            Career Portal <span className="mx-1">/</span>{" "}
            <strong className="text-[#25332d] font-semibold">
              {breadcrumb[view]}
            </strong>
          </p>
          <span className="inline-flex items-center min-h-8 px-3 rounded bg-[#173e34] text-white text-[11px] font-bold">
            HR
          </span>
        </header>

        <main className="flex-1 py-8 px-6 overflow-auto">
          <div className="max-w-[1500px] mx-auto">
            {view === "programs" && <ProgramPage />}
            {view === "shifts" && <SchedulePage />}
            {view === "tasks" && <TaskProgressPage />}
            {view === "statistics" && <EvaluationSummaryPage />}
          </div>
        </main>
      </div>
    </div>
  );
}
