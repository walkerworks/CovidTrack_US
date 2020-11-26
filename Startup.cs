using System;
using System.Text.RegularExpressions;
using CovidTrackUS_Core.Interfaces;
using CovidTrackUS_Core.Models;
using CovidTrackUS_Core.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CovidTrackUS_Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        /// <summary>
        /// 
        /// </summary>
        public IWebHostEnvironment HostingEnvironment { get; }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
            services.AddHttpContextAccessor();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options => {
                options.LoginPath = "/api/check-login";
                options.LogoutPath = "/api/logout";
                options.ExpireTimeSpan = TimeSpan.FromSeconds(86400);
            });

            services.Configure<Settings>(options =>
            {
                options.DataConnectionString = Configuration.GetConnectionString("DefaultConnection");
            });
            services.Configure<SMSSettings>(options =>
            {
                var smsSection = Configuration.GetSection("SMS");
                options.VerificationNumber = smsSection.GetValue<string>("VerificationNumber");
                options.NotificationNumber = smsSection.GetValue<string>("NotificationNumber");
                options.ApiKey = smsSection.GetValue<string>("ApiKey");
                options.Sid = smsSection.GetValue<string>("Sid");
                options.Token = smsSection.GetValue<string>("Token");
                options.Host = smsSection.GetValue<string>("Host");
            });
            services.Configure<EmailSettings>(options =>
            {
                var emailSection = Configuration.GetSection("Email");
                options.NotifyAddress = emailSection.GetValue<string>("NotifyAddress");
                options.LoginAddress = emailSection.GetValue<string>("LoginAddress");
                options.ApiKey = emailSection.GetValue<string>("ApiKey");
                options.FriendlyNotify = emailSection.GetValue<string>("FriendlyNotify");
                options.FriendlyLogin = emailSection.GetValue<string>("FriendlyLogin");
                options.NotificationTemplateID = emailSection.GetValue<string>("NotificationTemplateID");
                options.LoginKeyTemplateID = emailSection.GetValue<string>("LoginKeyTemplateID");
                options.Host = emailSection.GetValue<string>("Host");
            });

            //DI Email Sender
            services.AddScoped<IEmailSender, SendGridEmailSender>();

            //DI SMS Sender
            services.AddScoped<ISMSSender, TwillioSMSSender>();

            //DI Email & SMS Services
            services.AddScoped<EmailService, EmailService>();
            services.AddScoped<SMSService, SMSService>();

            //DI Data Service
            services.AddSingleton<IDataService, DataService>();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowCovidTrackOrigin",
                    builder => builder
                        .SetIsOriginAllowed(origin => {
                            return Regex.IsMatch(origin,
                                @"(\.|\/\/)(covid-track\.us)(:\d*)?$",
                                RegexOptions.Compiled);
                        })
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            // Logging
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration.GetSection("Logging"));
                builder.AddConsole();
                builder.AddDebug();
            });

            // Add framework services.
            services.AddControllersWithViews()
                .AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseCors();


            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
