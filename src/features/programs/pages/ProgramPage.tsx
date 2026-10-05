import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { Plus, Pencil, Trash2 } from "lucide-react";

const programSchema = z
  .object({
    title: z.string().min(2, "Tên chương trình phải có ít nhất 2 ký tự"),
    department: z.string().min(1, "Vui lòng chọn hoặc nhập phòng ban"),
    capacity: z
      .number({ error: "Chỉ tiêu phải là số" })
      .int("Chỉ tiêu phải là số nguyên")
      .min(1, "Chỉ tiêu tối thiểu là 1"),
    start: z.string().min(1, "Vui lòng chọn ngày bắt đầu"),
    end: z.string().min(1, "Vui lòng chọn ngày kết thúc"),
    detail: z.string().min(1, "Vui lòng nhập mô tả"),
  })
  .refine((d) => new Date(d.end) > new Date(d.start), {
    message: "Ngày kết thúc phải sau ngày bắt đầu",
    path: ["end"],
  });

type ProgramFormData = z.infer<typeof programSchema>;

interface Program extends ProgramFormData {
  id: number;
  status: string;
}

const DEPARTMENTS = [
  "Công nghệ thông tin",
  "Marketing",
  "Nhân sự",
  "Kế toán",
  "Kinh doanh",
];

/** Trạng thái tính theo ngày bắt đầu / kết thúc */
function statusByDates(start: string, end: string) {
  const now = new Date();
  now.setHours(0, 0, 0, 0);
  const s = new Date(start);
  const e = new Date(end);
  if (now < s) return "Sắp diễn ra";
  if (now > e) return "Đã kết thúc";
  return "Đang diễn ra";
}

const initialMock: Program[] = [
  {
    id: 1,
    title: "Thực tập Lập trình Web",
    department: "Công nghệ thông tin",
    capacity: 10,
    start: "2026-09-01",
    end: "2026-12-15",
    detail: "Chương trình thực tập Frontend/Backend cho sinh viên năm cuối.",
    status: statusByDates("2026-09-01", "2026-12-15"),
  },
  {
    id: 2,
    title: "Thực tập Marketing số",
    department: "Marketing",
    capacity: 6,
    start: "2026-10-15",
    end: "2027-01-30",
    detail: "Làm việc cùng nhóm nội dung và quảng cáo.",
    status: statusByDates("2026-10-15", "2027-01-30"),
  },
];

const inputClass =
  "w-full min-h-9 px-2.5 border border-[#dce1db] rounded-[3px] bg-white text-[#25332d] text-[11px] focus:outline-none focus:border-[#7d998b] focus:ring-[3px] focus:ring-[#173e34]/10 transition";
const labelClass = "block mb-1.5 text-[10px] font-bold text-[#506057]";

export default function ProgramPage() {
  const [programs, setPrograms] = useState<Program[]>(initialMock);
  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProgramFormData>({
    resolver: zodResolver(programSchema),
    defaultValues: {
      title: "",
      department: "",
      capacity: 5,
      start: "",
      end: "",
      detail: "",
    },
  });

  const openCreate = () => {
    setEditingId(null);
    reset({
      title: "",
      department: "",
      capacity: 5,
      start: "",
      end: "",
      detail: "",
    });
    setOpen(true);
  };

  const openEdit = (p: Program) => {
    setEditingId(p.id);
    reset({
      title: p.title,
      department: p.department,
      capacity: p.capacity,
      start: p.start,
      end: p.end,
      detail: p.detail,
    });
    setOpen(true);
  };

  const onSubmit = (data: ProgramFormData) => {
    const status = statusByDates(data.start, data.end);
    if (editingId != null) {
      setPrograms((prev) =>
        prev.map((p) => (p.id === editingId ? { ...p, ...data, status } : p))
      );
      setToast("Đã cập nhật chương trình.");
    } else {
      setPrograms((prev) => [
        { ...data, id: Date.now(), status },
        ...prev,
      ]);
      setToast("Đã thêm chương trình mới.");
    }
    setOpen(false);
  };

  const remove = (id: number) => {
    if (!confirm("Xóa chương trình này?")) return;
    setPrograms((prev) => prev.filter((p) => p.id !== id));
    setToast("Đã xóa chương trình.");
  };

  return (
    <div>
      <div className="flex flex-wrap items-end justify-between gap-4 mb-6">
        <div>
          <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
            Quản lý chương trình
          </p>
          <h1 className="m-0 font-serif text-[32px] font-medium leading-tight text-[#25332d]">
            Chương trình thực tập
          </h1>
          <p className="mt-2 mb-0 text-xs text-[#78827c] leading-relaxed">
            Tạo chương trình gồm tên, phòng ban, mô tả và chỉ tiêu tuyển.
          </p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex items-center gap-2 min-h-9 px-3 rounded-[3px] bg-[#173e34] text-white text-[11px] font-bold hover:bg-[#245748] transition"
        >
          <Plus className="w-4 h-4" />
          Thêm chương trình
        </button>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-3 gap-3 mb-6">
        {[
          { label: "Tổng chương trình", value: programs.length },
          {
            label: "Đang diễn ra",
            value: programs.filter((p) => p.status === "Đang diễn ra").length,
          },
          {
            label: "Tổng chỉ tiêu",
            value: programs.reduce((s, p) => s + p.capacity, 0),
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

      <div className="border border-[#e1e5df] rounded bg-white overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-[#f8f9f6]">
                {[
                  "Tên chương trình",
                  "Phòng ban",
                  "Chỉ tiêu",
                  "Thời gian",
                  "Trạng thái",
                  "Thao tác",
                ].map((h) => (
                  <th
                    key={h}
                    className="h-9 px-3 text-[9px] font-bold tracking-wide uppercase text-[#818b84] border-b border-[#e1e5df] whitespace-nowrap"
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {programs.length === 0 ? (
                <tr>
                  <td
                    colSpan={6}
                    className="px-4 py-10 text-center text-[11px] text-[#78827c]"
                  >
                    Chưa có chương trình. Bấm &quot;Thêm chương trình&quot; để tạo mới.
                  </td>
                </tr>
              ) : (
                programs.map((p) => (
                  <tr
                    key={p.id}
                    className="border-b border-[#edf0eb] hover:bg-[#fcfcfa]"
                  >
                    <td className="px-3 py-3">
                      <p className="m-0 text-[11px] font-bold text-[#26362e]">
                        {p.title}
                      </p>
                      <p className="m-0 mt-1 text-[9px] text-[#929b94] line-clamp-1 max-w-[220px]">
                        {p.detail}
                      </p>
                    </td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e]">
                      {p.department}
                    </td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e]">
                      {p.capacity}
                    </td>
                    <td className="px-3 py-3 text-[10px] text-[#59655e] whitespace-nowrap">
                      {p.start} → {p.end}
                    </td>
                    <td className="px-3 py-3">
                      <span
                        className={`inline-flex px-2 py-1 rounded-sm text-[9px] font-semibold ${
                          p.status === "Đang diễn ra"
                            ? "bg-[#edf5ef] text-[#347153]"
                            : p.status === "Đã kết thúc"
                              ? "bg-[#eef0ed] text-[#68736c]"
                              : "bg-[#faf1df] text-[#a46a17]"
                        }`}
                      >
                        {p.status}
                      </span>
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-1">
                        <button
                          type="button"
                          onClick={() => openEdit(p)}
                          className="p-1.5 text-[#52635a] hover:bg-[#f7f8f5] rounded"
                          title="Sửa"
                        >
                          <Pencil className="w-3.5 h-3.5" />
                        </button>
                        <button
                          type="button"
                          onClick={() => remove(p.id)}
                          className="p-1.5 text-[#ad4939] hover:bg-[#fff9f7] rounded"
                          title="Xóa"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
        <div className="px-3 py-2.5 border-t border-[#e1e5df] text-[9px] text-[#89938d]">
          {programs.length} chương trình
        </div>
      </div>

      {open && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
          <div className="w-full max-w-lg bg-white rounded border border-[#dce1db] shadow-xl max-h-[90vh] overflow-auto">
            <div className="px-5 pt-5 pb-3 border-b border-[#e1e5df]">
              <p className="m-0 mb-1 text-[9px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
                Chương trình thực tập
              </p>
              <h2 className="m-0 font-serif text-[22px] font-medium text-[#25332d]">
                {editingId != null
                  ? "Cập nhật chương trình"
                  : "Thêm chương trình"}
              </h2>
              <p className="mt-1 mb-0 text-[10px] text-[#78827c]">
                Các trường có dấu * là bắt buộc. Trạng thái tự tính theo ngày.
              </p>
            </div>

            <form onSubmit={handleSubmit(onSubmit)} className="p-5">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div className="sm:col-span-2">
                  <label className={labelClass} htmlFor="title">
                    Tên chương trình *
                  </label>
                  <input id="title" className={inputClass} {...register("title")} />
                  {errors.title && (
                    <p className="mt-1 text-[10px] text-[#ad4939]">
                      {errors.title.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className={labelClass} htmlFor="department">
                    Phòng ban *
                  </label>
                  <select
                    id="department"
                    className={inputClass}
                    {...register("department")}
                  >
                    <option value="">Chọn phòng ban</option>
                    {DEPARTMENTS.map((d) => (
                      <option key={d} value={d}>
                        {d}
                      </option>
                    ))}
                  </select>
                  {errors.department && (
                    <p className="mt-1 text-[10px] text-[#ad4939]">
                      {errors.department.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className={labelClass} htmlFor="capacity">
                    Chỉ tiêu *
                  </label>
                  <input
                    id="capacity"
                    type="number"
                    min={1}
                    className={inputClass}
                    {...register("capacity", { valueAsNumber: true })}
                  />
                  {errors.capacity && (
                    <p className="mt-1 text-[10px] text-[#ad4939]">
                      {errors.capacity.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className={labelClass} htmlFor="start">
                    Ngày bắt đầu *
                  </label>
                  <input
                    id="start"
                    type="date"
                    className={inputClass}
                    {...register("start")}
                  />
                  {errors.start && (
                    <p className="mt-1 text-[10px] text-[#ad4939]">
                      {errors.start.message}
                    </p>
                  )}
                </div>

                <div>
                  <label className={labelClass} htmlFor="end">
                    Ngày kết thúc *
                  </label>
                  <input
                    id="end"
                    type="date"
                    className={inputClass}
                    {...register("end")}
                  />
                  {errors.end && (
                    <p className="mt-1 text-[10px] text-[#ad4939]">
                      {errors.end.message}
                    </p>
                  )}
                </div>

                <div className="sm:col-span-2">
                  <label className={labelClass} htmlFor="detail">
                    Mô tả *
                  </label>
                  <textarea
                    id="detail"
                    rows={3}
                    className={`${inputClass} py-2 resize-none min-h-[72px]`}
                    {...register("detail")}
                  />
                  {errors.detail && (
                    <p className="mt-1 text-[10px] text-[#ad4939]">
                      {errors.detail.message}
                    </p>
                  )}
                </div>
              </div>

              <p className="mt-3 mb-0 text-[9px] text-[#8a948d] leading-relaxed">
                Dữ liệu lưu trên giao diện (mock). Backend gắn API sau.
              </p>

              <div className="flex justify-end gap-2 mt-5">
                <button
                  type="button"
                  onClick={() => setOpen(false)}
                  className="min-h-9 px-3 rounded-[3px] border border-[#e1e5df] bg-white text-[11px] font-bold text-[#25332d] hover:bg-[#f7f8f5]"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="min-h-9 px-3 rounded-[3px] bg-[#173e34] text-white text-[11px] font-bold hover:bg-[#245748]"
                >
                  {editingId != null ? "Cập nhật" : "Lưu chương trình"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

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
    </div>
  );
}