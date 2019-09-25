using System.Threading.Tasks;

namespace PushValidator.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
}
