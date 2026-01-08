using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;

namespace Email.API.Services
{
    public class EmailService 
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOrderAsync(string toEmail, string orderNumber, decimal totalAmount)
        {
            var subject = $"Order Confirmation - #{orderNumber}";
            var body = $@"
                <h1>Thank you for your order!</h1>
                <p>Your order <strong>#{orderNumber}</strong> has been successfully placed.</p>
                <p><strong>Total Amount:</strong> ${totalAmount}</p>
                <p>We'll notify you when your order ships.</p>
                <br/>
                <p>Best regards,<br/>The E-Commerce Team</p>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_configuration["Email:From"]));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart(TextFormat.Html) { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    _configuration["Email:Host"],
                    int.Parse(_configuration["Email:Port"]),
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {ToEmail}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}", to);
                throw;
            }
        }
    }
}