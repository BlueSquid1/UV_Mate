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

        UVPlotter uvGraph;
        //ArpansaRealtimeFeed arpansaService;

        List<UVIndex> uvIndexes;

        public GraphPage(ArpansaViewModel mArpansaModel)
        {
            InitializeComponent();

            this.arpansaModel = mArpansaModel;

            arpansaModel.ArpansaUpdateEvent += ArpansaModel_ArpansaUpdateEvent;

            this.BindingContext = this.arpansaModel;

            this.uvGraph = new UVPlotter(this.canvasView, new TimeSpan(6, 0, 0), new TimeSpan(20, 0, 0), new TimeSpan(3, 0, 0), 0, 13, 2, "Time of Day", "UV Level");

            this.Appearing += UpdateGraph;
        }

        private void ArpansaModel_ArpansaUpdateEvent(object sender, ArpansaUVResponse e)
        {
            RedrawGraphWithUpdates(e);
        }

        private async void UpdateGraph(object sender, EventArgs e)
        {
            ArpansaUVResponse arpansaUV = await arpansaModel.UpdateUVData();
            RedrawGraphWithUpdates(arpansaUV);
        }

        private void RedrawGraphWithUpdates(ArpansaUVResponse arpansaData)
        {
            if(this.uvIndexes == null)
            {
                this.uvIndexes = arpansaModel.GenerateUVIndexs();
            }

            uvGraph.SetPlotPoints(arpansaData, uvIndexes);

            //tell canvas to redraw
            this.canvasView.InvalidateSurface();
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
