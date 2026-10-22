using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Email:ResendApiKey"] = "re_test_secret",
    ["Email:From"] = "Career Portal <notice@example.org>"
}).Build();

var handler = new RecordingHandler();
using var client = new HttpClient(handler);
var job = new MailJob
{
    Id = 12,
    EventKey = "account-confirm:12:1",
    Recipient = "recipient@gmail.com",
    Subject = "Xác nhận email",
    Body = "Mở liên kết xác nhận",
    IsHtml = false
};

await ResendEmailSender.SendAsync(client, config, job, CancellationToken.None);
Check(handler.RequestMethod == HttpMethod.Post, "Uses POST");
Check(handler.RequestUri == "https://api.resend.com/emails", "Uses Resend API endpoint");
Check(handler.Authorization == "Bearer re_test_secret", "Uses bearer authorization");
Check(!string.IsNullOrWhiteSpace(handler.IdempotencyKey), "Provides idempotency key");
using (var payload = JsonDocument.Parse(handler.RequestBody!))
{
    var root = payload.RootElement;
    Check(root.GetProperty("from").GetString() == "Career Portal <notice@example.org>", "Uses verified sender configuration");
    Check(root.GetProperty("to")[0].GetString() == "recipient@gmail.com", "Targets Gmail recipient");
    Check(root.GetProperty("subject").GetString() == job.Subject, "Sets subject");
    Check(root.GetProperty("text").GetString() == job.Body, "Sends plain text body");
    Check(!root.TryGetProperty("html", out _), "Does not send HTML for a text job");
}

var originalKey = handler.IdempotencyKey;
await ResendEmailSender.SendAsync(client, config, job, CancellationToken.None);
Check(handler.IdempotencyKey == originalKey, "Keeps a stable idempotency key on retry");

job.IsHtml = true;
job.Body = "<p>Xin chào</p>";
await ResendEmailSender.SendAsync(client, config, job, CancellationToken.None);
using (var payload = JsonDocument.Parse(handler.RequestBody!))
{
    var root = payload.RootElement;
    Check(root.GetProperty("html").GetString() == job.Body, "Sends HTML body");
    Check(!root.TryGetProperty("text", out _), "Does not duplicate HTML as plain text");
}

handler.Status = HttpStatusCode.Forbidden;
try
{
    await ResendEmailSender.SendAsync(client, config, job, CancellationToken.None);
    throw new Exception("Expected HTTP 403 to fail");
}
catch (InvalidOperationException error)
{
    Check(error.Message.Contains("HTTP 403"), "Reports provider error status");
    Check(!error.Message.Contains("re_test_secret"), "Does not leak API key in errors");
}

Console.WriteLine("ResendMailChecks: 14 assertions passed (offline; no email sent).");

static void Check(bool result, string title)
{
    if (!result) throw new Exception(title);
}

sealed class RecordingHandler : HttpMessageHandler
{
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    public HttpMethod? RequestMethod { get; private set; }
    public string? RequestUri { get; private set; }
    public string? Authorization { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestMethod = request.Method;
        RequestUri = request.RequestUri?.ToString();
        Authorization = request.Headers.Authorization?.ToString();
        IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var values) ? values.SingleOrDefault() : null;
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(Status);
    }
}
