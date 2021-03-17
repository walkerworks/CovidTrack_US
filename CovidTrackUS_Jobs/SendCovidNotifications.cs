using CovidTrackUS_Core.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CovidTrackUS_Jobs
{
    public class SendCovidNotifications
    {
        ICovidNotifier _notifyService;
        public SendCovidNotifications(ICovidNotifier notifyService)
        {
            _notifyService = notifyService;
        }

        [FunctionName("SendCovidNotifications")]
        /// <summary>
        /// SendCovidNotifications
        /// </summary>
        /// <param name="timer">Runs Every day at 7:30am ET (12:30am UTC for EST, 11:30am UTC for EDT)</param>
        /// <returns></returns>
        public async Task RunAsync([TimerTrigger("0 30 11 * * *")] TimerInfo timer, ILogger log)
        {
            log.LogInformation($"*** Starting SendCovidNotifications job ***");
            await _notifyService.Notify(log);
            log.LogInformation($"*** Ending SendCovidNotifications job ***");
        }
    }
}
