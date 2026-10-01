import { useState } from "react";
import { Search, Eye, CheckCircle, XCircle, Clock } from "lucide-react";

// ============================================
// MOCK DATA — Backend thay bằng API GET /api/hr/dashboard
// ============================================
const mockInterns = [
  {
    id: 1,
    fullName: "Nguyễn Văn A",
    studentId: "SV001",
    email: "nguyenvana@gmail.com",
    university: "Đại học Bách Khoa Hà Nội",
    major: "Công nghệ thông tin",
    status: "pending" as const,
    createdAt: "2026-09-20",
  },
  {
    id: 2,
    fullName: "Trần Thị B",
    studentId: "SV002",
    email: "tranthib@gmail.com",
    university: "Đại học FPT",
    major: "Kỹ thuật phần mềm",
    status: "pending" as const,
    createdAt: "2026-09-21",
  },
  {
    id: 3,
    fullName: "Lê Văn C",
    studentId: "SV003",
    email: "levanc@gmail.com",
    university: "Đại học Quốc gia",
    major: "An toàn thông tin",
    status: "approved" as const,
    createdAt: "2026-09-18",
  },
  {
    id: 4,
    fullName: "Phạm Thị D",
    studentId: "SV004",
    email: "phamthid@gmail.com",
    university: "Đại học Kinh tế",
    major: "Quản trị kinh doanh",
    status: "rejected" as const,
    createdAt: "2026-09-15",
  },
];

const statusMap = {
  pending: {
    label: "Chờ duyệt",
    className: "bg-[#faf1df] text-[#a46a17]",
    icon: Clock,
  },
  approved: {
    label: "Đã duyệt",
    className: "bg-[#edf5ef] text-[#347153]",
    icon: CheckCircle,
  },
  rejected: {
    label: "Từ chối",
    className: "bg-[#f8e9e6] text-[#ad4939]",
    icon: XCircle,
  },
};

interface ApprovalListPageProps {
  onViewDetail?: (id: number) => void;
}

export default function ApprovalListPage({ onViewDetail }: ApprovalListPageProps) {
  const [filter, setFilter] = useState<"all" | "pending" | "approved" | "rejected">("pending");
  const [search, setSearch] = useState("");

  const filtered = mockInterns.filter((item) => {
    const matchStatus = filter === "all" || item.status === filter;
    const q = search.trim().toLocaleLowerCase("vi");
    const matchSearch =
      !q ||
      `${item.fullName} ${item.studentId} ${item.email} ${item.university} ${item.major}`
        .toLocaleLowerCase("vi")
        .includes(q);
    return matchStatus && matchSearch;
  });

  const countPending = mockInterns.filter((x) => x.status === "pending").length;
  const countApproved = mockInterns.filter((x) => x.status === "approved").length;
  const countRejected = mockInterns.filter((x) => x.status === "rejected").length;

  return (
    <div>
      {/* Heading */}
      <div className="flex items-end justify-between gap-4 mb-6">
        <div>
          <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
            Tiếp nhận hồ sơ
          </p>
          <h1 className="m-0 text-[32px] font-medium leading-tight text-[#25332d]">
            Duyệt hồ sơ thực tập
          </h1>
          <p className="mt-2 mb-0 text-xs text-[#78827c] leading-relaxed">
            Xem danh sách ứng viên và chuyển sang chi tiết để duyệt hoặc từ chối.
          </p>
        </div>
      </div>

      {/* Metrics — giống demo */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
        {[
          { label: "Tổng hồ sơ", value: mockInterns.length, hint: "trong danh sách" },
          { label: "Chờ duyệt", value: countPending, hint: "cần xử lý", accent: true },
          { label: "Đã duyệt", value: countApproved, hint: "ứng viên phù hợp" },
          { label: "Từ chối", value: countRejected, hint: "đã phản hồi" },
        ].map((m) => (
          <article
            key={m.label}
            className="min-h-[91px] p-4 border border-[#e1e5df] rounded bg-white"
          >
            <p className="m-0 text-[10px] font-semibold text-[#78827c]">{m.label}</p>
            <div className="mt-3 flex items-baseline justify-between gap-2">
              <p
                className={`m-0 text-[28px] leading-none font-medium ${
                  m.accent ? "text-[#c76e49]" : "text-[#25332d]"
                }`}
              >
                {m.value}
              </p>
              <span className="text-[9px] text-[#89928c]">{m.hint}</span>
            </div>
          </article>
        ))}
      </div>

      {/* Toolbar + filter */}
      <div className="mb-0">
        <div className="flex flex-wrap items-center justify-between gap-3 mb-3">
          <div>
            <h2 className="m-0 text-[13px] font-bold text-[#25332d]">Danh sách hồ sơ</h2>
            <p className="m-0 mt-1 text-[10px] text-[#78827c]">
              Tìm theo tên, mã SV, email, trường hoặc ngành.
            </p>
          </div>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-[1fr_auto] gap-2 p-3 border border-[#e1e5df] border-b-0 rounded-t bg-white">
          <label className="relative block">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[#89938d] pointer-events-none" />
            <input
              type="search"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Tìm tên, mã sinh viên, email..."
              className="w-full min-h-9 pl-9 pr-3 border border-[#dce1db] rounded-[3px] text-[11px] text-[#25332d] focus:outline-none focus:border-[#7d998b] focus:ring-[3px] focus:ring-[#173e34]/10"
            />
          </label>
          <div className="flex flex-wrap gap-1.5">
            {(
              [
                ["pending", "Chờ duyệt"],
                ["approved", "Đã duyệt"],
                ["rejected", "Từ chối"],
                ["all", "Tất cả"],
              ] as const
            ).map(([key, label]) => (
              <button
                key={key}
                type="button"
                onClick={() => setFilter(key)}
                className={`min-h-9 px-3 rounded-[3px] text-[11px] font-bold border transition ${
                  filter === key
                    ? "bg-[#173e34] text-white border-[#173e34]"
                    : "bg-white text-[#25332d] border-[#e1e5df] hover:bg-[#f7f8f5]"
                }`}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Table */}
      <div className="border border-[#e1e5df] rounded-b bg-white overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-[#f8f9f6]">
                {["Thực tập sinh", "Trường / Ngành", "Trạng thái", "Ngày nộp", "Thao tác"].map(
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
                  <td colSpan={5} className="px-4 py-10 text-center text-[11px] text-[#78827c]">
                    Không có hồ sơ phù hợp. Thử đổi bộ lọc hoặc từ khóa tìm kiếm.
                  </td>
                </tr>
              ) : (
                filtered.map((item) => {
                  const st = statusMap[item.status];
                  const Icon = st.icon;
                  return (
                    <tr key={item.id} className="hover:bg-[#fcfcfa] border-b border-[#edf0eb] last:border-0">
                      <td className="px-3 py-3 align-middle min-w-[180px]">
                        <p className="m-0 text-[11px] font-bold text-[#26362e]">{item.fullName}</p>
                        <p className="m-0 mt-1 text-[9px] text-[#929b94]">
                          {item.studentId} · {item.email}
                        </p>
                      </td>
                      <td className="px-3 py-3 align-middle text-[10px] text-[#59655e]">
                        <div>{item.university}</div>
                        <div className="text-[9px] text-[#929b94] mt-0.5">{item.major}</div>
                      </td>
                      <td className="px-3 py-3 align-middle">
                        <span
                          className={`inline-flex items-center gap-1.5 px-2 py-1 rounded-sm text-[9px] font-semibold ${st.className}`}
                        >
                          <Icon className="w-3 h-3" />
                          {st.label}
                        </span>
                      </td>
                      <td className="px-3 py-3 align-middle text-[10px] text-[#59655e] whitespace-nowrap">
                        {item.createdAt}
                      </td>
                      <td className="px-3 py-3 align-middle text-right">
                        <button
                          type="button"
                          onClick={() => onViewDetail?.(item.id)}
                          className="inline-flex items-center gap-1.5 min-h-[30px] px-2 text-[10px] font-bold text-[#52635a] hover:bg-[#f7f8f5] rounded transition"
                        >
                          <Eye className="w-3.5 h-3.5" />
                          Xem
                        </button>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
        <div className="px-3 py-2.5 border-t border-[#e1e5df] text-[9px] text-[#89938d]">
          {filtered.length} hồ sơ hiển thị · {mockInterns.length} tổng số
        </div>
      </div>
    </div>
  );
}