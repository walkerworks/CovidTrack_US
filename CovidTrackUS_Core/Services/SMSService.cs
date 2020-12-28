using System;
using CovidTrackUS_Core.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CovidTrackUS_Core.Models.Data;
using CovidTrackUS_Core.Models;
using Microsoft.Extensions.Options;
using Polly.Retry;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Linq;

namespace CovidTrackUS_Core.Services
{
    /// <summary>
    /// This class is used by the application to send notification
    /// messages through a 3rd party SMS API.
    /// </summary>
    public class SMSService
    {
        private readonly ISMSSender _smsSender;
        private readonly ILogger<SMSService> _logger;
        private readonly IDataService _dataService;
        private readonly SMSSettings _smsSettings;
        private AsyncRetryPolicy _retryPolicy;

        /// <summary>
        /// Constructor for SMS Service. Takes dependency injected services to 
        /// send SMS notifications.
        /// </summary>
        /// <param name="smsSender">The <see cref="ISMSSender"/> implementation to use in this SMS Service</param>
        /// <param name="logger">The <see cref="ILogger"/> to be used in logging</param>
        /// <param name="dataService">The <see cref="IDataService"/> used for data access</param>
        /// <param name="settings">DI <see cref="SMSSettings"/> for the application.</param>
        public SMSService(ISMSSender smsSender, ILogger<SMSService> logger, IDataService dataService,IOptions<SMSSettings> settings)
        {
            _smsSender = smsSender;
            _logger = logger;
            _dataService = dataService;
            _smsSettings = settings.Value;
            var maxDelay = TimeSpan.FromSeconds(36);
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 50)
                .Select(s => TimeSpan.FromTicks(Math.Min(s.Ticks, maxDelay.Ticks)));

            _retryPolicy = Policy
              .Handle<Exception>()
              .WaitAndRetryAsync(delay);
        }

        /// <summary>
        /// Sends a one time login link via SMS to a susbscribers phone number
        /// </summary>
        /// <param name="phoneDigits">The subscriber phone number send a login link to</param>
        /// <returns>Whether or not the SMS message sent successfully</returns>
        public async Task<bool> SendLoginKeySMSAsync(string phoneDigits)
        {
            if (string.IsNullOrEmpty(phoneDigits)) throw new ArgumentNullException(nameof(phoneDigits));
            LoginKey key = LoginKey.GenerateFor(phoneDigits);
            try
            {
                await _dataService.ExecuteInsertAsync(key);
                var txt = $"Your login link: https://{_smsSettings.Host}/api/login-with-key/{phoneDigits}/{key.Kee}";
                return await _retryPolicy.ExecuteAsync(async () => await _smsSender.SendMessageAsync(phoneDigits, _smsSettings.VerificationNumber, txt));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failure to send login SMS {phoneDigits}, {ex.Message}, {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Sends a prebuild notification SMS build to the provided phone numbers.
        /// </summary>
        /// <param name="toPhoneNumbers">Array of phone numbers to send notifications to.</param>
        /// <param name="txt">The SMS message content to send to the given <paramref name="toPhoneNumbers"/>.</param>
        /// <returns>Bool as to the success of this call with the SMSService.</returns>
        public async Task<bool> SendMessage(string toPhoneNumber, string txt)
        {
            if (string.IsNullOrEmpty(toPhoneNumber)) throw new ArgumentNullException(nameof(toPhoneNumber));

            try
            {
                return await _retryPolicy.ExecuteAsync(async () => await _smsSender.SendMessageAsync(toPhoneNumber, _smsSettings.NotificationNumber, txt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failure to send notification SMS {toPhoneNumber}, {ex.Message}, {ex.StackTrace}"); ;
                return false;
            }
        }

        /// <summary>
        /// Just get the digits out of whatever is provided in a string
        /// </summary>
        /// <param name="stringWithNumbers">The string to extract digits from</param>
        /// <returns>Only the digits of a given string</returns>
        public static string PullOutOnlyDigits(string stringWithNumbers)
        {
            if (string.IsNullOrWhiteSpace(stringWithNumbers)) return string.Empty;
            string b = string.Empty;
            for (int i = 0; i < stringWithNumbers.Length; i++)
            {
                if (char.IsDigit(stringWithNumbers[i]))
                    b += stringWithNumbers[i];
            }

            return b;
        }
    }
}
