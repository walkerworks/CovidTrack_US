using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CovidTrackUS_Core.Enums;
using CovidTrackUS_Core.Interfaces;
using CovidTrackUS_Core.Models;
using CovidTrackUS_Core.Models.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CovidTrackUS_Core.Services
{
    /// <summary>
    /// Queries DB for subscribers that are to be notified today - builds and sends notifications
    /// </summary>
    public class CovidNotifier : ICovidNotifier
    {
        private readonly IDataService _dataService;
        private readonly SMSService _smsService;
        private readonly EmailService _emailService;
        private readonly SMSSettings _smsSettings;

        /// <summary>
        /// Constructor for dependency injection
        /// </summary>
        /// <param name="dataService">DI Database service</param>
        /// <param name="smsSender">DI SMS service</param>
        /// <param name="emailSender">DI Email service</param>
        /// <param name="emailSender">DI SMS Settings options</param>
        public CovidNotifier(IDataService dataService, SMSService smsService, EmailService emailService, IOptions<SMSSettings> settings)
        {
            _dataService = dataService;
            _smsService = smsService;
            _emailService = emailService;
            _smsSettings = settings.Value;
        }

        /// <summary>
        /// Send notification to a single <see cref="Subscriber"/>
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public async Task Notify(Subscriber subscriber)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@SubscriberID", subscriber.ID);
            var counties = await _dataService.FindAsync<County>("select * from County where ID in (select CountyID from CountySubscriber where SubscriberID = @SubscriberID)", parameters);
            if (subscriber.Type == HandleType.Phone)
            {
                await SendSMSMessages(subscriber.Handle, counties.ToArray());
            }
            else
            {
                await SendEmailMessages(subscriber, counties.ToArray());
            }
            /* Update the notifications */
            subscriber.Notifications = subscriber.Notifications + 1;
            await _dataService.ExecuteUpdateAsync(subscriber);
        }


        /// <summary>
        /// Send notifications to all the <see cref="Subscriber">Subscribers</see> that are due
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public async Task Notify(ILogger log)
        {
            try
            {
                // Get three results sets in one shot, stich together the results in code as needed
                var mapItems = new List<DataItemMap>(){
                    new DataItemMap(typeof(Subscriber), DataRetrieveType.List, "Subscribers"),
                    new DataItemMap(typeof(County), DataRetrieveType.List, "Counties"),
                    new DataItemMap(typeof(CountySubscriber), DataRetrieveType.List, "CountySubscribers"),
                };

                log.LogInformation("Querying DB for Subscribers to be notified");
                /* Select Daily frequencies that are over 1440 minutes (a day) due. */
                /* Select Weekly frequencies that are over 10080 minutes (7 days( due. */
                /* Select Monthly frequencies that are over a calendar month due */
                var dueNotifications = await _dataService.QueryMultipleAsync(
                    @"select * into #TempCountySubscribers from CountySubscriber CS where 
                    (Frequency = 'Daily' and(LastNotification is null or DATEDIFF(day, LastNotification, getdate()) >= 1)) or
                    (Frequency = 'Weekly' and(LastNotification is null or DATEDIFF(day, LastNotification, getdate()) >= 1)) or
                    (Frequency = 'Monthly' and(LastNotification is null or (DATEDIFF(month, LastNotification, getdate()) >= 1) and DATEPART(day, getdate()) >= DATEPART(day, LastNotification)))

                    /* Result Set 1 Subscribers */
                    select * from Subscriber where ID in (select SubscriberID from #TempCountySubscribers)

                    /* Result Set 2 Counties */
                    select * from County where ID in (select CountyID from #TempCountySubscribers)

                    /* Result Set 3 CountySubscribers */
                    select * from #TempCountySubscribers

                    drop table #TempCountySubscribers
                
                ", mapItems);

                var subscribers = ((List<dynamic>)dueNotifications.Subscribers).Cast<Subscriber>().ToDictionary(s => s.ID);
                var counties = ((List<dynamic>)dueNotifications.Counties).Cast<County>().ToDictionary(c => c.ID);
                var countySubscribers = ((List<dynamic>)dueNotifications.CountySubscribers).Cast<CountySubscriber>();

                // Hydrate CountySubscriber objects
                foreach (var cs in countySubscribers)
                {
                    cs.Subscriber = subscribers[cs.SubscriberID];
                    cs.County = counties[cs.CountyID];
                }

                /* Separate into types of notifications & group by Subscriber */
                var SMSSubscriptionsDue = countySubscribers
                    .Where(cs => cs.Subscriber.Type == HandleType.Phone)
                    .GroupBy(cs => cs.Subscriber);

                var EmailSubscriptionsDue = countySubscribers
                    .Where(cs => cs.Subscriber.Type == HandleType.Email)
                    .GroupBy(cs => cs.Subscriber);

                log.LogInformation($"{SMSSubscriptionsDue.Count()} SMS Subscribers. {EmailSubscriptionsDue.Count()} Email Subscribers");

                //Build and send an SMS to each MS notification subscriber 
                if (SMSSubscriptionsDue.Any())
                {
                    log.LogInformation("Sending SMS Messages...");
                    foreach (var group in SMSSubscriptionsDue)
                    {
                        if (await SendSMSMessages(group.Key.Handle, group.Select(g => g.County).ToArray()))
                        {
                            // If send was successful, update the last notified date on the countysubscription
                            // And increment the subscriber total sends (notifications)
                            foreach (var cs in group)
                            {
                                cs.LastNotification = DateTime.UtcNow;
                                await _dataService.ExecuteUpdateAsync(cs);
                                cs.Subscriber.Notifications = cs.Subscriber.Notifications + 1;
                                await _dataService.ExecuteUpdateAsync(cs.Subscriber);
                            }
                        }
                    }
                    log.LogInformation("...Finished SMS Sends");
                }

                if (EmailSubscriptionsDue.Any())
                {
                    log.LogInformation("Sending Email Messages...");
                    foreach (var group in EmailSubscriptionsDue)
                    {
                        if (await SendEmailMessages(group.Key, group.Select(g => g.County).ToArray()))
                        {
                            // If send was successful, update the last notified date on the countysubscription
                            // And increment the subscriber total sends (notifications)
                            foreach (var cs in group)
                            {
                                cs.LastNotification = DateTime.UtcNow;
                                await _dataService.ExecuteUpdateAsync(cs);
                                cs.Subscriber.Notifications = cs.Subscriber.Notifications + 1;
                                await _dataService.ExecuteUpdateAsync(cs.Subscriber);
                            }
                        }
                    }
                    log.LogInformation("...Finished Email Sends");
                }
            }
            catch (Exception ex)
            {
                log.LogError("Notification Failure message: {0}  stack: {1}", ex.Message, ex.StackTrace);
            }
        }


        private async Task<bool> SendEmailMessages(Subscriber subscriber, County[] counties)
        {
            //Build and send an Email to email notification subscriber 
            return await _emailService.SendNotificationEmailAsync(subscriber, counties);
        }

        private async Task<bool> SendSMSMessages(string smsHandle, County[] counties)
        {
            StringBuilder smsBuilder = new StringBuilder();
            var today = DateTime.Today.ToString("MM/dd");
            var ourLink = _smsSettings.ShortLink;
#if DEBUG
            ourLink = _smsSettings.LocalShortLink;
#endif
            var aboutDataLink = _smsSettings.AboutShortLink;
#if DEBUG
            aboutDataLink = _smsSettings.LocalAboutShortLink;
#endif
            smsBuilder.Clear();
            smsBuilder.AppendLine($"County");
            smsBuilder.AppendLine($"-Tot. Confirmed, (7-day/2-day) change");
            smsBuilder.AppendLine($"-Est. Active (per mil), (7-day/2-day) change");
            string actPerMil, pctChange, confirmedCases, confirmedChange;
            foreach (var cs in counties)
            {
                smsBuilder.AppendLine("");
                smsBuilder.AppendLine("* * * * * * * * *");
                actPerMil = pctChange = confirmedCases = confirmedChange = "";
                actPerMil = cs.ActiveCasesTodayPerMillion.HasValue ? cs.ActiveCasesTodayPerMillion.Value.ToString("N0") : "?";
                pctChange = cs.ActivePastWeekPercentChange.HasValue ? $" {County.IncreaseOrDecreaseBlurb(cs.ActivePastWeekPercentChange)} / {County.IncreaseOrDecreaseBlurb(cs.ActiveYesterdayPercentChange)}" : " ? / ?";

                actPerMil = cs.ActiveCasesTodayPerMillion.HasValue ? cs.ActiveCasesTodayPerMillion.Value.ToString("N0") : "?";
                pctChange = cs.ActivePastWeekPercentChange.HasValue ? $" {County.IncreaseOrDecreaseBlurb(cs.ActivePastWeekPercentChange)} / {County.IncreaseOrDecreaseBlurb(cs.ActiveYesterdayPercentChange)}" : " ? / ?";

                confirmedCases = cs.ConfirmedCases.HasValue ? cs.ConfirmedCases.Value.ToString("N0") : "?";
                confirmedChange = cs.ConfirmedPastWeekPercentChange.HasValue ? $" {County.IncreaseOrDecreaseBlurb(cs.ConfirmedPastWeekPercentChange)} / {County.IncreaseOrDecreaseBlurb(cs.ConfirmedYesterdayPercentChange)}" : " ? / ?";


                smsBuilder.AppendLine();
                smsBuilder.AppendLine($"{cs.Name.Replace(" County", "")}, {cs.StateAbbreviation}");
                smsBuilder.AppendLine($"-{confirmedCases}, {confirmedChange}");
                smsBuilder.AppendLine($"-{actPerMil}, {pctChange}");

            }
            smsBuilder.AppendLine();
            smsBuilder.AppendLine($"About data: {aboutDataLink}");
            smsBuilder.AppendLine($"Manage: {ourLink}");
            return await _smsService.SendMessage(smsHandle, smsBuilder.ToString());
        }
    }
}
