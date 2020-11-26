using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CovidTrackUS_Core.Interfaces;
using System.Text.RegularExpressions;
using System.Globalization;
using CovidTrackUS_Core.Models.Data;
using Polly.Retry;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System.Linq;

namespace CovidTrackUS_Core.Services
{
    /// <summary>
    /// This class is used by the application to send notification
    /// emails through a DI 3rd party email API.
    /// </summary>
    public class EmailService
    {

        private readonly IEmailSender _emailSender;
        private readonly ILogger<EmailService> _logger;
        private readonly IDataService _dataService;
        private AsyncRetryPolicy _retryPolicy;

        /// <summary>
        /// Constructor for Email Service. Takes dependency injected services to 
        /// send email notifications.
        /// </summary>
        /// <param name="emailSender">The <see cref="IEmailSender"/> implementation to use in this Email Service</param>
        /// <param name="logger">The <see cref="ILogger"/> to be used in logging</param>
        /// <param name="dataService">The <see cref="IDataService"/> used for data access</param>
        public EmailService(IEmailSender emailSender, ILogger<EmailService> logger, IDataService dataService)
        {
            _emailSender = emailSender;
            _logger = logger;
            _dataService = dataService;

            var maxDelay = TimeSpan.FromSeconds(36);
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 50)
                .Select(s => TimeSpan.FromTicks(Math.Min(s.Ticks, maxDelay.Ticks)));

            _retryPolicy = Policy
              .Handle<Exception>()
              .WaitAndRetryAsync(delay);
        }

        /// <summary>
        /// Sends a notification email build from the given <see cref="County"> data</see> to 
        /// the provided email addresses.
        /// </summary>
        /// <param name="emails">Array of email addresses to send notifications to.</param>
        /// <param name="data">The <see cref="County"/> to build the email content from.</param>
        /// <returns>Bool as to the success of this call with the EmailService.</returns>
        public async Task<bool> SendNotificationEmailAsync(Subscriber subscriber, County[] data)
        {
            if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>  await _emailSender.SendNotificationEmailAsync(subscriber, data));
              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failure to send notification email {subscriber.Handle}, {ex.Message}, {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Sends an email with an expirating one time login link for subscriber to gain access to their
        /// account and preferences in the application
        /// </summary>
        /// <param name="subscriber">The Subscriber to send the login link to.</param>
        /// <returns>Bool as to the success of this call with the EmailService.</returns>
        public async Task<bool> SendLoginKeyEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            LoginKey key = LoginKey.GenerateFor(email);
            await _dataService.ExecuteInsertAsync(key);

                return await _retryPolicy.ExecuteAsync(async () => await _emailSender.SendLoginKeyEmailAsync(email, key.Kee));

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failure to send login email {email}, {ex.Message}, {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Test validity of Email address from a format standpoint
        /// </summary>
        /// <param name="email">The email to test</param>
        /// <returns>True or false as to whether email is valid.</returns>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    string domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }


    }


}
