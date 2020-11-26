//using CovidTrackUS_Core.Models;
//using CovidTrackUS_Core.Interfaces;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using Newtonsoft.Json;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Threading.Tasks;
//using MessageBird.Objects;
//using MessageBird.Exceptions;

//namespace CovidTrackUS_Core.Services
//{
//    /// <summary>
//    /// This is MessageBird's implementation of the <see cref="ISMSSender"/> interface to send notification
//    /// messages through a 3rd party SMS API.
//    /// </summary>
//    public class MessageBirdSMSSender : ISMSSender
//    {

//        private readonly SMSSettings _smsSettings;
//        private readonly ILogger<MessageBirdSMSSender> _logger;

//        /// <summary>
//        /// Constructor for the MessageBirdSMSService.  Takes dependency injected services to 
//        /// send SMS notifications.
//        /// </summary>
//        /// </summary>
//        /// <param name="settings">DI <see cref="SMSSettings"/> for the application.</param>
//        /// <param name="logger">The <see cref="ILogger"/> to be used in logging</param>
//        public MessageBirdSMSSender(IOptions<SMSSettings> settings, ILogger<MessageBirdSMSSender> logger)
//        {
//            _smsSettings = settings.Value;
//            _logger = logger;

//        }


//        /// <summary>
//        /// Sends a notification SMS via MessageBird to the provided phone numbers.
//        /// </summary>
//        /// <param name="toPhoneNumbers">Array of phone numbers  to send notifications to.</param>
//        /// <param name="txt">The body of the SMS message to send</param>
//        /// <returns></returns>
//        public async Task<bool> SendMessageAsync(long[] toPhoneNumbers, string txt)
//        {
//            var lastProcessedIndex = 0;
//            /* MessageBird allows 50 phone numbers per call.  Batch these in 50's */
//            while (toPhoneNumbers.Skip(lastProcessedIndex).Count() > 0)
//            {
//                var processBatch = toPhoneNumbers.Skip(lastProcessedIndex).Take(50);
//                lastProcessedIndex = lastProcessedIndex + 50;

//                using (var client = new HttpClient())
//                {
//                    try
//                    {
//                        var url = "https://rest.messagebird.com/messages";
//                        client.DefaultRequestHeaders.Add("Authorization", $"AccessKey {_smsSettings.ApiKey}");
//                        var formContent = new FormUrlEncodedContent(new[]
//                        {
//                        new KeyValuePair<string, string>("recipients", string.Join(",",processBatch.Select(pn => pn.ToString()))),
//                        new KeyValuePair<string, string>("originator", _smsSettings.Originator),
//                        new KeyValuePair<string, string>("body", txt)
//                    });
//                        var response = await client.PostAsync(url,formContent);
//                        var stringContent = await response.Content.ReadAsStringAsync();
//                        Message message = JsonConvert.DeserializeObject<Message>(stringContent);
//                        if (message.Recipients.TotalDeliveryFailedCount > 0)
//                        {
//                            var failed = message.Recipients.Items.Where(m => m.Status != Recipient.RecipientStatus.Sent);
//                            _logger.LogWarning("The following SMS message: [{0}] failed to be delivered to the following phone numbers: [{1}]", txt, string.Join(",", failed.Select(f => f.Msisdn)));
//                        }
//                    }
//                    catch (ErrorException e)
//                    {
//                        // Either the request fails with error descriptions from the endpoint.
//                        if (e.HasErrors)
//                        {
//                            foreach (Error error in e.Errors)
//                            {
//                                _logger.LogError("MessageBird send failure. code: {0} description: '{1}' parameter: '{2}'", error.Code, error.Description, error.Parameter);
//                            }
//                        }
//                        // or fails without error information from the endpoint, in which case the reason contains a 'best effort' description.
//                        if (e.HasReason)
//                        {
//                            _logger.LogError("MessageBird send failure. Reason: {0}", e.Reason);
//                        }
//                    }
//                }
//            }
//            return true;
//        }
//    }
//}
