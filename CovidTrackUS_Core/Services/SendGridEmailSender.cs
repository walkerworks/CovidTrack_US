using CovidTrackUS_Core.Models;
using CovidTrackUS_Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Linq;
using System.Threading.Tasks;
using CovidTrackUS_Core.Models.Data;
using System.Dynamic;
using System;

namespace CovidTrackUS_Core.Services
{
    /// <summary>
    /// Sendgrid's implemetation of the IEmailSender
    /// </summary>
    public class SendGridEmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;

        /// <summary>
        /// Constructor for this implementation of the <see cref="IEmailSender"/> interface.
        /// </summary>
        /// <param name="emailSettings">DI settings for email service.</param>
        public SendGridEmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        /// <summary>
        /// Sends an email with an expirating one time login link for them to gain access to their
        /// account and preferences in the application
        /// </summary>
        /// <param name="email">The email to send the login link to.</param>
        /// <param name="key">The generated <see cref="LoginKey"/> Kee property to send to the subscriber.</param>
        /// <returns>Bool as to the success of this call.</returns>
        public async Task<bool> SendLoginKeyEmailAsync(string email, string key)
        {
            var client = new SendGridClient(_emailSettings.ApiKey);

            var from = new EmailAddress(_emailSettings.LoginAddress, _emailSettings.FriendlyLogin);
            var jsonParameters = new { HANDLE = email, KEY = key, DOMAIN = $"https://{_emailSettings.Host}" } ;
            var msg = MailHelper.CreateSingleTemplateEmail(from, new EmailAddress(email), _emailSettings.LoginKeyTemplateID, jsonParameters);

            var response = await client.SendEmailAsync(msg);
            return (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Sends a notification email via SendGrid built from the given <see cref="County"> data</see> to 
        /// the provided email addresses.
        /// </summary>
        /// <param name="subscribers">Array of <see cref="Subscriber"/> to send notifications to.</param>
        /// <param name="data">The <see cref="County"/> to build the email content from.</param>
        /// <returns>Bool as to the success of this call with the EmailService.</returns>
        public async Task<bool> SendNotificationEmailAsync(Subscriber subscriber, County[] data)
        {
            var client = new SendGridClient(_emailSettings.ApiKey);

            var from = new EmailAddress(_emailSettings.NotifyAddress, _emailSettings.FriendlyNotify);
            var to = new EmailAddress(subscriber.Handle);

            var templateParameters = getJSONParameters(data);
            var msg = MailHelper.CreateSingleTemplateEmail(from, to, _emailSettings.NotificationTemplateID, templateParameters);

            var response = await client.SendEmailAsync(msg);
            return (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Build the Dyanmic Email Template variables for the SendGrid template being used for this notification message.
        /// </summary>
        /// <param name="data">An array of <see cref="County"/> objects to build the template variables from.</param>
        /// <returns>A <see cref="NotifyEmailParams"/> object</returns>
        private object getJSONParameters(County[] data)
        {
            dynamic parameters = new ExpandoObject();
            parameters.DOMAIN = $"https://{_emailSettings.Host}";
            parameters.ASOFDATE = DateTime.Today.ToString("dddd, MMMM d");
            parameters.NOTIFICATIONS = data.Where(d => d.ActiveCases.HasValue).Select(d => new
            {
                COUNTY_NAME = d.Name.Replace(" County",""),
                STATE_NAME = d.StateAbbreviation,
                ACTIVE_CASES_PER_MILLION = d.ActiveCasesTodayPerMillion.HasValue ? d.ActiveCasesTodayPerMillion.Value.ToString("N0") : null,
                PERCENT_CHANGE_WEEK = d.PastWeekPercentChange.HasValue ? County.IncreaseOrDecreaseBlurb(d.PastWeekPercentChange) : null,
                PERCENT_CHANGE_DAY = d.YesterdayPercentChange.HasValue ? County.IncreaseOrDecreaseBlurb(d.YesterdayPercentChange) : null,
            });
            return parameters;
        }
    }
}