using CovidTrackUS_Core.Models;
using CovidTrackUS_Core.Models.Data;
using System.Threading.Tasks;

namespace CovidTrackUS_Core.Interfaces
{
    public interface IEmailSender
    {
        Task<bool> SendNotificationEmailAsync(Subscriber subscriber, County[] data);

        Task<bool> SendCustomEmailAsync(Subscriber subscriber);

        Task<bool> SendLoginKeyEmailAsync(string email, string key);
    }
}
