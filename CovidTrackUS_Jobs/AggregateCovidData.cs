using CovidTrackUS_Core.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CovidTrackUS_Jobs
{
    public class AggregateCovidData
    {
        ICovidDataUpdater _updaterService;
        public AggregateCovidData(ICovidDataUpdater updaterService)
        {
            _updaterService = updaterService;
        }

        [FunctionName("AggregateCovidData")]
        /// <summary>
        /// AggregateCovidData
        /// </summary>
        /// <param name="timer">Runs Every day at 3:00am ET (8am UTC for EST, 7am UTC for EDT)</param>
        /// <returns></returns>
        public async Task RunAsync([TimerTrigger("0 0 7 * * *")] TimerInfo timer, ILogger log)
        {
            log.LogInformation($"*** Starting AggregateCovidData job ***");
            await _updaterService.RetrieveAndCrunch(log);
            log.LogInformation($"*** Ending AggregateCovidData job ***");
        }
    }
}
