using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuickBooksDemo.Models.Configuration;
using QuickBooksDemo.Service.Interfaces;

namespace QuickBooksDemo.Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfig _emailConfig;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailConfig> emailConfig, ILogger<EmailService> logger)
        {
            _emailConfig = emailConfig.Value;
            _logger = logger;
        }

        public async Task SendQuickBooksTokenErrorAsync(string errorMessage, Exception exception)
        {
            try
            {
                // For now, we'll log the error and send a simple HTTP POST to a webhook or logging service
                // In a real application, you'd use SendGrid, SMTP, or another email service

                var emailSubject = "QuickBooks Token Refresh Failed - Action Required";
                var emailBody = $@"
QuickBooks Integration Error

Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"}

Error: {errorMessage}

Exception Details:
{exception.Message}

Stack Trace:
{exception.StackTrace}

Action Required:
Please visit the OAuth endpoint to refresh QuickBooks tokens:
- Development: http://localhost:5042/api/quickbooks/connect
- Production: https://quickbooksdemo-api.onrender.com/api/quickbooks/connect

This error typically occurs when the QuickBooks refresh token has expired (after 100-101 days).
";

                // Log the error for now (in production, you'd actually send an email)
                _logger.LogError("EMAIL ALERT for {AdminEmail}: {Subject}\n{Body}",
                    _emailConfig.AdminEmail, emailSubject, emailBody);

                // In a real implementation, add actual email sending here:
                // await SendEmailAsync(_emailConfig.AdminEmail, emailSubject, emailBody);

                await Task.CompletedTask; // Placeholder for actual async email sending
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send QuickBooks token error email notification");
            }
        }
    }
}