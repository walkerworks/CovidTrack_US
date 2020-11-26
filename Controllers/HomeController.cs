using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CovidTrackUS_Web.Models;
using Newtonsoft.Json;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Options;
using CovidTrackUS_Core.Services;
using System.Threading.Tasks;
using CovidTrackUS_Core.Models.Data;
using Microsoft.AspNetCore.Identity;
using CovidTrackUS_Core.Interfaces;
using Dapper;

namespace CovidTrackUS_Web.Controllers
{
    public class HomeController : Controller
    {

        private readonly IWebHostEnvironment _env;
        private readonly IDataService _dataService;
        public HomeController(IDataService dataService, IWebHostEnvironment webHostEnvironment)
        {
            _dataService = dataService;
            _env = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var appName = "CovidTrack_US";
            ViewData["clientName"] = appName;
            ViewData["appName"] = appName;
            ViewData["title"] = $"{appName}";
            ViewData["backgroundColor"] = "#EFF3F4";
            ViewData["color"] = "rgba(0, 0, 0, 0.6)";
            Subscriber covidTrackUser = null;
            if (!string.IsNullOrEmpty(HttpContext?.User?.Identity?.Name))
            {
                DynamicParameters parameters = new DynamicParameters();
                parameters.Add("@handle", HttpContext.User.Identity.Name);
                covidTrackUser = await _dataService.QueryFirstOrDefaultAsync<Subscriber>("select * from Subscriber where handle = @handle", parameters);
            }
            if (covidTrackUser != null)
            {
                var paredDownUser = new { Id = covidTrackUser.ID, handle = covidTrackUser.Handle, confirmed = covidTrackUser.Verified, counties = "" };
                ViewData["covidTrackUser"] = new HtmlString(JsonConvert.SerializeObject(paredDownUser));
            }

            var appFiles = GetAppFiles();
            if (appFiles.AppCss != null)
            {
                ViewData["appCssFile"] = $"/track/{appFiles.AppCss.Name}";
            }

            if (appFiles.AppJs != null)
            {
                ViewData["appJsFile"] = $"/track/{appFiles.AppJs.Name}";
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [Route("/manifest.json")]
        [Produces("application/json")]
        public IActionResult Manifest(string display = "standalone")
        {
            switch (display)
            {
                case "fullscreen":
                case "standalone":
                case "minimal-ui":
                case "browser":
                    break;
                default:
                    display = "standalone";
                    break;
            }

            var name = "CovidTrack_US";
            var segments = Request.Path.Value.Split('/');
            var startUrl = string.Join('/', segments.Take(segments.Length - 1));

            Response.ContentType = "application/manifest+json";
            return new JsonResult(new
            {
                name,
                short_name = name,
                theme_color = "#2196f3",
                background_color = "#000000",
                start_url = $"{startUrl}/",
                scope = $"{startUrl}/",
                orientation = "portrait",
                display,
                icons = new object[] {
                    new {
                        src = "/android-chrome-192x192.png",
                        sizes = "192x192",
                        type = "image/png"
                    },
                    new {
                        src = "/android-chrome-512x512.png",
                        sizes = "512x512",
                        type = "image/png"
                    }
                 }
            },
            new JsonSerializerSettings { Formatting = Formatting.Indented });
        }

        [HttpGet]
        [Route("precache-manifest.js")]
        [Produces("application/javascript")]
        public IActionResult PrecacheManifest()
        {
            (string url, DateTime revision) urlWithRevision(string path)
            {
                return (path, _env.ContentRootFileProvider.GetFileInfo($"wwwroot{path}").LastModified.UtcDateTime);
            }

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var assemblyRevision = System.IO.File.GetLastWriteTime(assembly.Location).ToUniversalTime();
            var viewsRevision = System.IO.File.GetLastWriteTime(Path.ChangeExtension(assembly.Location, ".Views.dll")).ToUniversalTime();
            var appFilesRevision = GetAppFiles().LastModified.UtcDateTime;

            var files = new List<(string url, DateTime revision)>
            {
                ("./", viewsRevision > appFilesRevision ? viewsRevision : appFilesRevision),
                ("manifest.json", assemblyRevision),
                urlWithRevision("/favicon.ico"),
                urlWithRevision("/android-chrome-192x192.png"),
                urlWithRevision("/android-chrome-512x512.png"),
                urlWithRevision("/images/logo/GSALogo24x25.png"),
            };

            var log = "logForSvcWorker('precache-manifest.js loaded')";
            var json = JsonConvert.SerializeObject(files.Select(i => new { i.url, revision = i.revision.ToString("o") }), Formatting.Indented);
            var result = new ContentResult
            {
                Content = $"{log};\nself.__precacheManifest = {json};",
                ContentType = "application/javascript"
            };

            return result;
        }

        private class AppFiles
        {
            public string AppRoot { get; set; }
            public IDirectoryContents DirectoryContents { get; set; }
            public IFileInfo AppJs { get; set; }
            public IFileInfo AppCss { get; set; }
            public DateTimeOffset LastModified { get; set; }
        }

        private AppFiles GetAppFiles()
        {
            var appRoot = Path.Combine("wwwroot", "track");
            var items = _env.ContentRootFileProvider.GetDirectoryContents(appRoot);
            var appCssFile = items.Where(i => Regex.IsMatch(i.Name, @"^app(\.[^.]+)?\.css$")).OrderByDescending(i => i.LastModified).FirstOrDefault();
            var appJsFile = items.Where(i => Regex.IsMatch(i.Name, @"^app(\.[^.]+)?\.js$")).OrderByDescending(i => i.LastModified).FirstOrDefault();
            DateTimeOffset lastModified = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            if (items.Any())
            {
                lastModified = items.Max(i => i.LastModified);
            }

            return new AppFiles
            {
                AppRoot = appRoot,
                DirectoryContents = items,
                AppCss = appCssFile,
                AppJs = appJsFile,
                LastModified = lastModified
            };
        }
    }
}
