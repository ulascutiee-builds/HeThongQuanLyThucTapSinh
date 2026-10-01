import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useState } from "react";

const internSchema = z
  .object({
    name: z.string().min(2, "Họ tên phải có ít nhất 2 ký tự"),
    studentId: z.string().min(1, "Vui lòng nhập mã sinh viên"),
    email: z.string().email("Email không hợp lệ"),
    school: z.string().min(1, "Vui lòng nhập trường"),
    major: z.string().min(1, "Vui lòng nhập ngành"),
    startDate: z.string().min(1, "Vui lòng chọn ngày bắt đầu"),
    endDate: z.string().min(1, "Vui lòng chọn ngày kết thúc"),
    status: z.enum(["Đang thực tập", "Chờ bắt đầu", "Đã hoàn thành"]),
  })
  .refine((data) => new Date(data.endDate) > new Date(data.startDate), {
    message: "Ngày kết thúc phải sau ngày bắt đầu",
    path: ["endDate"],
  });

export type InternFormData = z.infer<typeof internSchema>;

interface InternFormProps {
  onSubmit?: (data: InternFormData) => void | Promise<void>;
  isLoading?: boolean;
  defaultValues?: Partial<InternFormData>;
}

const inputClass =
  "w-full min-h-9 px-2.5 border border-[#dce1db] rounded-[3px] bg-white text-[#25332d] text-[11px] focus:outline-none focus:border-[#7d998b] focus:ring-[3px] focus:ring-[#173e34]/10 transition";

const labelClass = "block mb-1.5 text-[10px] font-bold text-[#506057]";

export default function InternForm({
  onSubmit,
  isLoading = false,
  defaultValues,
}: InternFormProps) {
  const [message, setMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<InternFormData>({
    resolver: zodResolver(internSchema),
    defaultValues: {
      status: "Đang thực tập",
      ...defaultValues,
    },
  });

  const submitHandler = async (data: InternFormData) => {
    setMessage(null);
    try {
      if (onSubmit) {
        await onSubmit(data);
      } else {
        console.log("Mock lưu hồ sơ:", data);
      }
      setMessage("Đã lưu hồ sơ (mock). Backend sẽ gắn API sau.");
      reset({ status: "Đang thực tập" });
    } catch {
      setMessage("Không lưu được hồ sơ. Thử lại.");
    }
  };

  return (
    <div className="w-full flex justify-center">
      <div className="w-full max-w-[560px]">
        {/* Tiêu đề căn giữa khối form */}
        <div className="mb-6">
          <p className="m-0 mb-2 text-[10px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
            Quản lý chương trình
          </p>
          <h1 className="m-0 text-[32px] font-medium leading-tight text-[#25332d]">
            Thêm hồ sơ thực tập
          </h1>
          <p className="mt-2 mb-0 text-xs text-[#78827c] leading-relaxed">
            Nhập thông tin cơ bản để tạo hồ sơ theo dõi trên hệ thống.
          </p>
        </div>

        <form
          onSubmit={handleSubmit(submitHandler)}
          className="bg-white border border-[#e1e5df] rounded overflow-hidden shadow-sm"
        >
          <div className="px-5 pt-5 pb-3 border-b border-[#e1e5df]">
            <p className="m-0 mb-1 text-[9px] font-bold tracking-[0.08em] uppercase text-[#c76e49]">
              Thông tin thực tập sinh
            </p>
            <h2 className="m-0 text-[23px] font-medium text-[#25332d]">Thêm hồ sơ</h2>
            <p className="mt-1 mb-0 text-[10px] text-[#78827c]">
              Các trường có dấu * là bắt buộc.
            </p>
          </div>

          <div className="p-5">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className={labelClass} htmlFor="name">
                  Họ và tên *
                </label>
                <input id="name" className={inputClass} autoComplete="name" {...register("name")} />
                {errors.name && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.name.message}</p>
                )}
              </div>

              <div>
                <label className={labelClass} htmlFor="studentId">
                  Mã sinh viên *
                </label>
                <input id="studentId" className={inputClass} {...register("studentId")} />
                {errors.studentId && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.studentId.message}</p>
                )}
              </div>

              <div className="sm:col-span-2">
                <label className={labelClass} htmlFor="email">
                  Email *
                </label>
                <input
                  id="email"
                  type="email"
                  className={inputClass}
                  autoComplete="email"
                  {...register("email")}
                />
                {errors.email && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.email.message}</p>
                )}
              </div>

              <div>
                <label className={labelClass} htmlFor="school">
                  Trường *
                </label>
                <input id="school" className={inputClass} {...register("school")} />
                {errors.school && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.school.message}</p>
                )}
              </div>

              <div>
                <label className={labelClass} htmlFor="major">
                  Ngành *
                </label>
                <input id="major" className={inputClass} {...register("major")} />
                {errors.major && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.major.message}</p>
                )}
              </div>

              <div>
                <label className={labelClass} htmlFor="startDate">
                  Ngày bắt đầu *
                </label>
                <input
                  id="startDate"
                  type="date"
                  className={inputClass}
                  {...register("startDate")}
                />
                {errors.startDate && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.startDate.message}</p>
                )}
              </div>

              <div>
                <label className={labelClass} htmlFor="endDate">
                  Ngày kết thúc *
                </label>
                <input
                  id="endDate"
                  type="date"
                  className={inputClass}
                  {...register("endDate")}
                />
                {errors.endDate && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.endDate.message}</p>
                )}
              </div>

              <div className="sm:col-span-2">
                <label className={labelClass} htmlFor="status">
                  Trạng thái
                </label>
                <select id="status" className={inputClass} {...register("status")}>
                  <option value="Đang thực tập">Đang thực tập</option>
                  <option value="Chờ bắt đầu">Chờ bắt đầu</option>
                  <option value="Đã hoàn thành">Đã hoàn thành</option>
                </select>
                {errors.status && (
                  <p className="mt-1 text-[10px] text-[#ad4939]">{errors.status.message}</p>
                )}
              </div>
            </div>

            <p className="mt-3 mb-0 text-[9px] text-[#8a948d] leading-relaxed">
              Hồ sơ sẽ được lưu vào cơ sở dữ liệu (hiện đang mock — Backend gắn API sau).
            </p>

            {message && (
              <p className="mt-3 mb-0 text-[11px] text-[#173e34] bg-[#edf5ef] border border-[#c9d9cc] rounded px-3 py-2">
                {message}
              </p>
            )}

            <div className="flex justify-end gap-2 mt-5">
              <button
                type="button"
                onClick={() => {
                  reset({ status: "Đang thực tập" });
                  setMessage(null);
                }}
                className="inline-flex items-center justify-center min-h-9 px-3 rounded-[3px] border border-[#e1e5df] bg-white text-[#25332d] text-[11px] font-bold hover:bg-[#f7f8f5] transition"
              >
                Hủy
              </button>
              <button
                type="submit"
                disabled={isLoading}
                className="inline-flex items-center justify-center min-h-9 px-3 rounded-[3px] bg-[#173e34] text-white text-[11px] font-bold hover:bg-[#245748] disabled:opacity-50 transition"
              >
                {isLoading ? "Đang lưu..." : "Lưu hồ sơ"}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}