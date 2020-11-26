using System;
using CovidTrackUS_Core.Interfaces;
using CovidTrackUS_Core.Models;
using CovidTrackUS_Core.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(CovidTrackUS_Jobs.Startup))]

namespace CovidTrackUS_Jobs
{
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// 
        /// </summary>
        public IConfiguration Configuration { get; }

        public override void Configure(IFunctionsHostBuilder builder)
        {

            var config = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddLogging();
            builder.Services.Configure<Settings>(options =>
            {
                options.DataConnectionString = config.GetConnectionString("DefaultConnection");
            });
            builder.Services.Configure<SMSSettings>(options =>
            {
                var smsSection = config.GetSection("SMS");
                options.VerificationNumber = smsSection.GetValue<string>("VerificationNumber");
                options.NotificationNumber = smsSection.GetValue<string>("NotificationNumber");
                options.ApiKey = smsSection.GetValue<string>("ApiKey");
                options.Sid = smsSection.GetValue<string>("Sid");
                options.Token = smsSection.GetValue<string>("Token");
                options.Host = smsSection.GetValue<string>("Host");
            });
            builder.Services.Configure<EmailSettings>(options =>
            {
                var emailSection = config.GetSection("Email");
                options.NotifyAddress = emailSection.GetValue<string>("NotifyAddress");
                options.LoginAddress = emailSection.GetValue<string>("LoginAddress");
                options.ApiKey = emailSection.GetValue<string>("ApiKey");
                options.FriendlyNotify = emailSection.GetValue<string>("FriendlyNotify");
                options.FriendlyLogin = emailSection.GetValue<string>("FriendlyLogin");
                options.NotificationTemplateID = emailSection.GetValue<string>("NotificationTemplateID");
                options.LoginKeyTemplateID = emailSection.GetValue<string>("LoginKeyTemplateID");
                options.Host = emailSection.GetValue<string>("Host");
            });
            builder.Services.AddScoped<ICovidDataUpdater, CovidDataUpdater>();
            builder.Services.AddScoped<ICovidNotifier, CovidNotifier>();
            builder.Services.AddSingleton<IDataService, DataService>();

            //DI Email Sender
            builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();
            //DI SMS Sender
            builder.Services.AddScoped<ISMSSender, TwillioSMSSender>();
            //DI Email & SMS Services
            builder.Services.AddScoped<EmailService, EmailService>();
            builder.Services.AddScoped<SMSService, SMSService>();
        }
    }
}