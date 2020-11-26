using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CovidTrackUS_Core.Interfaces
{
    public interface ICovidDataUpdater
    {
        Task RetrieveAndCrunch(ILogger log);
    }
}