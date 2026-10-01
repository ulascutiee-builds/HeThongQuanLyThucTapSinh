import { useState } from "react";
import { ArrowLeft, FileText, CheckCircle, XCircle } from "lucide-react";

// ============================================
// MOCK — Backend thay bằng API workspace / dashboard
// ============================================
const mockDetail: Record<
  number,
  {
    id: number;
    fullName: string;
    studentId: string;
    email: string;
    university: string;
    major: string;
    status: "pending" | "approved" | "rejected";
    createdAt: string;
    documents: { id: number; type: string; fileName: string; status: string }[];
  }
> = {
  1: {
    id: 1,
    fullName: "Nguyễn Văn A",
    studentId: "SV001",
    email: "nguyenvana@gmail.com",
    university: "Đại học Bách Khoa Hà Nội",
    major: "Công nghệ thông tin",
    status: "pending",
    createdAt: "2026-09-20",
    documents: [
      { id: 11, type: "CV", fileName: "CV_NguyenVanA.pdf", status: "Chờ duyệt" },
      { id: 12, type: "Đơn xin thực tập", fileName: "Don_xin_TTS.pdf", status: "Chờ duyệt" },
    ],
  },
  2: {
    id: 2,
    fullName: "Trần Thị B",
    studentId: "SV002",
    email: "tranthib@gmail.com",
    university: "Đại học FPT",
    major: "Kỹ thuật phần mềm",
    status: "pending",
    createdAt: "2026-09-21",
    documents: [
      { id: 21, type: "CV", fileName: "CV_TranThiB.pdf", status: "Chờ duyệt" },
      { id: 22, type: "Đơn xin thực tập", fileName: "Don_TranThiB.pdf", status: "Chờ duyệt" },
    ],
  },
  3: {
    id: 3,
    fullName: "Lê Văn C",
    studentId: "SV003",
    email: "levanc@gmail.com",
    university: "Đại học Quốc gia",
    major: "An toàn thông tin",
    status: "approved",
    createdAt: "2026-09-18",
    documents: [
      { id: 31, type: "CV", fileName: "CV_LeVanC.pdf", status: "Đã duyệt" },
      { id: 32, type: "Đơn xin thực tập", fileName: "Don_LeVanC.pdf", status: "Đã duyệt" },
    ],
  },
  4: {
    id: 4,
    fullName: "Phạm Thị D",
    studentId: "SV004",
    email: "phamthid@gmail.com",
    university: "Đại học Kinh tế",
    major: "Quản trị kinh doanh",
    status: "rejected",
    createdAt: "2026-09-15",
    documents: [
      { id: 41, type: "CV", fileName: "CV_PhamThiD.pdf", status: "Từ chối" },
      { id: 42, type: "Đơn xin thực tập", fileName: "Don_PhamThiD.pdf", status: "Từ chối" },
    ],
  },
};

interface InternDetailPageProps {
  internId?: number;
  onBack?: () => void;
}

export default function InternDetailPage({ internId = 1, onBack }: InternDetailPageProps) {
  const data = mockDetail[internId] ?? mockDetail[1];
  const [status, setStatus] = useState(data.status);
  const [showReject, setShowReject] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [toast, setToast] = useState<string | null>(null);

  const handleApprove = () => {
    // TODO: Backend PATCH /api/hr/applications/{id} { status: "Đã duyệt" }
    setStatus("approved");
    setToast("Đã duyệt hồ sơ (mock).");
  };

  const handleReject = () => {
    if (!rejectReason.trim()) {
      setToast("Vui lòng nhập lý do từ chối.");
      return;
    }
    // TODO: Backend PATCH ... { status: "Từ chối", note: rejectReason }
    setStatus("rejected");
    setShowReject(false);
    setToast("Đã từ chối hồ sơ (mock).");
  };

  const statusLabel =
    status === "pending" ? "Chờ duyệt" : status === "approved" ? "Đã duyệt" : "Từ chối";
  const statusClass =
    status === "pending"
      ? "bg-[#faf1df] text-[#a46a17]"
      : status === "approved"
        ? "bg-[#edf5ef] text-[#347153]"
        : "bg-[#f8e9e6] text-[#ad4939]";

  return (
    <div>
      {/* Top actions */}
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
        <button
          type="button"
          onClick={onBack}
          className="inline-flex items-center gap-1.5 min-h-9 px-3 rounded-[3px] border border-[#e1e5df] bg-white text-[11px] font-bold text-[#25332d] hover:bg-[#f7f8f5] transition"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          Quay lại danh sách
        </button>
        <span className={`inline-flex items-center px-2.5 py-1 rounded-sm text-[9px] font-semibold ${statusClass}`}>
          {statusLabel}
        </span>
      </div>

      <div className="mb-6">
        <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
          Chi tiết ứng viên
        </p>
        <h1 className="m-0 text-[32px] font-medium leading-tight text-[#25332d]">{data.fullName}</h1>
        <p className="mt-2 mb-0 text-xs text-[#78827c]">
          {data.studentId} · Nộp ngày {data.createdAt}
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-[1fr_320px] gap-4 items-start">
        {/* Cột trái: thông tin + tài liệu */}
        <div className="space-y-4">
          <section className="bg-white border border-[#e1e5df] rounded overflow-hidden">
            <div className="px-4 py-3 border-b border-[#e1e5df]">
              <h2 className="m-0 text-[12px] font-bold text-[#25332d]">Thông tin hồ sơ</h2>
            </div>
            <dl className="p-4 grid grid-cols-1 sm:grid-cols-2 gap-3 text-[11px]">
              {[
                ["Họ và tên", data.fullName],
                ["Mã sinh viên", data.studentId],
                ["Email", data.email],
                ["Trường", data.university],
                ["Ngành", data.major],
                ["Ngày nộp", data.createdAt],
              ].map(([label, value]) => (
                <div key={label}>
                  <dt className="text-[9px] font-bold uppercase tracking-wide text-[#8a948d] mb-1">
                    {label}
                  </dt>
                  <dd className="m-0 font-semibold text-[#344239]">{value}</dd>
                </div>
              ))}
            </dl>
          </section>

          <section className="bg-white border border-[#e1e5df] rounded overflow-hidden">
            <div className="px-4 py-3 border-b border-[#e1e5df]">
              <h2 className="m-0 text-[12px] font-bold text-[#25332d]">Tài liệu đính kèm</h2>
              <p className="m-0 mt-1 text-[10px] text-[#78827c]">CV và đơn xin thực tập (mock preview)</p>
            </div>
            <div className="p-4 space-y-2">
              {data.documents.map((doc) => (
                <div
                  key={doc.id}
                  className="flex items-center justify-between gap-3 p-3 border border-[#e1e5df] rounded bg-[#fbfcf9]"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <span className="w-9 h-10 flex items-center justify-center border border-[#eadfd8] rounded bg-[#fff9f6] text-[#c76e49]">
                      <FileText className="w-4 h-4" />
                    </span>
                    <div className="min-w-0">
                      <p className="m-0 text-[9px] font-bold uppercase text-[#c76e49]">{doc.type}</p>
                      <p className="m-0 text-[11px] font-bold text-[#3c4941] truncate">{doc.fileName}</p>
                    </div>
                  </div>
                  <span className="text-[9px] font-semibold text-[#68736c] whitespace-nowrap">
                    {doc.status}
                  </span>
                </div>
              ))}
            </div>
          </section>
        </div>

        {/* Cột phải: quyết định */}
        <aside className="bg-white border border-[#e1e5df] rounded overflow-hidden lg:sticky lg:top-20">
          <div className="px-4 py-3 border-b border-[#e1e5df]">
            <h2 className="m-0 text-[12px] font-bold text-[#25332d]">Quyết định xét duyệt</h2>
            <p className="m-0 mt-1 text-[10px] text-[#78827c]">
              Duyệt hoặc từ chối có lý do (giống demo HR).
            </p>
          </div>
          <div className="p-4 space-y-3">
            {status === "pending" ? (
              <>
                <button
                  type="button"
                  onClick={handleApprove}
                  className="w-full inline-flex items-center justify-center gap-2 min-h-10 rounded-[3px] bg-[#173e34] text-white text-[11px] font-bold hover:bg-[#245748] transition"
                >
                  <CheckCircle className="w-4 h-4" />
                  Duyệt hồ sơ
                </button>
                <button
                  type="button"
                  onClick={() => setShowReject(true)}
                  className="w-full inline-flex items-center justify-center gap-2 min-h-10 rounded-[3px] border border-[#edd4cd] bg-[#fff9f7] text-[#ad4939] text-[11px] font-bold hover:bg-[#fff0ec] transition"
                >
                  <XCircle className="w-4 h-4" />
                  Từ chối
                </button>
              </>
            ) : (
              <p className="m-0 text-[11px] text-[#78827c] leading-relaxed">
                Hồ sơ đã ở trạng thái <strong className="text-[#25332d]">{statusLabel}</strong>.
                Thao tác chỉ mock trên giao diện Sprint 1.
              </p>
            )}
          </div>
        </aside>
      </div>

      {toast && (
        <div className="fixed right-5 bottom-5 z-50 max-w-sm px-4 py-3 rounded border border-[#c9d9cc] bg-white text-[11px] text-[#173e34] shadow-lg">
          {toast}
          <button
            type="button"
            className="ml-3 text-[#78827c] underline"
            onClick={() => setToast(null)}
          >
            Đóng
          </button>
        </div>
      )}

      {/* Modal từ chối */}
      {showReject && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="w-full max-w-md bg-white rounded border border-[#dce1db] shadow-xl">
            <div className="px-5 pt-5 pb-3 border-b border-[#e1e5df]">
              <h3 className="m-0 text-[18px] font-medium text-[#25332d]">Lý do từ chối</h3>
              <p className="mt-1 mb-0 text-[10px] text-[#78827c]">
                Bắt buộc nhập lý do để thông báo ứng viên (theo nghiệp vụ demo).
              </p>
            </div>
            <div className="p-5">
              <textarea
                value={rejectReason}
                onChange={(e) => setRejectReason(e.target.value)}
                rows={4}
                placeholder="Ví dụ: Hồ sơ chưa đầy đủ, không phù hợp vị trí..."
                className="w-full px-3 py-2.5 border border-[#dce1db] rounded-[3px] text-[11px] text-[#25332d] resize-none focus:outline-none focus:border-[#7d998b] focus:ring-[3px] focus:ring-[#ad4939]/15"
              />
              <div className="flex justify-end gap-2 mt-4">
                <button
                  type="button"
                  onClick={() => setShowReject(false)}
                  className="min-h-9 px-3 rounded-[3px] border border-[#e1e5df] bg-white text-[11px] font-bold text-[#25332d] hover:bg-[#f7f8f5]"
                >
                  Hủy
                </button>
                <button
                  type="button"
                  onClick={handleReject}
                  className="min-h-9 px-3 rounded-[3px] bg-[#ad4939] text-white text-[11px] font-bold hover:bg-[#963f32]"
                >
                  Xác nhận từ chối
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}