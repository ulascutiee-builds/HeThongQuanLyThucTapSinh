import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";

// TODO: Backend – POST /api/interns
const internSchema = z
  .object({
    fullName: z.string().min(2, "Họ tên phải có ít nhất 2 ký tự"),
    studentId: z.string().min(1, "Vui lòng nhập MSSV"),
    email: z.string().email("Email không hợp lệ"),
    phone: z
      .string()
      .regex(/^(0[3|5|7|8|9])+([0-9]{8})$/, "Số điện thoại không hợp lệ (10 số)"),
    dateOfBirth: z.string().min(1, "Vui lòng chọn ngày sinh"),
    gender: z.enum(["male", "female", "other"], { message: "Vui lòng chọn giới tính" }),
    university: z.string().min(1, "Vui lòng nhập trường"),
    major: z.string().min(1, "Vui lòng nhập ngành"),
    startDate: z.string().min(1, "Vui lòng chọn ngày bắt đầu"),
    endDate: z.string().min(1, "Vui lòng chọn ngày kết thúc"),
    department: z.string().optional(),
    note: z.string().optional(),
  })
  .refine((data) => new Date(data.endDate) > new Date(data.startDate), {
    message: "Ngày kết thúc phải sau ngày bắt đầu",
    path: ["endDate"],
  });

export type InternFormData = z.infer<typeof internSchema>;

interface InternFormProps {
  onSubmit?: (data: InternFormData) => void | Promise<void>;
  isLoading?: boolean;
}

export default function InternForm({ onSubmit, isLoading = false }: InternFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<InternFormData>({
    resolver: zodResolver(internSchema),
    defaultValues: {
      fullName: "",
      studentId: "",
      email: "",
      phone: "",
      dateOfBirth: "",
      gender: undefined,
      university: "",
      major: "",
      startDate: "",
      endDate: "",
      department: "",
      note: "",
    },
  });

  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const onFormSubmit = async (data: InternFormData) => {
    try {
      setMessage(null);
      if (onSubmit) await onSubmit(data);
      else {
        console.log("POST /api/interns", data);
        await new Promise((r) => setTimeout(r, 600));
      }
      setMessage({ type: "success", text: "Tạo hồ sơ thành công!" });
    } catch {
      setMessage({ type: "error", text: "Có lỗi xảy ra, vui lòng thử lại." });
    }
  };

  const inputClass =
    "w-full px-3 py-2.5 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent";

  return (
    <div className="max-w-3xl mx-auto">
      <div className="mb-6">
        <h1 className="text-xl font-bold text-slate-900">Thêm hồ sơ thực tập sinh</h1>
        <p className="text-sm text-slate-500 mt-1">POST /api/interns – kiểm tra dữ liệu bắt buộc và định dạng</p>
      </div>

      {message && (
        <div
          className={`mb-4 p-3 rounded-lg text-sm ${
            message.type === "success"
              ? "bg-emerald-50 text-emerald-700 border border-emerald-200"
              : "bg-red-50 text-red-700 border border-red-200"
          }`}
        >
          {message.text}
        </div>
      )}

      <form onSubmit={handleSubmit(onFormSubmit)} className="bg-white rounded-xl border border-slate-200 p-6 shadow-sm space-y-5">
        <h2 className="font-semibold text-slate-800">Thông tin cá nhân</h2>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Họ và tên <span className="text-red-500">*</span>
            </label>
            <input {...register("fullName")} className={inputClass} placeholder="Nguyễn Văn A" />
            {errors.fullName && <p className="mt-1 text-xs text-red-500">{errors.fullName.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              MSSV <span className="text-red-500">*</span>
            </label>
            <input {...register("studentId")} className={inputClass} placeholder="B20DCCN001" />
            {errors.studentId && <p className="mt-1 text-xs text-red-500">{errors.studentId.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Email <span className="text-red-500">*</span>
            </label>
            <input type="email" {...register("email")} className={inputClass} placeholder="nguyenvana@gmail.com" />
            {errors.email && <p className="mt-1 text-xs text-red-500">{errors.email.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Số điện thoại <span className="text-red-500">*</span>
            </label>
            <input {...register("phone")} className={inputClass} placeholder="0988123456" />
            {errors.phone && <p className="mt-1 text-xs text-red-500">{errors.phone.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Ngày sinh <span className="text-red-500">*</span>
            </label>
            <input type="date" {...register("dateOfBirth")} className={inputClass} />
            {errors.dateOfBirth && <p className="mt-1 text-xs text-red-500">{errors.dateOfBirth.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Giới tính <span className="text-red-500">*</span>
            </label>
            <select {...register("gender")} className={inputClass}>
              <option value="">-- Chọn --</option>
              <option value="male">Nam</option>
              <option value="female">Nữ</option>
              <option value="other">Khác</option>
            </select>
            {errors.gender && <p className="mt-1 text-xs text-red-500">{errors.gender.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Trường <span className="text-red-500">*</span>
            </label>
            <input {...register("university")} className={inputClass} placeholder="ICTU" />
            {errors.university && <p className="mt-1 text-xs text-red-500">{errors.university.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Ngành <span className="text-red-500">*</span>
            </label>
            <input {...register("major")} className={inputClass} placeholder="Công nghệ thông tin" />
            {errors.major && <p className="mt-1 text-xs text-red-500">{errors.major.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Ngày bắt đầu <span className="text-red-500">*</span>
            </label>
            <input type="date" {...register("startDate")} className={inputClass} />
            {errors.startDate && <p className="mt-1 text-xs text-red-500">{errors.startDate.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              Ngày kết thúc <span className="text-red-500">*</span>
            </label>
            <input type="date" {...register("endDate")} className={inputClass} />
            {errors.endDate && <p className="mt-1 text-xs text-red-500">{errors.endDate.message}</p>}
          </div>
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-slate-700 mb-1">Phòng ban mong muốn</label>
            <input {...register("department")} className={inputClass} placeholder="Phòng Kỹ thuật / HR..." />
          </div>
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-slate-700 mb-1">Ghi chú</label>
            <textarea {...register("note")} rows={2} className={`${inputClass} resize-none`} placeholder="Thông tin bổ sung..." />
          </div>
        </div>

        <div className="p-3 rounded-lg bg-slate-50 border border-slate-100 text-sm text-slate-600">
          Tài liệu: <span className="text-slate-800">CV.pdf</span>, <span className="text-slate-800">Don_xin_thuc_tap.pdf</span>
          <span className="text-slate-400 text-xs ml-2">(UI – Backend gắn upload sau)</span>
        </div>

        <div className="flex justify-end gap-3 pt-1">
          <button
            type="button"
            onClick={() => {
              reset();
              setMessage(null);
            }}
            className="px-4 py-2 text-sm font-medium text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50"
          >
            Hủy
          </button>
          <button
            type="submit"
            disabled={isLoading}
            className="px-5 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-60"
          >
            {isLoading ? "Đang lưu..." : "Lưu hồ sơ"}
          </button>
        </div>
      </form>
    </div>
  );
}