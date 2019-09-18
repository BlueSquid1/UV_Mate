using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms.Xaml;
using Xamarin.Essentials;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace UV_Mate
{
    public partial class GraphPage : ContentPage
    {
        public bool IsAppearing { get; set; } = true;

        private ArpansaViewModel arpansaModel;
        private ArpansaRealtimeFeed arpansaService;

        private UVPlotter uvGraph;

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

                    if (this.arpansaModel.LocIndexValue == null)
                    {
                        //model has not been set

                        //populate a list of all measured locations
                        this.arpansaModel.MeasureLocations = await this.arpansaService.GetValidLocations();

                        //populate current location
                        //see if their is a stored preference
                        int storedIndex = Preferences.Get("LocIndexValue", -1);
                        if (storedIndex >= 0)
                        {
                            this.arpansaModel.LocIndexValue = storedIndex;
                        }
                        //try to override with GPS location instead if avaliable
                        int? selectedIndex = await this.FindClosestStation(this.arpansaModel.MeasureLocations);
                        if (selectedIndex != null)
                        {
                            this.arpansaModel.LocIndexValue = selectedIndex.Value;
                        }
                    }

                    await this.UpdateGraph(sender, e);
                }
            }
            catch (Exception e1)
            {
                await DisplayAlert("Error", e1.Message, "Ok");
            }
        }

        private async Task<int?> FindClosestStation(List<MeasuredLocation> locations)
        {
            //FindIndex defaults to -1 so it makes sense to start with -1
            int? siteIndex = -1;
            try
            {
                await this.DisplayReasonForLocation();
                Location gpsPosition = await Geolocation.GetLocationAsync();

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
                //failed to find a location. Return null
                siteIndex = null;
                if (!(e1 is PermissionException))
                {
                    await DisplayAlert("Error", e1.Message, "Ok");
                }

            }
            return siteIndex;
        }

        private async Task DisplayReasonForLocation()
        {
            bool haveAsked = Preferences.Get("haveAskedForLocation", false);
            if (haveAsked == false)
            {
                await DisplayAlert("Location Permission", "This app will request for your location which is used to find the closest UV station. If you would prefer not to share your location then you can can select the closest UV station manually as an alternative.", "Ok");
                Preferences.Set("haveAskedForLocation", true);
            }
        }

        private async Task UpdateGraph(object sender, EventArgs e)
        {
            try
            {
                //fetch latest UV data from service and then update model
                int? LocIndex = this.arpansaModel.LocIndexValue;
                if (LocIndex == null)
                {
                    //no location has been selected. Nothing to paint.
                    return;
                }
                MeasuredLocation curLocation = this.arpansaModel.MeasureLocations[LocIndex.Value];
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
