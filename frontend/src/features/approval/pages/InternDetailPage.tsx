import { useState } from "react";
import { ArrowLeft } from "lucide-react";

const mockDetail = {
  code: "TS001",
  fullName: "Nguyễn Văn A",
  studentId: "B20DCCN001",
  email: "nguyenvana@gmail.com",
  phone: "0912345678",
  dateOfBirth: "15/05/2002",
  gender: "Nam",
  university: "ICTU",
  major: "CNTT",
  department: "Phòng Kỹ thuật",
  startDate: "01/10/2026",
  endDate: "31/12/2026",
  note: "Có kinh nghiệm React, mong muốn thực tập Frontend.",
  status: "pending" as "pending" | "approved" | "rejected",
  documents: [
    { id: 1, name: "CV.pdf" },
    { id: 2, name: "Don_thuc_tap.pdf" },
  ],
  history: [{ time: "28/09 22:10", action: "Tạo hồ sơ" }],
};

interface InternDetailPageProps {
  internId?: number;
  onBack?: () => void;
}

export default function InternDetailPage({ internId, onBack }: InternDetailPageProps) {
  // TODO: GET /api/interns/:id
  const data = mockDetail;
  const [status, setStatus] = useState(data.status);
  const [rejectReason, setRejectReason] = useState("");
  const [history, setHistory] = useState(data.history);
  const [message, setMessage] = useState<string | null>(null);

  const handleApprove = () => {
    setStatus("approved");
    setHistory((h) => [...h, { time: new Date().toLocaleString("vi-VN"), action: "Duyệt hồ sơ" }]);
    setMessage("Đã duyệt hồ sơ thành công!");
  };

  const handleReject = () => {
    if (!rejectReason.trim()) {
      alert("Vui lòng nhập lý do từ chối");
      return;
    }
    setStatus("rejected");
    setHistory((h) => [...h, { time: new Date().toLocaleString("vi-VN"), action: `Từ chối: ${rejectReason}` }]);
    setMessage("Đã từ chối hồ sơ.");
  };

  return (
    <div className="max-w-5xl mx-auto">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-6">
        <div>
          <h1 className="text-xl font-bold text-slate-900">Duyệt hồ sơ thực tập sinh</h1>
          <p className="text-sm text-slate-500 mt-1">HR kiểm tra thông tin, tài liệu và quyết định hồ sơ</p>
        </div>
        <button
          onClick={onBack}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700"
        >
          <ArrowLeft className="w-4 h-4" />
          Danh sách
        </button>
      </div>

      {message && (
        <div className="mb-4 p-3 rounded-lg text-sm bg-emerald-50 text-emerald-700 border border-emerald-200">
          {message}
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-5 gap-5">
        <div className="lg:col-span-3 bg-white rounded-xl border border-slate-200 p-6 shadow-sm">
          <h2 className="text-lg font-bold text-slate-900 mb-4">
            {data.code} – {data.fullName}
          </h2>
          <dl className="space-y-2.5 text-sm">
            {[
              ["MSSV", data.studentId],
              ["Giới tính", data.gender],
              ["Ngày sinh", data.dateOfBirth],
              ["Email", data.email],
              ["SĐT", data.phone],
              ["Trường", data.university],
              ["Ngành", data.major],
              ["Phòng ban", data.department],
              ["Thời gian", `${data.startDate} – ${data.endDate}`],
            ].map(([label, value]) => (
              <div key={label} className="flex gap-3">
                <dt className="w-28 text-slate-500 shrink-0">{label}</dt>
                <dd className="font-medium text-slate-900">{value}</dd>
              </div>
            ))}
          </dl>

          {data.note && (
            <div className="mt-4 pt-4 border-t border-slate-100">
              <p className="text-xs text-slate-500 mb-1">Ghi chú</p>
              <p className="text-sm text-slate-800">{data.note}</p>
            </div>
          )}

          <h3 className="font-semibold text-slate-800 mt-5 mb-2">Tài liệu</h3>
          <div className="space-y-2">
            {data.documents.map((doc) => (
              <div key={doc.id} className="px-3 py-2.5 rounded-lg border border-slate-200 text-sm bg-slate-50">
                {doc.name}
              </div>
            ))}
          </div>
        </div>

        <div className="lg:col-span-2 bg-white rounded-xl border border-slate-200 p-6 shadow-sm">
          <h2 className="font-semibold text-slate-800 mb-4">Quyết định</h2>
          <p className="text-xs text-slate-500 mb-2">Trạng thái hiện tại</p>
          {status === "pending" && (
            <span className="inline-block px-3 py-1 rounded-full text-sm font-medium bg-amber-50 text-amber-700 border border-amber-200 mb-4">
              Chờ duyệt
            </span>
          )}
          {status === "approved" && (
            <span className="inline-block px-3 py-1 rounded-full text-sm font-medium bg-emerald-50 text-emerald-700 border border-emerald-200 mb-4">
              Đã duyệt
            </span>
          )}
          {status === "rejected" && (
            <span className="inline-block px-3 py-1 rounded-full text-sm font-medium bg-red-50 text-red-700 border border-red-200 mb-4">
              Từ chối
            </span>
          )}

          {status === "pending" && (
            <>
              <textarea
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                rows={3}
                placeholder="Ghi chú / lý do từ chối (nếu có)"
                className="w-full px-3 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none mb-4"
              />
              <div className="flex gap-2">
                <button
                  onClick={handleReject}
                  className="flex-1 px-4 py-2 text-sm font-medium text-red-600 border border-red-300 rounded-lg hover:bg-red-50"
                >
                  Từ chối
                </button>
                <button
                  onClick={handleApprove}
                  className="flex-1 px-4 py-2 text-sm font-medium text-white bg-emerald-600 rounded-lg hover:bg-emerald-700"
                >
                  Duyệt hồ sơ
                </button>
              </div>
            </>
          )}

          <h3 className="font-semibold text-slate-800 mt-6 mb-2">Lịch sử xử lý</h3>
          <ul className="space-y-1.5 text-sm text-slate-600">
            {history.map((h, i) => (
              <li key={i}>
                <span className="text-slate-400">{h.time}</span> – {h.action}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}