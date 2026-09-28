import { useState } from "react";
import { Search, Eye, CheckCircle, XCircle, Clock } from "lucide-react";

const mockInterns = [
  { id: 1, code: "TS001", fullName: "Nguyễn Văn A", email: "nguyenvana@gmail.com", university: "ICTU", major: "CNTT", status: "pending", createdAt: "2026-09-20" },
  { id: 2, code: "TS002", fullName: "Trần Thị B", email: "tranthib@gmail.com", university: "FPT", major: "Phần mềm", status: "pending", createdAt: "2026-09-21" },
  { id: 3, code: "TS003", fullName: "Lê Văn C", email: "levanc@gmail.com", university: "ĐHQG", major: "ATTT", status: "approved", createdAt: "2026-09-18" },
  { id: 4, code: "TS004", fullName: "Phạm Thị D", email: "phamthid@gmail.com", university: "KT", major: "QTKD", status: "rejected", createdAt: "2026-09-15" },
];

const statusMap = {
  pending: { label: "Chờ duyệt", color: "bg-amber-50 text-amber-700 border-amber-200", icon: Clock },
  approved: { label: "Đã duyệt", color: "bg-emerald-50 text-emerald-700 border-emerald-200", icon: CheckCircle },
  rejected: { label: "Từ chối", color: "bg-red-50 text-red-700 border-red-200", icon: XCircle },
};

interface ApprovalListPageProps {
  onViewDetail?: (id: number) => void;
}

export default function ApprovalListPage({ onViewDetail }: ApprovalListPageProps) {
  const [filter, setFilter] = useState<"all" | "pending" | "approved" | "rejected">("all");
  const [search, setSearch] = useState("");

  const filteredData = mockInterns.filter((item) => {
    const matchStatus = filter === "all" ? true : item.status === filter;
    const q = search.toLowerCase();
    const matchSearch =
      item.fullName.toLowerCase().includes(q) ||
      item.code.toLowerCase().includes(q) ||
      item.email.toLowerCase().includes(q) ||
      item.university.toLowerCase().includes(q);
    return matchStatus && matchSearch;
  });

  return (
    <div className="max-w-6xl mx-auto">
      <div className="mb-6">
        <h1 className="text-xl font-bold text-slate-900">Duyệt hồ sơ thực tập sinh</h1>
        <p className="text-sm text-slate-500 mt-1">Tìm kiếm / lọc theo trường, ngành, trạng thái</p>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 p-4 mb-4 shadow-sm flex flex-col md:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm theo tên, mã, email, trường..."
            className="w-full pl-10 pr-4 py-2.5 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div className="flex gap-2 flex-wrap">
          {[
            { key: "all", label: "Tất cả" },
            { key: "pending", label: "Chờ duyệt" },
            { key: "approved", label: "Đã duyệt" },
            { key: "rejected", label: "Từ chối" },
          ].map((tab) => (
            <button
              key={tab.key}
              onClick={() => setFilter(tab.key as any)}
              className={`px-3 py-2 rounded-lg text-sm font-medium transition ${
                filter === tab.key
                  ? "bg-blue-600 text-white"
                  : "bg-slate-100 text-slate-600 hover:bg-slate-200"
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        {filteredData.length === 0 ? (
          <div className="py-14 text-center text-slate-500 text-sm">Không tìm thấy hồ sơ nào</div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="text-left px-5 py-3 font-medium text-slate-600">Mã / Họ tên</th>
                <th className="text-left px-5 py-3 font-medium text-slate-600">Trường / Ngành</th>
                <th className="text-left px-5 py-3 font-medium text-slate-600">Trạng thái</th>
                <th className="text-left px-5 py-3 font-medium text-slate-600">Ngày tạo</th>
                <th className="text-right px-5 py-3 font-medium text-slate-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filteredData.map((item) => {
                const st = statusMap[item.status as keyof typeof statusMap];
                const Icon = st.icon;
                return (
                  <tr key={item.id} className="hover:bg-slate-50">
                    <td className="px-5 py-3">
                      <div className="font-medium text-slate-900">
                        {item.code} – {item.fullName}
                      </div>
                      <div className="text-xs text-slate-400">{item.email}</div>
                    </td>
                    <td className="px-5 py-3">
                      <div className="text-slate-800">{item.university}</div>
                      <div className="text-xs text-slate-500">{item.major}</div>
                    </td>
                    <td className="px-5 py-3">
                      <span className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-medium border ${st.color}`}>
                        <Icon className="w-3.5 h-3.5" />
                        {st.label}
                      </span>
                    </td>
                    <td className="px-5 py-3 text-slate-600">{item.createdAt}</td>
                    <td className="px-5 py-3 text-right">
                      <button
                        onClick={() => onViewDetail?.(item.id)}
                        className="inline-flex items-center gap-1 px-3 py-1.5 text-sm text-blue-600 hover:bg-blue-50 rounded-lg"
                      >
                        <Eye className="w-4 h-4" />
                        Xem
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}