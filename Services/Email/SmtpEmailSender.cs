using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace GanaderiaControl.Services.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;

    public SmtpEmailSender(IOptions<EmailOptions> opt) => _opt = opt.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_opt.SmtpHost, _opt.SmtpPort)
        {
            EnableSsl = _opt.UseSsl || _opt.UseStartTls,
            Credentials = new NetworkCredential(_opt.Username, _opt.Password)
        };

        var msg = new MailMessage
        {
            From = new MailAddress(_opt.FromAddress, _opt.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(to);

        using var reg = ct.Register(() => client.SendAsyncCancel());
        await client.SendMailAsync(msg);
    }
}
