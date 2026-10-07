const form = document.getElementById("register-form");
const status = document.getElementById("status-message");
const submitButton = form.querySelector('button[type="submit"]');
const fields = [...form.querySelectorAll("input")];
const dateOfBirthInput = form.elements.dateOfBirth;

function validateDateOfBirth() {
  const today = new Date();
  today.setMinutes(today.getMinutes() - today.getTimezoneOffset());
  dateOfBirthInput.max = today.toISOString().slice(0, 10);
  dateOfBirthInput.setCustomValidity(
    dateOfBirthInput.validity.rangeOverflow ? "Ngày sinh không hợp lệ." : "",
  );
}

dateOfBirthInput.addEventListener("input", validateDateOfBirth);
validateDateOfBirth();

function clearFieldError(input) {
  const error = document.getElementById(`${input.id}-error`);
  if (error) error.remove();
  input.removeAttribute("aria-invalid");
  const descriptions = (input.getAttribute("aria-describedby") || "")
    .split(" ")
    .filter((id) => id !== `${input.id}-error`);
  if (descriptions.length)
    input.setAttribute("aria-describedby", descriptions.join(" "));
  else input.removeAttribute("aria-describedby");
}

function showFieldError(input, message) {
  clearFieldError(input);
  const error = document.createElement("p");
  error.className = "field-error";
  error.id = `${input.id}-error`;
  error.setAttribute("role", "alert");
  error.textContent = message;
  input.closest(".field").append(error);
  input.setAttribute("aria-invalid", "true");
  input.setAttribute("aria-describedby", error.id);
}

function getClientError(input) {
  const label = input
    .closest(".field")
    .querySelector("label")
    .textContent.split("(")[0]
    .trim();
  if (input.required && !input.value.trim())
    return `Vui lòng nhập ${label.toLowerCase()}.`;
  if (input.validity.rangeOverflow) return "Ngày sinh không hợp lệ.";
  if (input.validity.typeMismatch) return "Email không đúng định dạng.";
  if (input.validity.patternMismatch)
    return "Số điện thoại cần có 9-15 chữ số, có thể bắt đầu bằng dấu +.";
  if (input.validity.tooShort)
    return `Mật khẩu phải có ít nhất ${input.minLength} ký tự.`;
  if (input.validity.tooLong)
    return `${label} không được vượt quá ${input.maxLength} ký tự.`;
  return "";
}

function validateFields() {
  let firstInvalid = null;
  for (const input of fields) {
    clearFieldError(input);
    const message = getClientError(input);
    if (!message) continue;
    showFieldError(input, message);
    firstInvalid ??= input;
  }
  firstInvalid?.focus();
  return firstInvalid === null;
}

function showServerErrors(errors) {
  let matched = 0;
  for (const [name, messages] of Object.entries(errors)) {
    const input = fields.find(
      (field) => field.name.toLowerCase() === name.toLowerCase(),
    );
    if (!input) continue;
    const clientError = getClientError(input);
    const message = Array.isArray(messages) ? messages[0] : messages;
    showFieldError(input, clientError || String(message));
    matched++;
  }
  return matched;
}

fields.forEach((input) =>
  input.addEventListener("input", () => {
    clearFieldError(input);
    status.textContent = "";
    delete status.dataset.state;
  }),
);

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  status.textContent = "";
  delete status.dataset.state;
  if (!validateFields()) return;

  submitButton.disabled = true;
  try {
    const r = await fetch("/api/interns/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(Object.fromEntries(new FormData(form))),
    });
    const data = await r.json().catch(() => null);
    if (!r.ok) {
      if (data?.errors && showServerErrors(data.errors)) return;
      const message = data?.message || "Đăng ký không thành công.";
      throw new Error(
        message.replace(
          /Email,\s*mã sinh viên hoặc số điện thoại đã tồn tại/iu,
          "Email hoặc số điện thoại đã tồn tại",
        ),
      );
    }
    window.location.assign("dang-nhap.html");
  } catch (e) {
    status.textContent =
      e instanceof Error ? e.message : "Không kết nối được API.";
    status.dataset.state = "error";
    status.focus();
    status.scrollIntoView({ behavior: "smooth", block: "center" });
  } finally {
    submitButton.disabled = false;
  }
});

const passwordInput = document.getElementById("reg-password");
const passwordToggle = document.getElementById("password-toggle");
passwordToggle.addEventListener("click", () => {
  const showing = passwordInput.type === "text";
  passwordInput.type = showing ? "password" : "text";
  passwordToggle.textContent = showing ? "Hiện" : "Ẩn";
  passwordToggle.setAttribute("aria-pressed", String(!showing));
});
