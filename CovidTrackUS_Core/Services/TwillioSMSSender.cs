using CovidTrackUS_Core.Interfaces;
using CovidTrackUS_Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;

namespace CovidTrackUS_Core.Services
{
    /// <summary>
    /// This is Twilio's implementation of the <see cref="ISMSSender"/> interface to send notification
    /// messages through a 3rd party SMS API.
    /// </summary>
    public class TwillioSMSSender : ISMSSender
    {

        private readonly SMSSettings _smsSettings;
        private readonly ILogger<TwillioSMSSender> _logger;

        /// <summary>
        /// Constructor for the TwillioSMSSender Service.  Takes dependency injected services to 
        /// send SMS notifications.
        /// </summary>
        /// </summary>
        /// <param name="settings">DI <see cref="SMSSettings"/> for the application.</param>
        /// <param name="logger">The <see cref="ILogger"/> to be used in logging</param>
        public TwillioSMSSender(IOptions<SMSSettings> settings, ILogger<TwillioSMSSender> logger)
        {
            _smsSettings = settings.Value;
            _logger = logger;

        }

        /// <summary>
        /// Sends a notification SMS via Twilio to the provided phone numbers.
        /// </summary>
        /// <param name="toPhoneNumber">Pphone number to send notification to.</param>
        /// <param name="fromNumber">Which phone number to send this meesage from</param>
        /// <param name="txt">The body of the SMS message to send</param>
        /// <returns></returns>
        public async Task<bool> SendMessageAsync(string toPhoneNumber, string fromNumber, string txt)
        {
            string accountSid = _smsSettings.Sid;
            string authToken = _smsSettings.Token;
            try
            {
                TwilioClient.Init(accountSid, authToken);

                var message = await MessageResource.CreateAsync(
                    body: txt,
                    from: new Twilio.Types.PhoneNumber(fromNumber),
                    to: new Twilio.Types.PhoneNumber(toPhoneNumber)
                );
                return true;
            }
            catch (ApiException ex)
            {
                var errorMsg = "";
                switch (ex.Code)
                {
                    case 21422:
                        errorMsg = "Phone number is unavailable";
                        break;
                    case 21421:
                        errorMsg = "Phone number is invalid";
                        break;
                    default:
                        errorMsg = "Unknown error";
                        break;
                }
                _logger.LogWarning("The following SMS message: [{0}] failed to be delivered to the following phone number: [{1}]. Error: {2}", txt, toPhoneNumber, errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("The following SMS message: [{0}] failed to be delivered to the following phone number: [{1}]. Error: {2}", txt, toPhoneNumber, ex.Message);
                return false;
            }
        }
    }
}
