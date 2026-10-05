import { useMemo, useState } from "react";

interface Program {
  id: number;
  title: string;
  department: string;
  capacity: number;
  status: string;
}

interface Evaluation {
  id: number;
  internName: string;
  department: string;
  programId: number;
  skill: number;
  attitude: number;
  detail: string;
}

const programs: Program[] = [
  {
    id: 1,
    title: "Thực tập Lập trình Web",
    department: "Công nghệ thông tin",
    capacity: 10,
    status: "Đang diễn ra",
  },
  {
    id: 2,
    title: "Thực tập Marketing số",
    department: "Marketing",
    capacity: 6,
    status: "Sắp diễn ra",
  },
  {
    id: 3,
    title: "Thực tập Data Analyst",
    department: "Công nghệ thông tin",
    capacity: 5,
    status: "Đang diễn ra",
  },
];

const evaluations: Evaluation[] = [
  {
    id: 1,
    internName: "Nguyễn Văn An",
    department: "Công nghệ thông tin",
    programId: 1,
    skill: 8,
    attitude: 9,
    detail: "Tiếp thu nhanh, làm việc chủ động.",
  },
  {
    id: 2,
    internName: "Trần Thị Bình",
    department: "Marketing",
    programId: 2,
    skill: 7,
    attitude: 8,
    detail: "Cần cải thiện kỹ năng giao tiếp.",
  },
  {
    id: 3,
    internName: "Lê Minh Châu",
    department: "Công nghệ thông tin",
    programId: 1,
    skill: 9,
    attitude: 8,
    detail: "Hoàn thành task đúng hạn.",
  },
  {
    id: 4,
    internName: "Phạm Quốc Dũng",
    department: "Công nghệ thông tin",
    programId: 3,
    skill: 6,
    attitude: 7,
    detail: "Cần hỗ trợ thêm về SQL.",
  },
];

const selectClass =
  "min-h-9 px-2.5 border border-[#dce1db] rounded-[3px] bg-white text-[11px] text-[#25332d] focus:outline-none focus:border-[#7d998b]";

function programTitle(id: number) {
  return programs.find((p) => p.id === id)?.title ?? "—";
}

export default function EvaluationSummaryPage() {
  const [dept, setDept] = useState("");
  const [programId, setProgramId] = useState("");

  const departments = useMemo(
    () => [...new Set(programs.map((p) => p.department))].sort(),
    []
  );

  const programOptions = useMemo(() => {
    if (!dept) return programs;
    return programs.filter((p) => p.department === dept);
  }, [dept]);

  const filteredPrograms = useMemo(() => {
    return programs.filter((p) => {
      if (dept && p.department !== dept) return false;
      if (programId && p.id !== Number(programId)) return false;
      return true;
    });
  }, [dept, programId]);

  const filteredEvals = useMemo(() => {
    return evaluations.filter((e) => {
      if (dept && e.department !== dept) return false;
      if (programId && e.programId !== Number(programId)) return false;
      return true;
    });
  }, [dept, programId]);

  /** TB theo từng chương trình */
  const avgByProgram = useMemo(() => {
    return filteredPrograms.map((p) => {
      const rows = evaluations.filter((e) => e.programId === p.id);
      const n = rows.length;
      const skill = n ? rows.reduce((s, e) => s + e.skill, 0) / n : 0;
      const attitude = n ? rows.reduce((s, e) => s + e.attitude, 0) / n : 0;
      return { ...p, count: n, skill, attitude };
    });
  }, [filteredPrograms]);

  /** TB theo từng phòng ban */
  const avgByDept = useMemo(() => {
    const map: Record<string, { skill: number; attitude: number; count: number }> = {};
    for (const e of filteredEvals) {
      if (!map[e.department]) map[e.department] = { skill: 0, attitude: 0, count: 0 };
      map[e.department].skill += e.skill;
      map[e.department].attitude += e.attitude;
      map[e.department].count += 1;
    }
    return Object.entries(map).map(([department, v]) => ({
      department,
      count: v.count,
      skill: v.count ? v.skill / v.count : 0,
      attitude: v.count ? v.attitude / v.count : 0,
    }));
  }, [filteredEvals]);

  const avgSkill =
    filteredEvals.length === 0
      ? 0
      : filteredEvals.reduce((s, e) => s + e.skill, 0) / filteredEvals.length;
  const avgAttitude =
    filteredEvals.length === 0
      ? 0
      : filteredEvals.reduce((s, e) => s + e.attitude, 0) / filteredEvals.length;

  return (
    <div>
      <div className="mb-6">
        <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
          Thống kê & đánh giá
        </p>
        <h1 className="m-0 font-serif text-[32px] font-medium leading-tight text-[#25332d]">
          Tổng hợp đánh giá
        </h1>
        <p className="mt-2 mb-0 text-xs text-[#78827c] leading-relaxed">
          Lọc theo phòng ban / chương trình; so sánh điểm TB theo CT và PB.
        </p>
      </div>

      <div className="flex flex-wrap gap-2 mb-5">
        <select
          className={selectClass}
          value={dept}
          onChange={(e) => {
            setDept(e.target.value);
            setProgramId("");
          }}
          aria-label="Lọc phòng ban"
        >
          <option value="">Tất cả phòng ban</option>
          {departments.map((d) => (
            <option key={d} value={d}>
              {d}
            </option>
          ))}
        </select>
        <select
          className={selectClass}
          value={programId}
          onChange={(e) => setProgramId(e.target.value)}
          aria-label="Lọc chương trình"
        >
          <option value="">Tất cả chương trình</option>
          {programOptions.map((p) => (
            <option key={p.id} value={p.id}>
              {p.title}
            </option>
          ))}
        </select>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
        {[
          { label: "Chương trình (lọc)", value: filteredPrograms.length },
          { label: "Số đánh giá", value: filteredEvals.length },
          { label: "TB kỹ năng", value: avgSkill.toFixed(1) },
          { label: "TB thái độ", value: avgAttitude.toFixed(1) },
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

      {/* TB theo chương trình */}
      <h2 className="m-0 mb-3 text-[13px] font-bold text-[#25332d]">
        Trung bình theo chương trình
      </h2>
      <div className="border border-[#e1e5df] rounded bg-white overflow-hidden mb-6">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-[#f8f9f6]">
              {["Chương trình", "Phòng ban", "Số ĐG", "TB kỹ năng", "TB thái độ"].map(
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
            {avgByProgram.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-[11px] text-[#78827c]">
                  Không có chương trình.
                </td>
              </tr>
            ) : (
              avgByProgram.map((r) => (
                <tr key={r.id} className="border-b border-[#edf0eb]">
                  <td className="px-3 py-3 text-[11px] font-bold text-[#26362e]">{r.title}</td>
                  <td className="px-3 py-3 text-[10px] text-[#59655e]">{r.department}</td>
                  <td className="px-3 py-3 text-[10px] text-[#59655e]">{r.count}</td>
                  <td className="px-3 py-3 text-[10px] text-[#59655e]">
                    {r.count ? r.skill.toFixed(1) : "—"}
                  </td>
                  <td className="px-3 py-3 text-[10px] text-[#59655e]">
                    {r.count ? r.attitude.toFixed(1) : "—"}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* TB theo phòng ban */}
      <h2 className="m-0 mb-3 text-[13px] font-bold text-[#25332d]">
        Trung bình theo phòng ban
      </h2>
      <div className="border border-[#e1e5df] rounded bg-white overflow-hidden mb-6">
        <table className="w-full text-left border-collapse">
          <thead>
            <tr className="bg-[#f8f9f6]">
              {["Phòng ban", "Số ĐG", "TB kỹ năng", "TB thái độ"].map((h) => (
                <th
                  key={h}
                  className="h-9 px-3 text-[9px] font-bold tracking-wide uppercase text-[#818b84] border-b border-[#e1e5df]"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {avgByDept.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-[11px] text-[#78827c]">
                  Không có dữ liệu.
                </td>
              </tr>
            ) : (
              avgByDept.map((r) => (
                <tr key={r.department} className="border-b border-[#edf0eb]">
                  <td className="px-3 py-3 text-[11px] font-bold text-[#26362e]">
                    {r.department}
                  </td>
                  <td className="px-3 py-3 text-[10px] text-[#59655e]">{r.count}</td>
                  <td className="px-3 py-3 text-[10px] text-[#59655e]">
                    {r.skill.toFixed(1)}
                  </td>
                  <td className="px-3 py-3 text-[10px] text-[#59655e]">
                    {r.attitude.toFixed(1)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Chi tiết đánh giá + cột Chương trình */}
      <h2 className="m-0 mb-3 text-[13px] font-bold text-[#25332d]">
        Đánh giá cuối kỳ
      </h2>
      <div className="border border-[#e1e5df] rounded bg-white overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-[#f8f9f6]">
                {[
                  "Thực tập sinh",
                  "Phòng ban",
                  "Chương trình",
                  "Kỹ năng",
                  "Thái độ",
                  "Nhận xét",
                ].map((h) => (
                  <th
                    key={h}
                    className="h-9 px-3 text-[9px] font-bold tracking-wide uppercase text-[#818b84] border-b border-[#e1e5df]"
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {filteredEvals.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-10 text-center text-[11px] text-[#78827c]">
                    Chưa có đánh giá phù hợp bộ lọc.
                  </td>
                </tr>
              ) : (
                filteredEvals.map((e) => (
                  <tr key={e.id} className="border-b border-[#edf0eb] hover:bg-[#fcfcfa]">
                    <td className="px-3 py-3 text-[11px] font-bold text-[#26362e]">
                      {e.internName}
                    </td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e]">{e.department}</td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e]">
                      {programTitle(e.programId)}
                    </td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e]">{e.skill}/10</td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e]">{e.attitude}/10</td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e] max-w-[220px]">
                      {e.detail}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        <div className="px-3 py-2.5 border-t border-[#e1e5df] text-[9px] text-[#89938d]">
          {filteredEvals.length} đánh giá
        </div>
      </div>
    </div>
  );
}