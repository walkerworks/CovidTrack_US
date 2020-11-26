using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CovidTrackUS_Core.Enums;
using CovidTrackUS_Core.Interfaces;
using CovidTrackUS_Core.Models.Data;
using CovidTrackUS_Core.Services;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CovidTrackUS_Web.Controllers
{
    [Route("api")]
    public class APIController : Controller
    {

        private readonly EmailService _emailService;
        private readonly SMSService _smsService;
        private readonly IDataService _dataService;
        private readonly ILogger<APIController> _logger;

        /// <summary>
        /// Constructor for <see cref="APIController"/> Handles Services Dependency Injection
        /// </summary>
        /// <param name="emailService">The injected <see cref="EmailService"/> service</param>
        /// <param name="smsService">The injected <see cref="SMSService"/> service</param>
        /// <param name="dataService">The injected <see cref="IDataService"/> service</param>
        public APIController(EmailService emailService, SMSService smsService, IDataService dataService, ILogger<APIController> logger)
        {
            _emailService = emailService;
            _smsService = smsService;
            _dataService = dataService;
            _logger = logger;
        }

        /// <summary>
        /// Generates and sends a quick expiring <see cref="LoginKey"/> to a 
        /// new or existing subscriber for login.
        /// </summary>
        [HttpPost("confirm-user-send-login")]
        public async Task<JsonResult> ConfirmUserSendLogin([FromForm] string handle)
        {
            try
            {
                if (string.IsNullOrEmpty(handle))
                {
                    return new JsonResult(new { sent = "", message = "No method was found to contact you.  Did you enter a phone number or email address?" });
                }
                handle = handle.Trim();
                dynamic result = new ExpandoObject();
                if (handle.Contains('@'))
                {
                    if (!EmailService.IsValidEmail(handle))
                    {
                        result.message = "Email does not appear to be valid.  Try again";
                        result.sent = "";
                    }
                    else
                    {
                        if (await _emailService.SendLoginKeyEmailAsync(handle))
                        {
                            result.sent = "email";
                            result.message = "An email has been sent to this address with a link to login. Click that link to proceed.";
                        }
                        else
                        {
                            result.message = "An unknown error occurred.  Please try again later.";
                            result.sent = "";
                        }
                    }
                }
                else
                {
                    //Validate the phone number length and format
                    var phoneDigits = SMSService.PullOutOnlyDigits(handle);
                    if (phoneDigits.Length != 10)
                    {
                        result.sent = "";
                        result.message = "Phone number does not appear to be valid.  Please use full 10 digit number";
                    }
                    else
                    {
                        if (await _smsService.SendLoginKeySMSAsync(phoneDigits))
                        {
                            result.sent = "phone";
                            result.message = "A text message has been sent to that phone number with a link to login. Tap that link to proceed.";
                        }
                        else
                        {
                            result.sent = "";
                            result.message = "An unknown error occurred. Please try again later.";
                        }
                    }
                }
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Message: {0}, Stack: {1}", ex.Message, ex.StackTrace);
                return new JsonResult(new { sent = "", message = "An unknown error occurred. Please try again later." });
            }
        }

        /// <summary>
        /// Records a subscriber's feedback as positive or negative
        /// </summary>
        /// <param name="id">The <see cref="Subscriber"/> Id leaving the feedback</param>
        /// <param name="kind">pos or neg for Postive of Negative feedback</param>
        [Route("feedback/{id}/{kind}")]
        [HttpGet]
        public async Task<ActionResult> RecordFeedback(int id, string kind)
        {
            if (string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(kind) || !(new []{ "pos","neg"}).Contains(kind.ToLower()))
            {
                return new BadRequestObjectResult("Missing or incorrect information");
            }

            SubscriberFeedback sf = new SubscriberFeedback()
            {
                SubscriberID = id,
                IsPositive = kind.Equals("pos", StringComparison.OrdinalIgnoreCase),
                CreatedOn = DateTime.UtcNow
            };
            await _dataService.ExecuteInsertAsync(sf);
            return new RedirectResult($"/#/feedback/{sf.ID}/{id}");
        }

        /// <summary>
        /// Adds additional comments to an existing <see cref="SubscriberFeedback"/> object
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("feedback/add")]
        public async Task<JsonResult> AddToFeedback([FromBody] JObject form)
        {
            string strFeedbackId = form.Value<string>("feedbackId");
            string strSubscriberId = form.Value<string>("subscriberId");
            string feedback = form.Value<string>("feedback");
            int feedbackId, subscriberId;
            if(!int.TryParse(strFeedbackId, out feedbackId) || !int.TryParse(strSubscriberId, out subscriberId))
            {
                return new JsonResult(new { error = "Missing or incorrect information" });
            }

            // Retrieve the existing feedback (use the subscriber AND id to ensure people don't go fishing for feedback objects */
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@id", feedbackId);
            parameters.Add("@subscriberId", subscriberId);
            var feedbackObj = await _dataService.QueryFirstOrDefaultAsync<SubscriberFeedback>("Select top 1 * from SubscriberFeedback where id = @id and subscriberId = @subscriberId", parameters);
            if(feedbackObj == null)
            {
                return new JsonResult(new { error = "Missing or incorrect information" });
            }
            feedbackObj.Feedback = feedback;
            await _dataService.ExecuteUpdateAsync(feedbackObj);
            return new JsonResult(new { success = true });
        }


        /// <summary>
        /// Authenticates a handle against a quick expiring <see cref="LoginKey"/> that was generated for their 
        /// one time login key.  
        /// </summary>
        [Route("login-with-key/{handle}/{key}")]
        [HttpGet]
        public async Task<ActionResult> LoginWithKey(string handle, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(handle) || string.IsNullOrEmpty(key))
                {

                    return new BadRequestObjectResult("Missing or incorrect information");
                }
                key = key.Trim();
                if (key.Length != 7)
                {
                    return new BadRequestObjectResult("Invalid login key");
                }
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@handle", handle);
                parameters.Add("@key", key);
                // Check if this code/handle combination exists.
                var loginKey = await _dataService.QueryFirstOrDefaultAsync<LoginKey>("select * from LoginKey where handle = @handle and Kee = @key and ExpiresOn > getdate()", parameters);
                if (loginKey == null)
                {
                    return new BadRequestObjectResult("Expired or invalid login key");
                }
                else
                {
                    //Remove the now "used" loginkey row
                    DynamicParameters KeyParameters = new DynamicParameters();
                    KeyParameters.Add("@ID", loginKey.ID);
                    await _dataService.ExecuteNonQueryAsync(@"delete from LoginKey where Id = @ID", KeyParameters);

                    int subscriberId;
                    //Make sure subscriber doesn't exist one more time before adding new one -
                    var existingSubscriber = await GetSubscriberByHandle(handle.Trim());
                    if (existingSubscriber != null)
                    {
                        subscriberId = existingSubscriber.ID;
                    }
                    else
                    {
                        //Add new subscriber
                        HandleType type = handle.Contains('@') ? HandleType.Email : HandleType.Phone;
                        Subscriber sub = new Subscriber() { Handle = handle, Type = type, Verified = true, CreatedOn = DateTime.Now };
                        subscriberId = await _dataService.ExecuteInsertAsync(sub);
                    }

                    //Log the new subscriber in 
                    await LoginSubscriberAsync(subscriberId);
                    return new RedirectResult("/");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Message: {0}, Stack: {1}", ex.Message, ex.StackTrace);
                return new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// Checks the login status of the current cookie/identity
        /// </summary>
        [HttpGet("check-login")]
        public JsonResult CheckLogin()
        {
            var loggedIn = !string.IsNullOrEmpty(HttpContext?.User?.Identity?.Name);
            return new JsonResult(new { loggedIn });
        }

        /// <summary>
        /// Logs out the currently logged in user
        /// </summary>
        [HttpPost("logout")]
        public async Task<ActionResult> LogoutUser()
        {
            await HttpContext.SignOutAsync();
            return new RedirectResult("/");
        }

        /// <summary>
        /// Removes/Deletes the currently logged in user from the database
        /// </summary>
        [HttpPost("unsubscribe")]
        public async Task<JsonResult> Unsubscribe()
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext?.User?.Identity?.Name))
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@handle", HttpContext.User.Identity.Name);
                    await _dataService.ExecuteNonQueryAsync("delete from subscriber where handle = @handle", parameters);
                    await HttpContext.SignOutAsync();
                    return new JsonResult(new { success = true });
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("trying to delete a user...Message: {0}, Stack: {1}", ex.Message, ex.StackTrace);
            }
            return new JsonResult(new { error= "Error" });
        }

        /// <summary>
        /// Gets the county subscription data for the currently logged in user
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("get-county-data")]
        public async Task<JsonResult> GetSubscriberCounties()
        {
            if (!string.IsNullOrEmpty(HttpContext?.User?.Identity?.Name))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@handle", HttpContext.User.Identity.Name);
                var subscriber = await _dataService.QueryFirstOrDefaultAsync<Subscriber>("select * from Subscriber where handle = @handle", parameters);
                if (subscriber == null)
                {
                    return new JsonResult(new { error = "Bad user data. User not found." });
                }
                parameters = new DynamicParameters();
                parameters.Add("@id", subscriber.ID);
                var currentCounties = await _dataService.FindAsync<CountySubscriber>("select CS.*,C.FIPS from CountySubscriber CS join COunty C on C.ID = CS.CountyID where CS.subscriberId = @id", parameters);
                parameters = new DynamicParameters();
                parameters.Add("@ids", currentCounties.Select(cc => cc.CountyID).ToArray());
                var counties = await _dataService.FindAsync<County>("select * from County where ID in @ids", parameters);

                var countyData = currentCounties.Select(cc => new CountyData() 
                { id = cc.FIPS, 
                  frequency = cc.Frequency.ToString(), 
                  name = counties.First(c => c.ID == cc.CountyID).Name.Replace(" County", ""), 
                  state = counties.First(c => c.ID == cc.CountyID).StateAbbreviation 
                }
                ).ToArray(); ;
                return new JsonResult(countyData);
            }
            return new JsonResult(new { error = "Bad user data. User not found." });
        }

        /// <summary>
        /// Saves county data for the currently logged in user
        /// </summary>
        [HttpPost]
        [Authorize]
        [Route("save-counties")]
        public async Task<JsonResult> SaveCounties([FromBody] CountyData[] countyPayload)
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpContext?.User?.Identity?.Name))
                {
                    DynamicParameters parameters = new DynamicParameters();
                    parameters.Add("@handle", HttpContext.User.Identity.Name);
                    var subscriber = await _dataService.QueryFirstOrDefaultAsync<Subscriber>("select * from Subscriber where handle = @handle", parameters);
                    if (subscriber == null)
                    {
                        return new JsonResult(new { error = "Bad user data. User not found." });
                    }
                    parameters = new DynamicParameters();
                    parameters.Add("@id", subscriber.ID);
                    var currentCounties = await _dataService.FindAsync<CountySubscriber>("select CS.*,C.FIPS from CountySubscriber CS join COunty C on C.ID = CS.CountyID where CS.subscriberId = @id", parameters);
 
                    // The incoming county FIPS
                    var payloadFIPS = countyPayload.Select(cp => cp.id);
                    // The existing county FIPS
                    var currentFIPS = currentCounties.Select(cc => cc.FIPS);

                    // Delete the CountySubscriber rows who's County FIPS isn't in the payload FIPS:
                    var FIPSToDelete = currentFIPS.Where(i => !payloadFIPS.Contains(i)).ToArray();
                    if (FIPSToDelete.Any())
                    {
                        parameters = new DynamicParameters();
                        parameters.Add("@ids", currentCounties.Where(cc => FIPSToDelete.Contains(cc.FIPS)).Select(cc => cc.ID).ToArray());
                        await _dataService.ExecuteNonQueryAsync("Delete from CountySubscriber where ID in @Ids", parameters);
                    }

                    // Loop through the payload and insert/update the remaining rows for this subscriber
                    foreach (var county in countyPayload)
                    {
                        // If they already are subscribed to this county, update the current row (only if it changed)
                        var existing = currentCounties.FirstOrDefault(cc => county.id == cc.FIPS);
                        if (existing != null)
                        {
                            if (!existing.Frequency.Equals(county.frequency,StringComparison.OrdinalIgnoreCase))
                            {
                                existing.Frequency = county.frequency;
                                await _dataService.ExecuteUpdateAsync(existing);
                            }
                        }
                        else
                        {
                            // It's a new row.  Create and insert it
                            parameters = new DynamicParameters();
                            parameters.Add("@SubscriberID", subscriber.ID);
                            parameters.Add("@Frequency", county.frequency);
                            parameters.Add("@FIPS", county.id);
                            await _dataService.ExecuteNonQueryAsync(@"insert into CountySubscriber (SubscriberID, CountyID, Frequency, LastNotification)
                                                                        select @SubscriberID, ID, @Frequency, null
                                                                        from County where FIPS = @FIPS",parameters);
                        }
                    }
                    return new JsonResult(new { message = "Updated!" });
                }
                else
                {
                    return new JsonResult(new { error = "Bad user data. User not found." });
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("Message: {0}, Stack: {1}", ex.Message, ex.StackTrace);
                return new JsonResult(new { error = "Error retrieving county data for subscriber" });
            }
        }

        #region Helpers 


        private async Task<Subscriber> GetSubscriberByHandle(string handle)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@handle", handle);
            return await _dataService.QueryFirstOrDefaultAsync<Subscriber>("select * from Subscriber where Handle = @handle", parameters);
        }

        /// <summary>
        /// Sets a cookie for the cuirrent subscriber to log them in 
        /// </summary>
        /// <param name="subscriberId">The ID of the subscriber we're logging in</param>
        /// <returns>Whether or not this was successful</returns>
        /// <remarks>This does not validate any passwords, we assume they have already authenticated with a email or SMS link before
        /// this method is called</remarks>
        private async Task<bool> LoginSubscriberAsync(int subscriberId)
        {
            DynamicParameters parameters = new DynamicParameters();
            parameters.Add("@ID", subscriberId);
            Subscriber subscriber = await _dataService.QueryFirstOrDefaultAsync<Subscriber>("Select * from Subscriber where ID = @ID", parameters);
            if (subscriber != null)
            {
                var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, subscriber.Handle),
                        new Claim(ClaimTypes.Role, "Subscriber"),
                    };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties {IsPersistent = true};

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);
                return true;

            }
            return false;
        }


        #endregion

        public class CountyData
        {
            public string id { get; set; }
            public string state { get; set; }
            public string name { get; set; }
            public string frequency { get; set; }
        }
    }
}
