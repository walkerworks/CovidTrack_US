using CovidTrackUS_Core.Models.Data;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CovidTrackUS_Core.Interfaces
{
    public interface ICovidNotifier
    {
        Task Notify(ILogger log);
        Task Notify(Subscriber subscriber);
    }
}