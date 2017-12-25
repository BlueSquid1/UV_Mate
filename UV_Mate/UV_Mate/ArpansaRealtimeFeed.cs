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
        public async Task<ArpansaUVResponse> GetUVData(string longitude = "138.62", string latitude = "-34.92")
        {
            HttpClient httpClient = new HttpClient();


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

        public List<UVIndex> GenerateUVIndexs()
        {
            //recommend UV indexes as outlined by WHO
            //source: http://www.who.int/uv/publications/en/UVIGuide.pdf
            List<UVIndex> UVIndexes = new List<UVIndex>();

            UVIndexes.Add(new UVIndex(0f, "Low", SKColors.Green));
            UVIndexes.Add(new UVIndex(3f, "Moderate", SKColors.Yellow));
            UVIndexes.Add(new UVIndex(6f, "High", SKColors.Orange));
            UVIndexes.Add(new UVIndex(8f, "Very High", SKColors.Red));
            UVIndexes.Add(new UVIndex(11f, "Extreme", SKColors.Purple));

            return UVIndexes;
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

