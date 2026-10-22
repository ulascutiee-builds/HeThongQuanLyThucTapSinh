export interface DecisionEmailData {
  fullName: string;
  rejectionReason?: string;
}

export function renderDecisionEmail(template: 'intern_approved' | 'intern_rejected', data: DecisionEmailData) {
  const name = escapeHtml(data.fullName);
  if (template === 'intern_approved') {
    return {
      subject: 'Kết quả hồ sơ thực tập: Đã được chấp thuận',
      text: `Chào ${data.fullName},\n\nHồ sơ thực tập của bạn đã được chấp thuận. Bộ phận phụ trách sẽ liên hệ với bạn về các bước tiếp theo.`,
      html: `<p>Chào ${name},</p><p>Hồ sơ thực tập của bạn đã được <strong>chấp thuận</strong>.</p><p>Bộ phận phụ trách sẽ liên hệ với bạn về các bước tiếp theo.</p>`,
    };
  }

  const reason = data.rejectionReason?.trim() || 'Hồ sơ chưa đáp ứng yêu cầu hiện tại.';
  return {
    subject: 'Kết quả hồ sơ thực tập: Cần bổ sung hoặc điều chỉnh',
    text: `Chào ${data.fullName},\n\nHồ sơ thực tập của bạn chưa được chấp thuận.\nLý do: ${reason}\n\nBạn có thể liên hệ bộ phận phụ trách để được hướng dẫn.`,
    html: `<p>Chào ${name},</p><p>Hồ sơ thực tập của bạn hiện <strong>chưa được chấp thuận</strong>.</p><p><strong>Lý do:</strong> ${escapeHtml(reason)}</p><p>Bạn có thể liên hệ bộ phận phụ trách để được hướng dẫn.</p>`,
  };
}

function escapeHtml(value: string): string {
  return value.replace(/[&<>"']/g, (character) => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;',
  })[character] ?? character);
}
