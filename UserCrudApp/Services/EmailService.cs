using System.Net;
using System.Net.Mail;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
            return;

        if (!MailAddress.TryCreate(to, out _))
            return;

        var smtp = _config.GetSection("Smtp");

        using var message = new MailMessage
        {
            From = new MailAddress(smtp["UserName"], "Helpdesk System"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(to);

        using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]))
        {
            Credentials = new NetworkCredential(
                smtp["UserName"],
                smtp["Password"]
            ),
            EnableSsl = bool.Parse(smtp["EnableSsl"])
        };

        await client.SendMailAsync(message);
    }

    //public async Task SendAsync(string to, string subject, string body)
    //{
    //    var smtp = _config.GetSection("Smtp");

    //    var message = new MailMessage
    //    {
    //        From = new MailAddress(smtp["UserName"], "Helpdesk System"),
    //        Subject = subject,
    //        Body = body,
    //        IsBodyHtml = true
    //    };

    //    message.To.Add(to);

    //    using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]))
    //    {
    //        Credentials = new NetworkCredential(
    //            smtp["UserName"],
    //            smtp["Password"]
    //        ),
    //        EnableSsl = bool.Parse(smtp["EnableSsl"])
    //    };

    //    await client.SendMailAsync(message);
    //}
}
