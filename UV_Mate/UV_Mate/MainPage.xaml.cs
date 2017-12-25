using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace UV_Mate
{
	public partial class MainPage : ContentPage
	{
        //AnalogClock clockObj;
        UVPlotter uvGraph;

        ArpansaRealtimeFeed arpansaService;

        public MainPage()
		{
			InitializeComponent();

            //this.clockObj = new AnalogClock(this.canvasView);
            this.uvGraph = new UVPlotter(this.canvasView, new TimeSpan(6, 0, 0), new TimeSpan(20, 0, 0), new TimeSpan(3, 0, 0), 0, 14, 2, "Time of Day", "UV Level");

            this.arpansaService = new ArpansaRealtimeFeed();

            this.Appearing += MainPage_Appearing;
        }

        private async void MainPage_Appearing(object sender, EventArgs e)
        {
            ArpansaUVResponse arpansaUV = await arpansaService.GetUVData();
            List<UVIndex> uvIndexes = arpansaService.GenerateUVIndexs();
            
            uvGraph.SetPlotPoints(arpansaUV, uvIndexes);

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

            //clockObj.Draw(e);

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
