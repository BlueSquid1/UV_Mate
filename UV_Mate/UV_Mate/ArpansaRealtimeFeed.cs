using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkiaSharp;

namespace UV_Mate
{
    public class ArpansaRealtimeFeed
    {
        private HttpClient httpClient;

        public ArpansaRealtimeFeed()
        {
            this.httpClient = new HttpClient();
        }
        public async Task<ArpansaUVResponse> GetUVData(string longitude, string latitude)
        {
            DateTime todayDate = DateTime.Now.Date;
            string dateString = todayDate.Year.ToString() + "-" + todayDate.Month.ToString() + "-" + todayDate.Day.ToString();

            string url = "https://uvdata.arpansa.gov.au/api/uvlevel/";
            string getParameters = "longitude=" + longitude + "&latitude=" + latitude + "&date=" + dateString;

            string completeUrl = url + "?" + getParameters;

            HttpResponseMessage httpResponse = await httpClient.GetAsync(completeUrl, HttpCompletionOption.ResponseContentRead);

            string serverResponse = await httpResponse.Content.ReadAsStringAsync();

            ArpansaUVResponse arpansaUV = JsonConvert.DeserializeObject<ArpansaUVResponse>(serverResponse);
            return arpansaUV;
        }

        public async Task<ArpansaLocationResponse> GetValidLocations()
        {
            string urlToSensorSites = "https://uvdata.arpansa.gov.au/api/categoriesSites";

            HttpResponseMessage httpResponse = await this.httpClient.GetAsync(urlToSensorSites, HttpCompletionOption.ResponseContentRead);

            string serverResponse = await httpResponse.Content.ReadAsStringAsync();

            ArpansaLocationResponse arpansaUV = JsonConvert.DeserializeObject<ArpansaLocationResponse>(serverResponse);

            return arpansaUV;
        }
    }
}

public class UVIndex
{
    public float LowerValue { get; set; }
    public string DetailText { get; set; }
    public SKColor Colour { get; set; }

    public UVIndex(float mLowerValue, string mDetailText, SKColor mColour)
    {
        this.LowerValue = mLowerValue;
        this.DetailText = mDetailText;
        this.Colour = mColour;
    }
}


public class ArpansaUVResponse
{
    public string id { get; set; }
    public GraphData[] GraphData { get; set; }
    public TableData[] TableData { get; set; }
    public string CurrentDateTime { get; set; }
    public string CurrentUVIndex { get; set; }
    public string MaximumUVLevel { get; set; }
    public string MaximumUVLevelDateTime { get; set; }
}

public class GraphData
{
    public string id { get; set; }
    public string Date { get; set; }
    public float? Forecast { get; set; }
    public float? Measured { get; set; }
}

public class TableData
{
    public string id { get; set; }
    public string Date { get; set; }
    public string Forecast { get; set; }
    public string Measured { get; set; }
}




public class ArpansaLocationResponse
{
    public Location[] locations { get; set; }
}

public class Location
{
    public string id { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public bool CategoryEnabled { get; set; }
    public bool CategoryIsDefault { get; set; }
    public int CategorySortOrder { get; set; }
    public string SiteIdentifier { get; set; }
    public string SiteName { get; set; }
    public float SiteLatitude { get; set; }
    public float SiteLongitude { get; set; }
    public bool SiteEnabled { get; set; }
    public DateTime SiteStartDate { get; set; }
    public DateTime SiteEndDate { get; set; }
    public bool SiteIsDefault { get; set; }
    public int SiteTimeZoneOffset { get; set; }
}
