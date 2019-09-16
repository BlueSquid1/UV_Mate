using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms.Xaml;
using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace UV_Mate
{
    public partial class GraphPage : ContentPage
    {
        private ArpansaViewModel arpansaModel;
        private ArpansaRealtimeFeed arpansaService;

        private UVPlotter uvGraph;
        public bool IsAppearing { get; set; } = true;

        public GraphPage(ArpansaViewModel mArpansaModel, ArpansaRealtimeFeed mArpansaService)
        {
            InitializeComponent();
            this.arpansaModel = mArpansaModel;
            this.arpansaService = mArpansaService;

            this.uvGraph = new UVPlotter(this.canvasView, this.arpansaModel, new TimeSpan(6, 0, 0), new TimeSpan(20, 0, 0), new TimeSpan(3, 0, 0), 0, 13, 2, "Time of Day", "UV Level");

            //subscribe to events
            arpansaModel.ArpansaUpdateEvent += UpdateGraph;
            this.Appearing += GraphPage_Appearing;

            //update graph every minute
            Device.StartTimer(TimeSpan.FromMinutes(1), () => {
                this.TimeExpired();
                return true; 
            });

            this.BindingContext = this.arpansaModel;
        }

        private async void TimeExpired()
        {
            await this.UpdateGraph(this, null);
        }

        private async void GraphPage_Appearing(object sender, EventArgs e)
        {
            try
            {
                //on android the appear event is called twice. Use the IsAppearing variable to prevent this from running too many times.
                if (this.IsAppearing == true)
                {
                    IsAppearing = false;

                    if (this.arpansaModel.LocIndexValue < 0)
                    {
                        //get a list of all measured locations
                        this.arpansaModel.MeasureLocations = await this.arpansaService.GetValidLocations();
                        //populate current location
                        this.arpansaModel.LocIndexValue = await this.FindClosestStation(this.arpansaModel.MeasureLocations);
                    }

                    await this.UpdateGraph(sender, e);
                }
            }
            catch(Exception e1)
            {
                await DisplayAlert("Error", e1.Message, "Ok");
            }
        }

        private async Task<int> FindClosestStation(List<MeasuredLocation> locations)
        {
            int siteIndex = -1;
            try
            {
                Position gpsPosition = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(3));

                float latitude = (float)gpsPosition.Latitude;
                float longitude = (float)gpsPosition.Longitude;

                //look up closest location
                ClosestLocResponse closestLocResponse = await this.arpansaService.GetClosestArpansaLocation(longitude, latitude);

                //find selected location
                siteIndex = locations.FindIndex((MeasuredLocation curLoc) =>
                {
                    return curLoc.SiteLatitude == closestLocResponse.Latitude && curLoc.SiteLongitude == closestLocResponse.Longitude;
                });

                if (siteIndex == -1)
                {
                    throw new Exception("failed to match closes location with the ARPANSA locations.");
                }
            }
            catch (Exception e1)
            {
                //failed to find a location. Just select the first one
                siteIndex = 0;
                string errorMsg = e1.Message;
                if ( e1 is GeolocationException )
                {
                    errorMsg = "Location is used to find the closest ARPANSA station. Please manual select your location instead.";
                }
                await DisplayAlert("Error", errorMsg, "Ok");
            }
            return siteIndex;
        }

        private async Task UpdateGraph(object sender, EventArgs e)
        {
            try
            {
                //fetch latest UV data from service and then update model
                MeasuredLocation curLocation = this.arpansaModel.MeasureLocations[this.arpansaModel.LocIndexValue];
                CurrentLocName.Text = curLocation.SiteName;

                ArpansaUVResponse arpansaUV = await this.arpansaService.GetUVData(curLocation.SiteLongitude.Value, curLocation.SiteLatitude.Value);
                List<UVIndex> uvIndexes = arpansaService.GenerateUVIndexs();

                //update model
                ArpansaUVData graphData = new ArpansaUVData(arpansaUV);
                graphData.ReferenceUVs = uvIndexes;
                this.arpansaModel.ArpansaUVData = graphData;
            }
            catch( Exception e1 )
            {
                await DisplayAlert("Error", e1.Message, "Ok");
            }
        }

        private void OnPaint(object sender, SKPaintGLSurfaceEventArgs e)
        {
            //clear screen
            GRBackendRenderTargetDesc viewInfo = e.RenderTarget;
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;


            //scale from pixels to xamarin units (64 units to each centimeter)
            int skiaPixels = viewInfo.Width;
            double xamarinUnits = this.canvasView.Width;
            float scaleFactor = (float)(skiaPixels / xamarinUnits);
            canvas.Scale(scaleFactor);


            //clear screen
            canvas.Clear(SKColors.White);

            uvGraph.DrawGraph(e);
        }

        //hook methods for future use
        private void OnTapSample(object sender, EventArgs e)
        {
        }

        private void OnPanSample(object sender, PanUpdatedEventArgs e)
        {

        }

        private void OnPinchSample(object sender, PinchGestureUpdatedEventArgs e)
        {

        }
    }
}
