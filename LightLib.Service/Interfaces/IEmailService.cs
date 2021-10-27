using LightLib.Models.Email;
using System.Threading.Tasks;

namespace LightLib.Service.Interfaces
{
    public interface IEmailService
    {
        Task Send(EmailModel model);
    }
}
