using System.Threading.Tasks;

namespace ETL.Infrastructure.Services.Email;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}