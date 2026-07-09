using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using TaskManager.Domain.Interfaces;

namespace TaskManager.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        // ── READ API KEY FROM USER SECRETS ────────────────
        // This is where "SendGrid:ApiKey" you stored earlier gets read —
        // same IConfiguration mechanism as JwtSettings, ConnectionStrings, etc
        var apiKey = _configuration["SendGrid:ApiKey"];

        if (string.IsNullOrEmpty(apiKey))
        {
            // ── FAIL LOUDLY, NOT SILENTLY ───────────────────
            // If the key is missing, log a clear error instead of
            // letting SendGridClient throw a cryptic exception later
            _logger.LogError("SendGrid API key is not configured. Email not sent to {ToEmail}", toEmail);
            return;
        }
        // ─────────────────────────────────────────────────

        var client = new SendGridClient(apiKey);

        // ── FROM ADDRESS ───────────────────────────────────
        // IMPORTANT: this email must be a "verified sender" in your
        // SendGrid account (Settings → Sender Authentication) or
        // SendGrid will reject the send. Use the email you signed
        // up with, or verify a new one — we'll do this next step
        var from = new EmailAddress("praveennath052004@gmail.com", "Task Manager");
        // ─────────────────────────────────────────────────

        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlBody);

        // ── SEND ───────────────────────────────────────────
        var response = await client.SendEmailAsync(msg, cancellationToken);
        // ─────────────────────────────────────────────────

        // ── LOG THE OUTCOME ──────────────────────────────
        // SendGrid returns 202 Accepted on success — log clearly
        // either way so we can debug delivery issues later
        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
        {
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        else
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to send email to {ToEmail}. Status: {StatusCode}, Body: {Body}",
                toEmail, response.StatusCode, body);
        }
        // ─────────────────────────────────────────────────
    }
}