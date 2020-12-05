using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CovidTrackUS_Core.Interfaces;
using CovidTrackUS_Core.Models;
using CovidTrackUS_Core.Models.Data;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
namespace CovidTrackUS_Core.Services
{
    /// <summary>
    /// C# Rewrite of Ryan Taggard's DFR Travel Map Code python script.
    /// https://raw.githubusercontent.com/RyanTaggard/county-status/master/DFR%20Travel%20Map%20Code.py
    /// </summary>
    public class CovidDataUpdater : ICovidDataUpdater
    {
        private readonly IDataService _dataService;
        /// <summary>
        /// Constructor for dependency injection
        /// </summary>
        /// <param name="dataService">DI Database service</param>
        public CovidDataUpdater(IDataService dataService)
        {
            _dataService = dataService;
        }

        private const string JHUDataURI = "https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_US.csv";
        private const string NYHealthDataURI = "https://health.data.ny.gov/resource/xdss-u53e.json";
        private readonly double factor = 2.4;

        /* Values obtained from our gamma distribution equal to
        1 - CDF for the first 30 integer values of x */
        private double[] gamma = new[] {
                            1, 0.94594234, 0.8585381, 0.76322904, 0.66938185,
                            0.58139261, 0.50124929, 0.42963663, 0.36651186, 0.31143254,
                            0.26375154, 0.22273485, 0.18763259, 0.15772068, 0.1323241,
                            0.11082822, 0.09268291, 0.077402, 0.06456005, 0.0537877,
                            0.04476636, 0.03722264, 0.03092299, 0.02566868, 0.02129114,
                            0.0176478, 0.01461838, 0.01210161, 0.01001242, 0.00827947};

        Dictionary<string, string> _NYCFIPS;
        private Dictionary<string, string> NYCFIPS
        {
            get
            {
                if(_NYCFIPS == null)
                {
                    _NYCFIPS = new Dictionary<string, string>();
                    _NYCFIPS.Add("Bronx", "36005");
                    _NYCFIPS.Add("Kings", "36047");
                    _NYCFIPS.Add("New York", "36061");
                    _NYCFIPS.Add("Queens", "36081");
                    _NYCFIPS.Add("Richmond", "36085");
                }
                return _NYCFIPS;
            }
        }

        public async Task RetrieveAndCrunch(ILogger log)
        {
            DataTable finalData;
            using (WebClient client = new WebClient())
            {
                DataTable hopkins;
                //Download CSV from JHU
                log.LogInformation($"loading hopkins data from: {JHUDataURI}");
                Stopwatch sw = new Stopwatch();
                sw.Start();
                var data = await client.DownloadDataTaskAsync(new Uri(JHUDataURI));
                using (MemoryStream ms = new MemoryStream(data))
                {
                    using (StreamReader sr = new StreamReader(ms))
                    {
                        hopkins = new DataTable();
                        using (var csvReader = new CsvReader(sr, true))
                        {
                            hopkins.Load(csvReader);
                        }
                    }
                }
                sw.Stop();
                log.LogInformation($"Downloaded {hopkins.Rows.Count} Rows in {sw.ElapsedMilliseconds} ms");

                // Fixing FIPS in JHU DataTable
                log.LogInformation("Fixing FIPS");
                var filteredRows = hopkins.AsEnumerable();
                hopkins.Columns["FIPS"].ReadOnly = false;
                foreach (var row in filteredRows) 
                {
                    if (row.IsNull("FIPS"))
                    {
                        row.SetField("FIPS", row.Field<string>("UID").Substring(3));
                    }
                    else
                    {
                        var vals = row.Field<string>("FIPS").Split('.'); // if there's a decimal
                        row.SetField("FIPS", vals[0].PadLeft(5,'0')); // pad left with zeros if less than 5
                    }
                }
                finalData = filteredRows.CopyToDataTable();
                log.LogInformation("FIPS Fixed");
 

                // The most recent date column in the table (it's the last column) for calculating (yesterday's) active cases
                var ix_Yesterday = finalData.Columns.Count - 1;
                // 30 columns back (for 30 days of dates)
                var ix_30DaysAgo = ix_Yesterday - 29;
                // Last week from yesterday's column in the table for calculating last week's active cases
                var ix_lastWeek = finalData.Columns.Count - 8;
                // 30 columns back from last week's index (for 30 days of dates) for calculating last week's active cases
                var ix_30DaysFromLastWeek = ix_lastWeek - 29;

                /* Hang on to the dates for hitting the NYC Data API */
                /* Active Cases This week Dates */
                var casesAMonthAgo = DateTime.Parse(finalData.Columns[ix_30DaysAgo].ColumnName).Date;
                var casesYesterday = DateTime.Parse(finalData.Columns[ix_Yesterday].ColumnName).Date.AddDays(1).AddSeconds(-1);
                /* Active Cases Last week Dates */
                var casesAMonthAgoLastWeek = DateTime.Parse(finalData.Columns[ix_30DaysFromLastWeek].ColumnName).Date;
                var casesLastWeek = DateTime.Parse(finalData.Columns[ix_lastWeek].ColumnName).Date.AddDays(1).AddSeconds(-1);


                log.LogInformation($"Calculating between dates: {casesAMonthAgo.ToShortDateString()} and {casesYesterday.ToShortDateString()}");

                /* Add active cases column to hopkins data */
                finalData.Columns.Add("Active Cases",typeof(double));
                /* Add last week active cases column to hopkins data */
                finalData.Columns.Add("Last Week Active Cases", typeof(double));

                /* Variables to be used in row/column interating */
                int gammaIndex;
                double countySum;

                log.LogInformation($"Calculating Active Cases (non-NYC)");
                /* Loop through each county row  */
                for (int i = 0; i < finalData.Rows.Count; i++)
                {
                    // Reset controls for this county
                    gammaIndex = 29;
                    countySum = 0;
                    /* Iterate through the 30 recent dates we're analyzing for this county, oldest to newest */
                    for (int j = ix_30DaysAgo; j <= ix_Yesterday; j++)
                    {
                        /* Calc active case difference from the day before */
                        var prevDay = int.Parse(finalData.Rows[i].Field<string>(j - 1));
                        var currDay = int.Parse(finalData.Rows[i].Field<string>(j));
                        var diff = currDay - prevDay;
                        /* Multiply by the proper gamma val and add to the county active case sum */
                        countySum += (diff * gamma[gammaIndex]);
                        gammaIndex--;
                    }
                    // update the Active cases column for the county row (multiple sum by factor)
                    finalData.Rows[i].SetField("Active Cases", (countySum < 0 ? 0 : countySum) * factor);
                }
                log.LogInformation($"- done...");
                log.LogInformation($"Calculating Last Week's Active Cases (non-NYC)");
                /* Loop through each county row  */
                for (int i = 0; i < finalData.Rows.Count; i++)
                {
                    // Reset controls for this county
                    gammaIndex = 29;
                    countySum = 0;
                    /* Iterate through the 30 recent dates we're analyzing for this county, oldest to newest */
                    for (int j = ix_30DaysFromLastWeek; j <= ix_lastWeek; j++)
                    {
                        /* Calc active case difference from the day before */
                        var prevDay = int.Parse(finalData.Rows[i].Field<string>(j - 1));
                        var currDay = int.Parse(finalData.Rows[i].Field<string>(j));
                        var diff = currDay - prevDay;
                        /* Multiply by the proper gamma val and add to the county active case sum */
                        countySum += (diff * gamma[gammaIndex]);
                        gammaIndex--;
                    }
                    // update the Last Week Active Cases column for the county row (multiple sum by factor)
                    finalData.Rows[i].SetField("Last Week Active Cases", (countySum < 0 ? 0 : countySum) * factor);
                }
                log.LogInformation($"- done...");
                // Keep only the columns we need
                var filterColumns = new[] { "FIPS", "Active Cases", "Last Week Active Cases" };
                for (int i = finalData.Columns.Count - 1; i >= 0; i--)
                {
                    if (!filterColumns.Contains(finalData.Columns[i].ColumnName))
                    {
                        finalData.Columns.RemoveAt(i);
                    }
                }

                // Make FIPS Column the primary lookup key
                finalData.PrimaryKey = new[] { finalData.Columns["FIPS"] };

                log.LogInformation($"Downloading Active Case NYC Data from {NYHealthDataURI}");
                sw.Restart();
                //Download Borough Data in JSON from City of New York with soQL query
                string soQL = @$"query=SELECT test_date,county,new_positives WHERE 
                    (county='Bronx' OR county='Richmond' OR county='New York' OR county='Kings' OR county='Queens') 
                    AND 
                    (test_date between '{casesAMonthAgo.ToString("s", System.Globalization.CultureInfo.InvariantCulture)}' and '{casesYesterday.ToString("s", System.Globalization.CultureInfo.InvariantCulture)}')";
                var nyData = await client.DownloadStringTaskAsync(new Uri($"{NYHealthDataURI}?${soQL}"));
                sw.Stop();
                log.LogInformation($"Downloaded {nyData.Length} Rows in {sw.ElapsedMilliseconds} ms");
                // Convert to objects from JSON
                NYCCovidData[] typedNYData = (NYCCovidData[])JsonConvert.DeserializeObject(nyData, typeof(NYCCovidData[]));
                log.LogInformation($"Calculating Active Cases (NYC)");
                IterateOverBoroughData(typedNYData, finalData, "Active Cases");
                log.LogInformation($"- done...");

                log.LogInformation($"Downloading Last Week's Active Case NYC Data from {NYHealthDataURI}");
                sw.Restart();
                //Download Borough Data in JSON from City of New York with soQL query
                soQL = @$"query=SELECT test_date,county,new_positives WHERE 
                    (county='Bronx' OR county='Richmond' OR county='New York' OR county='Kings' OR county='Queens') 
                    AND 
                    (test_date between '{casesAMonthAgoLastWeek.ToString("s", System.Globalization.CultureInfo.InvariantCulture)}' and '{casesLastWeek.ToString("s", System.Globalization.CultureInfo.InvariantCulture)}')";
                nyData = await client.DownloadStringTaskAsync(new Uri($"{NYHealthDataURI}?${soQL}"));
                sw.Stop();
                log.LogInformation($"Downloaded {nyData.Length} Rows in {sw.ElapsedMilliseconds} ms");
                // Convert to objects from JSON
                typedNYData = (NYCCovidData[])JsonConvert.DeserializeObject(nyData, typeof(NYCCovidData[]));
                log.LogInformation($"Calculating Last Week's Active Cases (NYC)");
                IterateOverBoroughData(typedNYData, finalData, "Last Week Active Cases");
                log.LogInformation($"- done...");

                var batchUpdateTime = DateTime.Now;
                // Get the Counties in the DB
                var CountyDataLookup = (await _dataService.FindAsync<County>("Select * from County")).ToDictionary(l => l.FIPS);
                // Iterate through our final data and update the county information in the database
                County countyToUpdate;
                log.LogInformation($"Updating County rows in database with calculations");
                sw.Restart();
                for (int i = 0; i < finalData.Rows.Count; i++)
                {
                    //Find DB County by FIPS
                    if (CountyDataLookup.TryGetValue(finalData.Rows[i].Field<string>("FIPS"), out countyToUpdate)) {
                        // Update Active Cases Data
                        if (countyToUpdate != null)
                        {
                            countyToUpdate.ActiveCasesYesterday = countyToUpdate.ActiveCases;
                            countyToUpdate.ActiveCases = finalData.Rows[i].Field<double>("Active Cases");
                            countyToUpdate.ActiveCasesLastWeek = finalData.Rows[i].Field<double>("Last Week Active Cases");
                            countyToUpdate.LastUpdated = DateTime.Now;
                        }
                        // Update DB
                        await _dataService.ExecuteUpdateAsync(countyToUpdate);
                    }
                }
                sw.Stop();
                log.LogInformation($"Finished in {sw.ElapsedMilliseconds} ms");
            }
        }
        private void IterateOverBoroughData(NYCCovidData[] typedNYData, DataTable finalData, string columnToUpdate)
        {
            //Iterate over Borough data grouped by County and do the same calculations as before (gamma and factor)
            foreach (var grouping in typedNYData.GroupBy(d => d.county))
            {
                // Reset controls for this county
                var gammaIndex = 29;
                double countySum = 0;
                var prevDay = -1;
                foreach (var group in grouping)
                {
                    if (prevDay >= 0)
                    {
                        var currDay = group.new_positives;
                        var diff = currDay - prevDay;
                        /* Multiply by the proper gamma val and add to the county active case sum */
                        countySum += (diff * gamma[gammaIndex]);
                        gammaIndex--;
                    }
                    /* Set prevDay value to this for the next iteration */
                    prevDay = group.new_positives;
                }
                var updateRow = finalData.Rows.Find(NYCFIPS[grouping.Key]);
                updateRow.SetField<double>(columnToUpdate, countySum * factor);
            }
        }

    }
}
