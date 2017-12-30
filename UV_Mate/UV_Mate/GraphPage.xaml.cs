using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System.Windows.Input;
using Xamarin.Forms.Xaml;
using System.ComponentModel;
using System.Collections.ObjectModel;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace UV_Mate
{
    public partial class GraphPage : ContentPage
    {

        ArpansaViewModel arpansaModel;
        ArpansaRealtimeFeed arpansaService;

        UVPlotter uvGraph;

        bool isFirstTime = true;

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
            await this.UpdateGraph(sender, e);
        }

        private async Task UpdateGraph(object sender, EventArgs e)
        {
            //check if location has been selected 
            if (this.arpansaModel.LocIndexValue == -1)
            {
                //get GPS lat and long
                //use dummy data for now.
                //Choose a point near Adelaide on planet earth
                float latitude = -34.74f;
                float longitude = 138.81f;

                //look up closest location
                ClosestLocResponse closestLocResponse = await this.arpansaService.GetClosestArpansaLocation(longitude, latitude);

                //get a list of all measured locations
                this.arpansaModel.MeasureLocations = await this.arpansaService.GetValidLocations();

                //find selected location
                this.arpansaModel.LocIndexValue = arpansaModel.MeasureLocations.FindIndex((MeasuredLocation x) =>
                {
                    return x.SiteLatitude == closestLocResponse.Latitude && x.SiteLongitude == closestLocResponse.Longitude;
                });

                if (this.arpansaModel.LocIndexValue == -1)
                {
                    throw new Exception("failed to match closes location with the ARPANSA locations");
                }
            }


            //fetch latest UV data from service and then update model
            Console.WriteLine("retrieving closest location from ARPANSA");
            MeasuredLocation curLocation = this.arpansaModel.MeasureLocations[this.arpansaModel.LocIndexValue];
            CurrentLocName.Text = curLocation.SiteName;

            Console.WriteLine("retrieving UV data - crash");
            ArpansaUVResponse arpansaUV = await this.arpansaService.GetUVData(curLocation.SiteLongitude, curLocation.SiteLatitude);
            Console.WriteLine("generating UV indexes");
            List<UVIndex> uvIndexes = arpansaService.GenerateUVIndexs();

            //update model
            Console.WriteLine("Updating graph data in model");
            ArpansaUVData graphData = new ArpansaUVData(arpansaUV);
            graphData.ReferenceUVs = uvIndexes;
            this.arpansaModel.ArpansaUVData = graphData;
        }

        private void OnPaint(object sender, SKPaintGLSurfaceEventArgs e)
        {
            Console.WriteLine("Painting Graph");
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
