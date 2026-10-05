import { useMemo, useState } from "react";
import { Search } from "lucide-react";

interface Task {
  id: number;
  title: string;
  internName: string;
  end: string;
  detail: string;
  progress: number;
  status: string;
}

const STATUS_OPTIONS = [
  "Chưa bắt đầu",
  "Đang thực hiện",
  "Tạm dừng",
  "Quá hạn",
  "Hoàn thành",
] as const;

const initialTasks: Task[] = [
  {
    id: 1,
    title: "Thiết kế màn hình đăng nhập",
    internName: "Nguyễn Văn An",
    end: "2026-10-10",
    detail: "Làm theo mẫu Figma",
    progress: 40,
    status: "Đang thực hiện",
  },
  {
    id: 2,
    title: "Viết tài liệu API",
    internName: "Trần Thị Bình",
    end: "2026-10-14",
    detail: "Mô tả các endpoint chính",
    progress: 10,
    status: "Đang thực hiện",
  },
  {
    id: 3,
    title: "Kiểm tra giao diện responsive",
    internName: "Lê Minh Châu",
    end: "2026-10-01",
    detail: "",
    progress: 100,
    status: "Hoàn thành",
  },
];

function statusClass(status: string) {
  if (status === "Hoàn thành") return "bg-[#edf5ef] text-[#347153]";
  if (status === "Đang thực hiện") return "bg-[#faf1df] text-[#a46a17]";
  if (status === "Quá hạn") return "bg-[#fceceb] text-[#ad4939]";
  if (status === "Tạm dừng") return "bg-[#eef0ed] text-[#68736c]";
  return "bg-[#eef0ed] text-[#68736c]";
}

export default function TaskProgressPage() {
  const [tasks, setTasks] = useState<Task[]>(initialTasks);
  const [search, setSearch] = useState("");
  const [editId, setEditId] = useState<number | null>(null);
  const [progressInput, setProgressInput] = useState(0);
  const [statusInput, setStatusInput] = useState<string>("Đang thực hiện");
  const [toast, setToast] = useState<string | null>(null);

  const filtered = useMemo(() => {
    const q = search.trim().toLocaleLowerCase("vi");
    if (!q) return tasks;
    return tasks.filter((t) =>
      `${t.title} ${t.internName} ${t.status}`.toLocaleLowerCase("vi").includes(q)
    );
  }, [tasks, search]);

  const openProgress = (t: Task) => {
    setEditId(t.id);
    setProgressInput(t.progress);
    setStatusInput(t.status);
  };

  const saveProgress = () => {
    if (editId == null) return;
    const p = Math.max(0, Math.min(100, Number(progressInput) || 0));
    let status = statusInput;
    // Gợi ý đồng bộ: 100% → Hoàn thành; 0% + chưa chọn thì Chưa bắt đầu
    if (p >= 100) status = "Hoàn thành";
    else if (p === 0 && status === "Hoàn thành") status = "Chưa bắt đầu";

    setTasks((prev) =>
      prev.map((t) =>
        t.id === editId ? { ...t, progress: p, status } : t
      )
    );
    setEditId(null);
    setToast(`Đã cập nhật: ${p}% — ${status}.`);
  };

  return (
    <div>
      <div className="mb-6">
        <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
          Công việc
        </p>
        <h1 className="m-0 font-serif text-[32px] font-medium leading-tight text-[#25332d]">
          Tiến độ nhiệm vụ
        </h1>
        <p className="mt-2 mb-0 text-xs text-[#78827c] leading-relaxed">
          Cập nhật % tiến độ (0–100) và trạng thái nhiệm vụ.
        </p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
        {[
          { label: "Tổng nhiệm vụ", value: tasks.length },
          {
            label: "Đang thực hiện",
            value: tasks.filter((t) => t.status === "Đang thực hiện").length,
          },
          {
            label: "Hoàn thành",
            value: tasks.filter((t) => t.status === "Hoàn thành").length,
          },
          {
            label: "Khác",
            value: tasks.filter(
              (t) => !["Đang thực hiện", "Hoàn thành"].includes(t.status)
            ).length,
          },
        ].map((m) => (
          <article
            key={m.label}
            className="min-h-[80px] p-4 border border-[#e1e5df] rounded bg-white"
          >
            <p className="m-0 text-[10px] font-semibold text-[#78827c]">{m.label}</p>
            <p className="m-0 mt-2 text-[26px] font-medium text-[#25332d] font-serif leading-none">
              {m.value}
            </p>
          </article>
        ))}
      </div>

      <div className="mb-3">
        <label className="relative block max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[#89938d]" />
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm tiêu đề, TTS, trạng thái..."
            className="w-full min-h-9 pl-9 pr-3 border border-[#dce1db] rounded-[3px] text-[11px] focus:outline-none focus:border-[#7d998b]"
          />
        </label>
      </div>

      <div className="border border-[#e1e5df] rounded bg-white overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-[#f8f9f6]">
                {["Nhiệm vụ", "Thực tập sinh", "Hạn", "Tiến độ", "Trạng thái", "Thao tác"].map(
                  (h) => (
                    <th
                      key={h}
                      className="h-9 px-3 text-[9px] font-bold tracking-wide uppercase text-[#818b84] border-b border-[#e1e5df] whitespace-nowrap"
                    >
                      {h}
                    </th>
                  )
                )}
              </tr>
            </thead>
            <tbody>
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-10 text-center text-[11px] text-[#78827c]">
                    Không có nhiệm vụ phù hợp.
                  </td>
                </tr>
              ) : (
                filtered.map((t) => (
                  <tr key={t.id} className="border-b border-[#edf0eb] hover:bg-[#fcfcfa]">
                    <td className="px-3 py-3">
                      <p className="m-0 text-[11px] font-bold text-[#26362e]">{t.title}</p>
                      {t.detail && (
                        <p className="m-0 mt-1 text-[9px] text-[#929b94]">{t.detail}</p>
                      )}
                    </td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e]">{t.internName}</td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e] whitespace-nowrap">
                      {t.end}
                    </td>
                    <td className="px-3 py-3 min-w-[120px]">
                      <div className="flex items-center gap-2">
                        <div className="flex-1 h-1.5 rounded-full bg-[#e8ebe6] overflow-hidden">
                          <div
                            className="h-full rounded-full bg-[#173e34]"
                            style={{ width: `${t.progress}%` }}
                          />
                        </div>
                        <span className="text-[10px] font-bold text-[#25332d] w-8 text-right">
                          {t.progress}%
                        </span>
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      <span
                        className={`inline-flex px-2 py-1 rounded-sm text-[9px] font-semibold ${statusClass(t.status)}`}
                      >
                        {t.status}
                      </span>
                    </td>
                    <td className="px-3 py-3 text-right">
                      <button
                        type="button"
                        onClick={() => openProgress(t)}
                        className="min-h-[30px] px-2 rounded-[3px] text-[10px] font-bold text-[#173e34] hover:bg-[#edf5ef] transition"
                      >
                        Cập nhật
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        <div className="px-3 py-2.5 border-t border-[#e1e5df] text-[9px] text-[#89938d]">
          {filtered.length} nhiệm vụ
        </div>
      </div>

      {editId != null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="w-full max-w-sm bg-white rounded border border-[#dce1db] shadow-xl">
            <div className="px-5 pt-5 pb-3 border-b border-[#e1e5df]">
              <h3 className="m-0 font-serif text-[20px] font-medium text-[#25332d]">
                Cập nhật tiến độ & trạng thái
              </h3>
              <p className="mt-1 mb-0 text-[10px] text-[#78827c]">
                Nhập % (0–100) và chọn trạng thái.
              </p>
            </div>
            <div className="p-5 space-y-3">
              <div>
                <label className="block mb-1.5 text-[10px] font-bold text-[#506057]">
                  Tiến độ (%)
                </label>
                <input
                  type="number"
                  min={0}
                  max={100}
                  value={progressInput}
                  onChange={(e) => setProgressInput(Number(e.target.value))}
                  className="w-full min-h-9 px-2.5 border border-[#dce1db] rounded-[3px] text-[11px] focus:outline-none focus:border-[#7d998b]"
                />
              </div>
              <div>
                <label className="block mb-1.5 text-[10px] font-bold text-[#506057]">
                  Trạng thái
                </label>
                <select
                  value={statusInput}
                  onChange={(e) => setStatusInput(e.target.value)}
                  className="w-full min-h-9 px-2.5 border border-[#dce1db] rounded-[3px] text-[11px] bg-white focus:outline-none focus:border-[#7d998b]"
                >
                  {STATUS_OPTIONS.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex justify-end gap-2 pt-1">
                <button
                  type="button"
                  onClick={() => setEditId(null)}
                  className="min-h-9 px-3 rounded-[3px] border border-[#e1e5df] bg-white text-[11px] font-bold text-[#25332d] hover:bg-[#f7f8f5]"
                >
                  Hủy
                </button>
                <button
                  type="button"
                  onClick={saveProgress}
                  className="min-h-9 px-3 rounded-[3px] bg-[#173e34] text-white text-[11px] font-bold hover:bg-[#245748]"
                >
                  Lưu
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {toast && (
        <div className="fixed right-5 bottom-5 z-50 px-4 py-3 rounded border border-[#c9d9cc] bg-white text-[11px] text-[#173e34] shadow-lg">
          {toast}
          <button
            type="button"
            className="ml-3 underline text-[#78827c]"
            onClick={() => setToast(null)}
          >
            Đóng
          </button>
        </div>
      )}
    </div>
  );
}