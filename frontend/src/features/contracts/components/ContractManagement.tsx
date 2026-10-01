import { useState } from "react";
import {
  ArrowLeft,
  FileText,
  Upload,
  Download,
  CheckCircle,
  Clock,
  Search,
} from "lucide-react";

// ============================================
// MOCK — Backend: profiles + documents type "Hợp đồng thực tập"
// ============================================
type DocStatus = "Chờ xác nhận" | "Đã xác nhận" | "Chờ duyệt" | "Đã duyệt";

interface InternRow {
  id: number;
  fullName: string;
  studentId: string;
  email: string;
}

interface ContractDoc {
  id: number;
  internId: number;
  type: string;
  fileName: string;
  status: DocStatus;
  uploadedAt: string;
}

const mockInterns: InternRow[] = [
  { id: 1, fullName: "Nguyễn Văn A", studentId: "SV001", email: "nguyenvana@gmail.com" },
  { id: 2, fullName: "Trần Thị B", studentId: "SV002", email: "tranthib@gmail.com" },
  { id: 3, fullName: "Lê Văn C", studentId: "SV003", email: "levanc@gmail.com" },
];

const mockContracts: ContractDoc[] = [
  {
    id: 101,
    internId: 1,
    type: "Hợp đồng thực tập",
    fileName: "HD_NguyenVanA.pdf",
    status: "Chờ xác nhận",
    uploadedAt: "2026-09-22",
  },
  {
    id: 102,
    internId: 3,
    type: "Hợp đồng thực tập",
    fileName: "HD_LeVanC.pdf",
    status: "Đã xác nhận",
    uploadedAt: "2026-09-19",
  },
];

interface ContractManagementProps {
  /** Giữ prop cũ để App.tsx không vỡ; màn này dùng danh sách nội bộ */
  internId?: number;
  internName?: string;
}

export default function ContractManagement(_props: ContractManagementProps) {
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [search, setSearch] = useState("");
  const [contracts, setContracts] = useState(mockContracts);
  const [toast, setToast] = useState<string | null>(null);

  const selected = mockInterns.find((x) => x.id === selectedId) ?? null;

  const filteredInterns = mockInterns.filter((item) => {
    const q = search.trim().toLocaleLowerCase("vi");
    if (!q) return true;
    return `${item.fullName} ${item.studentId} ${item.email}`
      .toLocaleLowerCase("vi")
      .includes(q);
  });

  const docsOfSelected = selected
    ? contracts.filter((c) => c.internId === selected.id)
    : [];

  const handleUpload = (file: File | null) => {
    if (!selected || !file) return;
    const ok = /\.(pdf|doc|docx)$/i.test(file.name);
    if (!ok) {
      setToast("Chỉ hỗ trợ PDF, DOC, DOCX.");
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      setToast("Tệp vượt quá 10 MB.");
      return;
    }
    // TODO: Backend POST /api/hr/profiles/{id}/contract
    const next: ContractDoc = {
      id: Date.now(),
      internId: selected.id,
      type: "Hợp đồng thực tập",
      fileName: file.name,
      status: "Chờ xác nhận",
      uploadedAt: new Date().toISOString().slice(0, 10),
    };
    setContracts((prev) => [
      next,
      ...prev.filter((c) => !(c.internId === selected.id && c.type === "Hợp đồng thực tập")),
    ]);
    setToast(`Đã tải ${file.name} (mock).`);
  };

  const handleConfirm = (docId: number) => {
    // TODO: Backend confirm-contract (phía TTS) — HR chỉ xem trạng thái ở Sprint 1
    setContracts((prev) =>
      prev.map((c) => (c.id === docId ? { ...c, status: "Đã xác nhận" as const } : c))
    );
    setToast("Đã cập nhật trạng thái hợp đồng (mock).");
  };

  const statusClass = (s: DocStatus) => {
    if (s === "Đã xác nhận" || s === "Đã duyệt") return "bg-[#edf5ef] text-[#347153]";
    if (s === "Chờ xác nhận" || s === "Chờ duyệt") return "bg-[#faf1df] text-[#a46a17]";
    return "bg-[#eef0ed] text-[#68736c]";
  };

  // ===== LIST VIEW =====
  if (!selected) {
    return (
      <div>
        <div className="mb-6">
          <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
            Kiểm tra & lưu trữ
          </p>
          <h1 className="m-0 text-[32px] font-medium leading-tight text-[#25332d]">
            Tài liệu & hợp đồng
          </h1>
          <p className="mt-2 mb-0 text-xs text-[#78827c] leading-relaxed">
            Chọn thực tập sinh để xem / tải hợp đồng thực tập.
          </p>
        </div>

        <div className="mb-3 p-3 border border-[#e1e5df] rounded bg-[#fbf7ee] text-[10px] text-[#736342] leading-relaxed border-l-[3px] border-l-[#d5aa65]">
          Hợp đồng do HR tải lên. Tệp mới sẽ thay hợp đồng hiện tại của hồ sơ (giống demo).
        </div>

        <div className="border border-[#e1e5df] rounded bg-white overflow-hidden">
          <div className="p-3 border-b border-[#e1e5df]">
            <label className="relative block max-w-md">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[#89938d]" />
              <input
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Tìm tên, mã sinh viên..."
                className="w-full min-h-9 pl-9 pr-3 border border-[#dce1db] rounded-[3px] text-[11px] focus:outline-none focus:border-[#7d998b]"
              />
            </label>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-[#f8f9f6]">
                  {["Thực tập sinh", "Email", "Hợp đồng", "Thao tác"].map((h) => (
                    <th
                      key={h}
                      className="h-9 px-3 text-[9px] font-bold uppercase tracking-wide text-[#818b84] border-b border-[#e1e5df]"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {filteredInterns.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="px-4 py-10 text-center text-[11px] text-[#78827c]">
                      Không tìm thấy thực tập sinh.
                    </td>
                  </tr>
                ) : (
                  filteredInterns.map((item) => {
                    const doc = contracts.find((c) => c.internId === item.id);
                    return (
                      <tr key={item.id} className="border-b border-[#edf0eb] hover:bg-[#fcfcfa]">
                        <td className="px-3 py-3">
                          <p className="m-0 text-[11px] font-bold text-[#26362e]">{item.fullName}</p>
                          <p className="m-0 mt-1 text-[9px] text-[#929b94]">{item.studentId}</p>
                        </td>
                        <td className="px-3 py-3 text-[10px] text-[#59655e]">{item.email}</td>
                        <td className="px-3 py-3">
                          {doc ? (
                            <span
                              className={`inline-flex items-center gap-1 px-2 py-1 rounded-sm text-[9px] font-semibold ${statusClass(doc.status)}`}
                            >
                              {doc.status === "Đã xác nhận" ? (
                                <CheckCircle className="w-3 h-3" />
                              ) : (
                                <Clock className="w-3 h-3" />
                              )}
                              {doc.status}
                            </span>
                          ) : (
                            <span className="text-[10px] text-[#89938d]">Chưa có</span>
                          )}
                        </td>
                        <td className="px-3 py-3 text-right">
                          <button
                            type="button"
                            onClick={() => setSelectedId(item.id)}
                            className="min-h-[30px] px-2 text-[10px] font-bold text-[#173e34] hover:bg-[#edf5ef] rounded transition"
                          >
                            Quản lý
                          </button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </div>

        {toast && (
          <div className="fixed right-5 bottom-5 z-50 px-4 py-3 rounded border border-[#c9d9cc] bg-white text-[11px] text-[#173e34] shadow-lg">
            {toast}
            <button type="button" className="ml-3 underline text-[#78827c]" onClick={() => setToast(null)}>
              Đóng
            </button>
          </div>
        )}
      </div>
    );
  }

  // ===== DETAIL VIEW (theo 1 TTS) =====
  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3 mb-6">
        <button
          type="button"
          onClick={() => setSelectedId(null)}
          className="inline-flex items-center gap-1.5 min-h-9 px-3 rounded-[3px] border border-[#e1e5df] bg-white text-[11px] font-bold text-[#25332d] hover:bg-[#f7f8f5]"
        >
          <ArrowLeft className="w-3.5 h-3.5" />
          Danh sách
        </button>
      </div>

      <div className="mb-6">
        <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
          Hợp đồng thực tập
        </p>
        <h1 className="m-0 text-[28px] font-medium text-[#25332d]">{selected.fullName}</h1>
        <p className="mt-1 mb-0 text-xs text-[#78827c]">
          {selected.studentId} · {selected.email}
        </p>
      </div>

      {/* Upload */}
      <section className="bg-white border border-[#e1e5df] rounded mb-4 overflow-hidden">
        <div className="px-4 py-3 border-b border-[#e1e5df]">
          <h2 className="m-0 text-[12px] font-bold">Tải lên hợp đồng thực tập</h2>
          <p className="m-0 mt-1 text-[10px] text-[#78827c]">PDF, DOC, DOCX — tối đa 10 MB</p>
        </div>
        <div className="p-4 flex flex-wrap items-end gap-3">
          <label className="flex-1 min-w-[200px]">
            <span className="block mb-1.5 text-[10px] font-bold text-[#506057]">Chọn tệp</span>
            <input
              type="file"
              accept=".pdf,.doc,.docx"
              className="w-full text-[11px] file:mr-3 file:min-h-9 file:px-3 file:rounded-[3px] file:border-0 file:bg-[#173e34] file:text-white file:text-[11px] file:font-bold"
              onChange={(e) => {
                handleUpload(e.target.files?.[0] ?? null);
                e.target.value = "";
              }}
            />
          </label>
          <span className="inline-flex items-center gap-1.5 text-[10px] text-[#78827c] min-h-9">
            <Upload className="w-3.5 h-3.5" />
            File mới thay hợp đồng cũ
          </span>
        </div>
      </section>

      {/* Danh sách hợp đồng */}
      <section className="bg-white border border-[#e1e5df] rounded overflow-hidden">
        <div className="px-4 py-3 border-b border-[#e1e5df]">
          <h2 className="m-0 text-[12px] font-bold">Hợp đồng hiện có</h2>
        </div>
        <div className="p-4 space-y-2">
          {docsOfSelected.length === 0 ? (
            <p className="m-0 py-8 text-center text-[11px] text-[#78827c]">
              Chưa có hợp đồng. Hãy tải tệp lên.
            </p>
          ) : (
            docsOfSelected.map((doc) => (
              <div
                key={doc.id}
                className="flex flex-wrap items-center justify-between gap-3 p-3 border border-[#e1e5df] rounded bg-[#fbfcf9]"
              >
                <div className="flex items-center gap-3 min-w-0">
                  <span className="w-9 h-10 flex items-center justify-center border border-[#eadfd8] rounded bg-[#fff9f6] text-[#c76e49]">
                    <FileText className="w-4 h-4" />
                  </span>
                  <div className="min-w-0">
                    <p className="m-0 text-[9px] font-bold uppercase text-[#c76e49]">{doc.type}</p>
                    <p className="m-0 text-[11px] font-bold text-[#3c4941] truncate">{doc.fileName}</p>
                    <p className="m-0 mt-0.5 text-[9px] text-[#8a948d]">Tải lên {doc.uploadedAt}</p>
                  </div>
                </div>
                <div className="flex items-center gap-2">
                  <span
                    className={`inline-flex items-center gap-1 px-2 py-1 rounded-sm text-[9px] font-semibold ${statusClass(doc.status)}`}
                  >
                    {doc.status === "Đã xác nhận" ? (
                      <CheckCircle className="w-3 h-3" />
                    ) : (
                      <Clock className="w-3 h-3" />
                    )}
                    {doc.status}
                  </span>
                  <button
                    type="button"
                    className="p-2 text-[#59655e] hover:bg-white rounded"
                    title="Tải xuống (mock)"
                    onClick={() => setToast("Mock: chưa có file từ API.")}
                  >
                    <Download className="w-4 h-4" />
                  </button>
                  {doc.status === "Chờ xác nhận" && (
                    <button
                      type="button"
                      onClick={() => handleConfirm(doc.id)}
                      className="min-h-[30px] px-2 rounded-[3px] bg-[#173e34] text-white text-[10px] font-bold hover:bg-[#245748]"
                    >
                      Đánh dấu đã xác nhận
                    </button>
                  )}
                </div>
              </div>
            ))
          )}
        </div>
      </section>

      {toast && (
        <div className="fixed right-5 bottom-5 z-50 px-4 py-3 rounded border border-[#c9d9cc] bg-white text-[11px] text-[#173e34] shadow-lg">
          {toast}
          <button type="button" className="ml-3 underline text-[#78827c]" onClick={() => setToast(null)}>
            Đóng
          </button>
        </div>
      )}
    </div>
  );
}