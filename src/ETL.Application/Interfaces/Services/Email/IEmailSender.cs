using System.Threading.Tasks;

namespace ETL.Application.Interfaces.Services.Email;

public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string message);
}