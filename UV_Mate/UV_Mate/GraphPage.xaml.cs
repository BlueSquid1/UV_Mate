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

            this.BindingContext = this.arpansaModel;
        }

        private async void GraphPage_Appearing(object sender, EventArgs e)
        {
            //on android the appear event is called twice.
            //this is a bug/feature (what's the difference anyway) that can be traced back to global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
            if (this.IsAppearing == true)
            {
                IsAppearing = false;
                await this.UpdateGraph(sender, e);
            }
        }

        private async Task UpdateGraph(object sender, EventArgs e)
        {
            try
            {
                //check if location has been selected 
                if (this.arpansaModel.LocIndexValue == -1)
                {
                    if (!CrossGeolocator.IsSupported)
                    {
                        await DisplayAlert("Warning", "GPS location is not enabled on this device", "OK");
                        return;
                    }
                    Position gpsPosition = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(10));

                    float latitude = (float)gpsPosition.Latitude;
                    float longitude = (float)gpsPosition.Longitude;

                    //look up closest location
                    ClosestLocResponse closestLocResponse = await this.arpansaService.GetClosestArpansaLocation(longitude, latitude);

                    //get a list of all measured locations
                    this.arpansaModel.MeasureLocations = await this.arpansaService.GetValidLocations();

                    //find selected location
                    this.arpansaModel.LocIndexValue = arpansaModel.MeasureLocations.FindIndex((MeasuredLocation curLoc) =>
                    {
                        return curLoc.SiteLatitude == closestLocResponse.Latitude && curLoc.SiteLongitude == closestLocResponse.Longitude;
                    });

                    if (this.arpansaModel.LocIndexValue == -1)
                    {
                        throw new Exception("failed to match closes location with the ARPANSA locations");
                    }
                }

                //fetch latest UV data from service and then update model
                MeasuredLocation curLocation = this.arpansaModel.MeasureLocations[this.arpansaModel.LocIndexValue];
                CurrentLocName.Text = curLocation.SiteName;

                ArpansaUVResponse arpansaUV = await this.arpansaService.GetUVData(curLocation.SiteLongitude, curLocation.SiteLatitude);
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
