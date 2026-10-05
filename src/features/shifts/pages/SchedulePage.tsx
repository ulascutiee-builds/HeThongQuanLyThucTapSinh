import { useMemo, useState } from "react";
import { List, CalendarDays, ChevronLeft, ChevronRight } from "lucide-react";

interface Shift {
  id: number;
  title: string;
  internName: string;
  start: string;
  end: string;
  detail: string;
  status: string;
}

const mockShifts: Shift[] = [
  {
    id: 1,
    title: "Ca sáng",
    internName: "Nguyễn Văn An",
    start: "2026-10-06T08:00:00",
    end: "2026-10-06T12:00:00",
    detail: "Làm việc tại văn phòng",
    status: "Đã xếp",
  },
  {
    id: 2,
    title: "Ca chiều",
    internName: "Nguyễn Văn An",
    start: "2026-10-07T13:00:00",
    end: "2026-10-07T17:00:00",
    detail: "Họp nhóm 14:00",
    status: "Đã xếp",
  },
  {
    id: 3,
    title: "Ca sáng",
    internName: "Trần Thị Bình",
    start: "2026-10-06T08:00:00",
    end: "2026-10-06T12:00:00",
    detail: "",
    status: "Đã xếp",
  },
  {
    id: 4,
    title: "Ca sáng",
    internName: "Lê Minh Châu",
    start: "2026-10-08T08:00:00",
    end: "2026-10-08T12:00:00",
    detail: "Onsite",
    status: "Đã xếp",
  },
];

function startOfWeek(d: Date) {
  const x = new Date(d);
  const day = (x.getDay() + 6) % 7;
  x.setDate(x.getDate() - day);
  x.setHours(0, 0, 0, 0);
  return x;
}

function sameDay(a: Date, b: Date) {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

type ViewMode = "list" | "week";

export default function SchedulePage() {
  const [mode, setMode] = useState<ViewMode>("list");
  const [search, setSearch] = useState("");
  const [person, setPerson] = useState("");
  const [weekOffset, setWeekOffset] = useState(0);

  const people = useMemo(
    () => [...new Set(mockShifts.map((s) => s.internName))].sort(),
    []
  );

  const filtered = useMemo(() => {
    const q = search.trim().toLocaleLowerCase("vi");
    return mockShifts.filter((s) => {
      if (person && s.internName !== person) return false;
      if (!q) return true;
      return `${s.title} ${s.internName} ${s.detail}`
        .toLocaleLowerCase("vi")
        .includes(q);
    });
  }, [search, person]);

  const weekDays = useMemo(() => {
    const base = new Date();
    base.setDate(base.getDate() + weekOffset * 7);
    const monday = startOfWeek(base);
    return Array.from({ length: 7 }, (_, i) => {
      const d = new Date(monday);
      d.setDate(monday.getDate() + i);
      return d;
    });
  }, [weekOffset]);

  const dayLabels = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];

  return (
    <div>
      <div className="mb-6">
        <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
          Lịch làm việc
        </p>
        <h1 className="m-0 font-serif text-[32px] font-medium leading-tight text-[#25332d]">
          Lịch cá nhân
        </h1>
        <p className="mt-2 mb-0 text-xs text-[#78827c] leading-relaxed">
          Xem theo từng thực tập sinh (lịch cá nhân) hoặc tất cả; dạng danh sách / lịch tuần.
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-2 mb-4">
        <button
          type="button"
          onClick={() => setMode("list")}
          className={`inline-flex items-center gap-1.5 min-h-9 px-3 rounded-[3px] text-[11px] font-bold border transition ${
            mode === "list"
              ? "bg-[#173e34] text-white border-[#173e34]"
              : "bg-white text-[#25332d] border-[#e1e5df] hover:bg-[#f7f8f5]"
          }`}
        >
          <List className="w-3.5 h-3.5" />
          Danh sách
        </button>
        <button
          type="button"
          onClick={() => setMode("week")}
          className={`inline-flex items-center gap-1.5 min-h-9 px-3 rounded-[3px] text-[11px] font-bold border transition ${
            mode === "week"
              ? "bg-[#173e34] text-white border-[#173e34]"
              : "bg-white text-[#25332d] border-[#e1e5df] hover:bg-[#f7f8f5]"
          }`}
        >
          <CalendarDays className="w-3.5 h-3.5" />
          Lịch tuần
        </button>

        <select
          value={person}
          onChange={(e) => setPerson(e.target.value)}
          className="min-h-9 px-2.5 border border-[#dce1db] rounded-[3px] text-[11px] bg-white focus:outline-none focus:border-[#7d998b]"
          aria-label="Lọc theo thực tập sinh"
        >
          <option value="">Tất cả thực tập sinh</option>
          {people.map((n) => (
            <option key={n} value={n}>
              {n}
            </option>
          ))}
        </select>

        <input
          type="search"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Tìm ca hoặc ghi chú..."
          className="min-h-9 px-3 border border-[#dce1db] rounded-[3px] text-[11px] text-[#25332d] min-w-[180px] flex-1 max-w-xs focus:outline-none focus:border-[#7d998b]"
        />
      </div>

      {mode === "list" && (
        <div className="border border-[#e1e5df] rounded bg-white overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-[#f8f9f6]">
                  {["Ca / tiêu đề", "Thực tập sinh", "Thời gian", "Ghi chú", "Trạng thái"].map(
                    (h) => (
                      <th
                        key={h}
                        className="h-9 px-3 text-[9px] font-bold tracking-wide uppercase text-[#818b84] border-b border-[#e1e5df]"
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
                    <td colSpan={5} className="px-4 py-10 text-center text-[11px] text-[#78827c]">
                      Không có ca phù hợp.
                    </td>
                  </tr>
                ) : (
                  filtered.map((s) => (
                    <tr key={s.id} className="border-b border-[#edf0eb] hover:bg-[#fcfcfa]">
                      <td className="px-3 py-3 text-[11px] font-bold text-[#26362e]">{s.title}</td>
                      <td className="px-3 py-3 text-[10px] text-[#59655e]">{s.internName}</td>
                      <td className="px-3 py-3 text-[10px] text-[#59655e] whitespace-nowrap">
                        {formatDateTime(s.start)} – {formatDateTime(s.end)}
                      </td>
                      <td className="px-3 py-3 text-[10px] text-[#59655e]">{s.detail || "—"}</td>
                      <td className="px-3 py-3">
                        <span className="inline-flex px-2 py-1 rounded-sm text-[9px] font-semibold bg-[#edf5ef] text-[#347153]">
                          {s.status}
                        </span>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
          <div className="px-3 py-2.5 border-t border-[#e1e5df] text-[9px] text-[#89938d]">
            {filtered.length} ca hiển thị
          </div>
        </div>
      )}

      {mode === "week" && (
        <div className="border border-[#e1e5df] rounded bg-white overflow-hidden">
          <div className="flex items-center justify-between gap-2 px-3 py-2.5 border-b border-[#e1e5df] bg-[#f8f9f6]">
            <button
              type="button"
              onClick={() => setWeekOffset((w) => w - 1)}
              className="inline-flex items-center gap-1 min-h-8 px-2 rounded text-[10px] font-bold text-[#173e34] hover:bg-white border border-transparent hover:border-[#e1e5df]"
            >
              <ChevronLeft className="w-4 h-4" />
              Tuần trước
            </button>
            <p className="m-0 text-[11px] font-bold text-[#25332d]">
              Tuần {weekDays[0].getDate()}/{weekDays[0].getMonth() + 1} –{" "}
              {weekDays[6].getDate()}/{weekDays[6].getMonth() + 1}/
              {weekDays[6].getFullYear()}
            </p>
            <button
              type="button"
              onClick={() => setWeekOffset((w) => w + 1)}
              className="inline-flex items-center gap-1 min-h-8 px-2 rounded text-[10px] font-bold text-[#173e34] hover:bg-white border border-transparent hover:border-[#e1e5df]"
            >
              Tuần sau
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse min-w-[700px]">
              <thead>
                <tr className="bg-[#f8f9f6]">
                  {weekDays.map((d, i) => (
                    <th
                      key={i}
                      className="h-9 px-2 text-[9px] font-bold uppercase text-[#818b84] border-b border-[#e1e5df] text-center"
                    >
                      {dayLabels[i]} {d.getDate()}/{d.getMonth() + 1}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                <tr>
                  {weekDays.map((d, i) => {
                    const items = filtered.filter((s) =>
                      sameDay(new Date(s.start), d)
                    );
                    return (
                      <td
                        key={i}
                        className="align-top px-2 py-2 border-r border-[#edf0eb] last:border-0 min-w-[100px]"
                      >
                        {items.length === 0 ? (
                          <span className="text-[10px] text-[#89938d]">—</span>
                        ) : (
                          items.map((s) => (
                            <div
                              key={s.id}
                              className="mb-2 p-2 border border-[#e1e5df] rounded bg-[#fbfcf9] text-[10px] last:mb-0"
                            >
                              <p className="m-0 font-bold text-[#26362e]">{s.title}</p>
                              <p className="m-0 mt-0.5 text-[#59655e]">{s.internName}</p>
                              <p className="m-0 mt-0.5 text-[9px] text-[#89938d]">
                                {formatDateTime(s.start)}
                              </p>
                            </div>
                          ))
                        )}
                      </td>
                    );
                  })}
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}