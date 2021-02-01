using CovidTrackUS_Core.Enums;
using Dapper.Contrib.Extensions;
using System;

namespace CovidTrackUS_Core.Models.Data
{
    /// <summary>
    /// County domain object. Represents one of the nearly 3,200 US Counties in the 
    /// United States.  Contains population data as well as the daily active COVID-19 cases for the past week.
    /// </summary>
    [Table("County")]
    public class County : CovidTrackDO
    {
        /// <summary>
        /// Federal Information Processing Standards code for the US County
        /// </summary>
        public string FIPS { get; set; }
        /// <summary>
        /// The County name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The US State this county belongs to
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// The total confirmed cases yesterday
        /// </summary>
        /// <remarks>
        /// This is actually data from 2 days ago.
        /// </remarks>
        public double? ConfirmedCasesYesterday { get; set; }

        /// <summary>
        /// The total confimed cases for this county.  This is a 
        /// constantly increasing number day over day.
        /// </summary>
        /// <remarks>
        /// This is actually data from yesterday.  We don't have today's data yet.
        /// </remarks>
        public double? ConfirmedCases { get; set; }

        /// <summary>
        /// The total confimed cases for this county last week.
        /// </summary>
        /// <remarks>
        /// This is actually data from a week ago, yesterday.
        /// </remarks>
        public double? ConfirmedCasesLastWeek { get; set; }

        /// <summary>
        /// The active cases as of today
        /// </summary>
        /// <remarks>
        /// This is actually data from yesterday.  We don't have today's data yet.
        /// </remarks>
        public double? ActiveCases { get; set; }

        /// <summary>
        /// The active cases yesterday
        /// </summary>
        /// <remarks>
        /// This is actually data from 2 days ago.
        /// </remarks>
        public double? ActiveCasesYesterday { get; set; }

        /// <summary>
        /// The active cases as of a week ago today
        /// </summary>
        /// <remarks>
        /// This is actually data from a week ago, yesterday.
        /// </remarks>
        public double? ActiveCasesLastWeek { get; set; }


        /// <summary>
        /// US Census Data provided population count for this county
        /// </summary>
        public int Population { get; set; }
        /// <summary>
        /// The last time the <see cref="DailyActiveCaseArray"/> was updated
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// The <see cref="CountyStatus"/> as of right now
        /// </summary>
        [Computed]
        public CountyStatus StatusToday
        {
            get
            {
                switch (ActiveCasesTodayPerMillion)
                {
                    case var actpm when (actpm < 400):
                        return CountyStatus.GREEN;
                    case var actpm when (actpm >= 400 && actpm < 800):
                        return CountyStatus.YELLOW;
                    case var actpm when (actpm >= 800):
                    default:
                        return CountyStatus.RED;
                }
            }
        }

        /// <summary>
        /// Active cases per million on today's date (EST)
        /// </summary>
        [Computed]
        public double? ActiveCasesTodayPerMillion
        {
            get
            {
                if (ActiveCases.HasValue && Population != 0)
                {
                    return ActiveCases.Value / Population * 1000000;
                }
                return null;
            }
        }

        /// <summary>
        /// Confirmed case percentage change over the past week (EST)
        /// </summary>
        [Computed]
        public double? ConfirmedPastWeekPercentChange
        {
            get
            {
                if (!ConfirmedCases.HasValue || !ConfirmedCasesLastWeek.HasValue || ConfirmedCasesLastWeek == 0)
                {
                    return null;
                }
                return ((ConfirmedCases - ConfirmedCasesLastWeek) / ConfirmedCasesLastWeek) * 100;
            }
        }

        /// <summary>
        /// Confirmed case percentage change from yesterday
        /// </summary>
        [Computed]
        public double? ConfirmedYesterdayPercentChange
        {
            get
            {
                if (!ConfirmedCases.HasValue || !ConfirmedCasesYesterday.HasValue || ConfirmedCasesYesterday == 0)
                {
                    return null;
                }
                return ((ConfirmedCases - ConfirmedCasesYesterday) / ConfirmedCasesYesterday) * 100;
            }
        }



        /// <summary>
        /// Active case percentage change over the past week (EST)
        /// </summary>
        [Computed]
        public double? ActivePastWeekPercentChange
        {
            get
            {
                if (!ActiveCases.HasValue || !ActiveCasesLastWeek.HasValue || ActiveCasesLastWeek == 0)
                {
                    return null;
                }
                return ((ActiveCases - ActiveCasesLastWeek) / ActiveCasesLastWeek) * 100;
            }
        }

        /// <summary>
        /// Active case percentage change from yesterday
        /// </summary>
        [Computed]
        public double? ActiveYesterdayPercentChange
        {
            get
            {
                if (!ActiveCases.HasValue || !ActiveCasesYesterday.HasValue || ActiveCasesYesterday == 0)
                {
                    return null;
                }
                return ((ActiveCases - ActiveCasesYesterday) / ActiveCasesYesterday) * 100;
            }
        }

        /// <summary>
        /// Readable blurb as to whether cases have increased, decreased or remainined the same
        /// </summary>
        public static string IncreaseOrDecreaseBlurb(double? val)
        {
            if (!val.HasValue)
                return "";
            var displayVal = Math.Round(val.Value);
            if (displayVal == 0)
                return "0%";

            return $"{(val.Value > 0 ? "+" : "-")}{displayVal:N0}%";
        }

        /// <summary>
        /// Can't believe I wrote this.  ಠ_ಠ
        /// </summary>
        [Computed]
        public string StateAbbreviation
        {
            get
            {
                switch (State.ToUpper())
                {
                    case "ALABAMA":
                        return "AL";
                    case "ALASKA":
                        return "AK";
                    case "ARIZONA":
                        return "AZ";
                    case "ARKANSAS":
                        return "CR";
                    case "CALIFORNIA":
                        return "CA";
                    case "COLORADO":
                        return "CO";
                    case "CONNECTICUT":
                        return "CT";
                    case "DELAWARE":
                        return "DE";
                    case "DISTRICT OF COLUMBIA":
                        return "DC";
                    case "FLORIDA":
                        return "FL";
                    case "GEORGIA":
                        return "GA";
                    case "HAWAII":
                        return "HI";
                    case "IDAHO":
                        return "ID";
                    case "ILLINOIS":
                        return "IL";
                    case "INDIANA":
                        return "IN";
                    case "IOWA":
                        return "IA";
                    case "KANSAS":
                        return "KS";
                    case "KENTUCKY":
                        return "KY";
                    case "LOUISIANA":
                        return "LA";
                    case "MAINE":
                        return "ME";
                    case "MARYLAND":
                        return "MD";
                    case "MASSACHUSETTS":
                        return "MA";
                    case "MICHIGAN":
                        return "MI";
                    case "MINNESOTA":
                        return "MN";
                    case "MISSISSIPPI":
                        return "MS";
                    case "MISSOURI":
                        return "MO";
                    case "MONTANA":
                        return "MT";
                    case "NEBRASKA":
                        return "NE";
                    case "NEVADA":
                        return "NV";
                    case "NEW HAMPSHIRE":
                        return "NH";
                    case "NEW JERSEY":
                        return "NJ";
                    case "NEW MEXICO":
                        return "NM";
                    case "NEW YORK":
                        return "NY";
                    case "NORTH CAROLINA":
                        return "NC";
                    case "NORTH DAKOTA":
                        return "ND";
                    case "OHIO":
                        return "OH";
                    case "OKLAHOMA":
                        return "OK";
                    case "OREGON":
                        return "OR";
                    case "PENNSYLVANIA":
                        return "PA";
                    case "RHODE ISLAND":
                        return "RI";
                    case "SOUTH CAROLINA":
                        return "SC";
                    case "SOUTH DAKOTA":
                        return "SD";
                    case "TENNESSEE":
                        return "TN";
                    case "TEXAS":
                        return "TX";
                    case "UTAH":
                        return "UT";
                    case "VERMONT":
                        return "VT";
                    case "VIRGINIA":
                        return "VA";
                    case "WASHINGTON":
                        return "WA";
                    case "WEST VIRGINIA":
                        return "WV";
                    case "WISCONSIN":
                        return "WI";
                    case "WYOMING":
                        return "WY";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
