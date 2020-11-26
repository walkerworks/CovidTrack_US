# CovidTrack_US

## County-Level Active Covid-19 Job 
Job that runs when Johns Hopkins updates their data sets. Calculates current county-level active case estimates in the US Northeast States as measured by the [VT DFR active case methodology](https://dfr.vermont.gov/document/travel-map-methodology).
The Job that calculates this data was ported to C# .Net Core 3.1 from [Ryan Taggard's County-Status Python project](https://github.com/RyanTaggard/county-status).
Data is pulled from [Health.Data.NY.GOV](https://health.data.ny.gov/browse?tags=covid-19) and [Johns Hopkins University](https://raw.githubusercontent.com/CSSEGISandData/COVID-19/master/csse_covid_19_data/csse_covid_19_time_series/time_series_covid19_confirmed_US.csv) DataSet. Population data from [United States Census 2019](https://www2.census.gov/programs-surveys/popest/datasets/2010-2019/counties/totals/)

## Covid-19 Notification Push Application
Vue.JS SPA for gathering user's that wish to receive weekly or monthly updates to the counties in the US Northeast they are interseted in.  Users can choose to be alerted via SMS or Email (or both).  When the data is updated - 
users who have subscribed to these notifications will be sent brief status reports as to the current status of their county(ies) along with links to more in-depth graphical analysis hosted elsewhere.

### Usage
Have at it.  The data for the calculations is all open source as well as the projects to navgiate and choose county information.  
This notification implementation uses [SendGrid](https://sendgrid.com/) for transactional email and [MessageBird](https://www.messagebird.com/en/) for SMS messaging. 
