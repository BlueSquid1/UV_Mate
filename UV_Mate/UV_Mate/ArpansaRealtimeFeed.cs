using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkiaSharp;
using System.Linq;

namespace UV_Mate
{
    public class ArpansaRealtimeFeed
    {
        private HttpClient httpClient;

        public ArpansaRealtimeFeed()
        {
            this.httpClient = new HttpClient();
        }
        public async Task<ArpansaUVResponse> GetUVData(float longitude, float latitude)
        {
            DateTime todayDate = DateTime.Now.Date;
            string dateString = todayDate.Year.ToString() + "-" + todayDate.Month.ToString() + "-" + todayDate.Day.ToString();

            string url = "https://uvdata.arpansa.gov.au/api/uvlevel/";
            string getParameters = "longitude=" + longitude.ToString() + "&latitude=" + latitude.ToString() + "&date=" + dateString;
            string completeUrl = url + "?" + getParameters;

            HttpResponseMessage httpResponse = await this.httpClient.GetAsync(completeUrl, HttpCompletionOption.ResponseContentRead);
            string serverResponse = await httpResponse.Content.ReadAsStringAsync();
            ArpansaUVResponse arpansaUV = JsonConvert.DeserializeObject<ArpansaUVResponse>(serverResponse);
            return arpansaUV;
        }

        public async Task<List<MeasuredLocation>> GetValidLocations()
        {
            string urlToSensorSites = "https://uvdata.arpansa.gov.au/api/categoriesSites";

            HttpResponseMessage httpResponse = await this.httpClient.GetAsync(urlToSensorSites, HttpCompletionOption.ResponseContentRead);
            string serverResponse = await httpResponse.Content.ReadAsStringAsync();
            List<MeasuredLocation> arpansaUV = JsonConvert.DeserializeObject<List<MeasuredLocation>>(serverResponse);

            //remove whitespace from location names
            for(int i = 0; i < arpansaUV.Count; i++)
            {
                arpansaUV[i].SiteName = arpansaUV[i].SiteName.Trim();
                arpansaUV[i].CategoryName = arpansaUV[i].CategoryName.Trim();
            }

            arpansaUV = arpansaUV.OrderBy(o => o.SiteName).ToList();

            return arpansaUV;
        }

        public List<UVIndex> GenerateUVIndexs()
        {
            //recommend UV indexes as outlined by WHO
            //source: http://www.who.int/uv/publications/en/UVIGuide.pdf
            List<UVIndex> UVIndexes = new List<UVIndex>();

            UVIndexes.Add(new UVIndex(0f, "Low", SKColors.Green));
            UVIndexes.Add(new UVIndex(3f, "Moderate", new SKColor(190, 190, 25))); //yellow
            UVIndexes.Add(new UVIndex(6f, "High", SKColors.Orange));
            UVIndexes.Add(new UVIndex(8f, "Very High", SKColors.Red));
            UVIndexes.Add(new UVIndex(11f, "Extreme", SKColors.Purple));

            return UVIndexes;
        }

        public async Task<ClosestLocResponse> GetClosestArpansaLocation(float longitude, float latitude)
        {
            string url = "https://uvdata.arpansa.gov.au/api/closestLocation/";
            string getParameters = "latitude=" + latitude.ToString() + "&longitude=" + longitude.ToString();
            string completeUrl = url + "?" + getParameters;

            HttpResponseMessage httpResponse = await this.httpClient.GetAsync(completeUrl, HttpCompletionOption.ResponseContentRead);
            string serverResponse = await httpResponse.Content.ReadAsStringAsync();
            ClosestLocResponse arpansaUV = JsonConvert.DeserializeObject<ClosestLocResponse>(serverResponse);

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


//update ArpansaUVData from ArpansaViewModel.cs if this changes
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


public class MeasuredLocation
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
    public override string ToString()
    {
        return this.SiteName;
    }
}


public class ClosestLocResponse
{
    public string id { get; set; }
    public string Identifier { get; set; }
    public string Name { get; set; }
    public float Latitude { get; set; }
    public float Longitude { get; set; }
    public bool Enabled { get; set; }
    public float Channel1_CalFactor { get; set; }
    public float Channel1_Offset { get; set; }
    public float Channel2_CalFactor { get; set; }
    public float Channel2_Offset { get; set; }
    public float Channel3_CalFactor { get; set; }
    public float Channel3_Offset { get; set; }
    public float Channel4_CalFactor { get; set; }
    public float Channel4_Offset { get; set; }
    public int UsedChannel { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public float Distance_In_Km { get; set; }
}
