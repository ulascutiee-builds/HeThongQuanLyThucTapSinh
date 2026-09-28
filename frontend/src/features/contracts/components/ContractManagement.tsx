import { useState } from "react";
import { Upload, FileText, ArrowLeft, Eye } from "lucide-react";

// ===== Danh sách thực tập sinh (mock) =====
const mockInterns = [
  { id: 1, code: "TS001", fullName: "Nguyễn Văn A", status: "pending" },
  { id: 2, code: "TS002", fullName: "Trần Thị B", status: "pending" },
  { id: 3, code: "TS003", fullName: "Lê Văn C", status: "approved" },
];

// ===== Hợp đồng theo từng internId (mock) =====
const mockContractsByIntern: Record<
  number,
  { id: number; fileName: string; uploadedAt: string; uploadedBy: string; status: "pending" | "confirmed"; size: string }[]
> = {
  1: [
    {
      id: 1,
      fileName: "HopDong_NguyenVanA_v1.pdf",
      uploadedAt: "22/09/2026 14:30",
      uploadedBy: "HR Nguyễn Thị H",
      status: "pending",
      size: "245 KB",
    },
    {
      id: 2,
      fileName: "HopDong_NguyenVanA_v2.pdf",
      uploadedAt: "24/09/2026 09:15",
      uploadedBy: "HR Nguyễn Thị H",
      status: "confirmed",
      size: "251 KB",
    },
  ],
  2: [],
  3: [
    {
      id: 1,
      fileName: "HopDong_LeVanC.pdf",
      uploadedAt: "18/09/2026 10:00",
      uploadedBy: "HR Nguyễn Thị H",
      status: "confirmed",
      size: "230 KB",
    },
  ],
};

const internStatusLabel: Record<string, string> = {
  pending: "Chờ duyệt",
  approved: "Đã duyệt",
  rejected: "Từ chối",
};

export default function ContractManagement() {
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [contractsMap, setContractsMap] = useState(mockContractsByIntern);
  const [isUploading, setIsUploading] = useState(false);

  const selected = mockInterns.find((i) => i.id === selectedId);
  const contracts = selectedId ? contractsMap[selectedId] || [] : [];

  // TODO: Backend – POST /api/interns/:id/contracts (upload)
  const handleUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !selectedId) return;
    setIsUploading(true);
    setTimeout(() => {
      setContractsMap((prev) => ({
        ...prev,
        [selectedId]: [
          {
            id: Date.now(),
            fileName: file.name,
            uploadedAt: new Date().toLocaleString("vi-VN"),
            uploadedBy: "Bạn (HR)",
            status: "pending",
            size: `${Math.round(file.size / 1024)} KB`,
          },
          ...(prev[selectedId] || []),
        ],
      }));
      setIsUploading(false);
    }, 800);
  };

  // TODO: Backend – POST /api/contracts/:id/confirm
  const handleConfirm = (contractId: number) => {
    if (!selectedId) return;
    setContractsMap((prev) => ({
      ...prev,
      [selectedId]: (prev[selectedId] || []).map((c) =>
        c.id === contractId ? { ...c, status: "confirmed" as const } : c
      ),
    }));
  };

  // ===== MÀN 1: Danh sách TTS =====
  if (!selectedId) {
    return (
      <div className="max-w-5xl mx-auto">
        <div className="mb-6">
          <h1 className="text-xl font-bold text-slate-900">Quản lý hợp đồng</h1>
          <p className="text-sm text-slate-500 mt-1">
            Chọn thực tập sinh để xem và quản lý hợp đồng
          </p>
        </div>

        <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 border-b border-slate-200">
              <tr>
                <th className="text-left px-5 py-3 font-medium text-slate-600">Mã / Họ tên</th>
                <th className="text-left px-5 py-3 font-medium text-slate-600">Trạng thái hồ sơ</th>
                <th className="text-right px-5 py-3 font-medium text-slate-600">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {mockInterns.map((item) => (
                <tr key={item.id} className="hover:bg-slate-50">
                  <td className="px-5 py-3">
                    <div className="font-medium text-slate-900">
                      {item.code} – {item.fullName}
                    </div>
                  </td>
                  <td className="px-5 py-3 text-slate-600">
                    {internStatusLabel[item.status] || item.status}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button
                      onClick={() => setSelectedId(item.id)}
                      className="inline-flex items-center gap-1 px-3 py-1.5 text-sm text-blue-600 hover:bg-blue-50 rounded-lg"
                    >
                      <Eye className="w-4 h-4" />
                      Xem hợp đồng
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  // ===== MÀN 2: Hợp đồng của 1 TTS =====
  return (
    <div className="max-w-5xl mx-auto">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-6">
        <div>
          <h1 className="text-xl font-bold text-slate-900">Quản lý hợp đồng</h1>
          <p className="text-sm text-slate-500 mt-1">
            Upload và xác nhận hợp đồng theo từng thực tập sinh
          </p>
        </div>
        <button
          onClick={() => setSelectedId(null)}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 shrink-0"
        >
          <ArrowLeft className="w-4 h-4" />
          Danh sách
        </button>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 p-4 mb-4 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <div className="font-semibold text-slate-900">
            {selected?.code} – {selected?.fullName}
          </div>
          <div className="text-sm text-slate-500">
            Trạng thái hồ sơ: {internStatusLabel[selected?.status || ""] || "—"}
          </div>
        </div>
        <label className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 cursor-pointer">
          <Upload className="w-4 h-4" />
          {isUploading ? "Đang upload..." : "Upload hợp đồng"}
          <input
            type="file"
            accept=".pdf,.doc,.docx"
            className="hidden"
            onChange={handleUpload}
            disabled={isUploading}
          />
        </label>
      </div>

      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 border-b border-slate-200">
            <tr>
              <th className="text-left px-5 py-3 font-medium text-slate-600">Tên file</th>
              <th className="text-left px-5 py-3 font-medium text-slate-600">Người upload</th>
              <th className="text-left px-5 py-3 font-medium text-slate-600">Ngày tải</th>
              <th className="text-left px-5 py-3 font-medium text-slate-600">Trạng thái</th>
              <th className="text-right px-5 py-3 font-medium text-slate-600">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {contracts.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-5 py-10 text-center text-slate-500">
                  Chưa có hợp đồng nào được upload
                </td>
              </tr>
            ) : (
              contracts.map((c) => (
                <tr key={c.id} className="hover:bg-slate-50">
                  <td className="px-5 py-3">
                    <div className="flex items-center gap-2 text-slate-900">
                      <FileText className="w-4 h-4 text-slate-400" />
                      <div>
                        <div className="font-medium">{c.fileName}</div>
                        <div className="text-xs text-slate-400">{c.size}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-5 py-3 text-slate-600">{c.uploadedBy}</td>
                  <td className="px-5 py-3 text-slate-600">{c.uploadedAt}</td>
                  <td className="px-5 py-3">
                    {c.status === "pending" ? (
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-medium bg-amber-50 text-amber-700 border border-amber-200">
                        Chờ xác nhận
                      </span>
                    ) : (
                      <span className="inline-flex px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                        Đã xác nhận
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button type="button" className="text-blue-600 hover:underline text-sm mr-2">
                      Xem
                    </button>
                    {c.status === "pending" && (
                      <button
                        type="button"
                        onClick={() => handleConfirm(c.id)}
                        className="px-3 py-1 text-xs font-medium bg-emerald-600 text-white rounded-lg hover:bg-emerald-700"
                      >
                        Xác nhận
                      </button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}