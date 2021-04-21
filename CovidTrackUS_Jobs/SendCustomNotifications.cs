using CovidTrackUS_Core.Interfaces;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CovidTrackUS_Jobs
{
    public class SendCustomNotifications
    {
        ICovidNotifier _notifyService;
        public SendCustomNotifications(ICovidNotifier notifyService)
        {
            _notifyService = notifyService;
        }

        /*
        [FunctionName("SendCustomNotifications")]
        /// <summary>
        /// SendCustomNotifications
        /// </summary>
        /// <param name="timer">Currently for April 21, 2021 12:07pm EST</param>
        /// <returns></returns> 
        public async Task RunAsync([TimerTrigger("0 07 16 21 4 3")] TimerInfo timer, ILogger log)
        {
            log.LogInformation($"*** Starting SendCustomNotifications job ***");
            await _notifyService.SubscriberReachout(log);
            log.LogInformation($"*** Ending SendCustomNotifications job ***");
        }
        */
    }
}
