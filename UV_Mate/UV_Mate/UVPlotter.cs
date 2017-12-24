using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace UV_Mate
{
    class UVPlotter
    {
        private SKGLView canvasView;

        string xAxisTitle;
        float minY;
        float maxY;
        float deltaY;

        string yAxisTitle;
        TimeSpan minX;
        TimeSpan maxX;
        TimeSpan deltaX;

        SKSurface fullPlotSurface;

        //paints for graph lines
        SKPaint predictedLinePaint;
        SKPaint measuredLinePaint;

        //paints for area under graph lines

        //paint for UV reference lines

        private static float paddingHeight = 22f;

        //Plot data
        private List<UVIndex> uvIndexes = null;

        private float unitsPerUVLevel;

        float gridPosX = 2 * paddingHeight;
        float gridPosY = 0f;
        float gridWidth;
        float gridHeight;
        //GRBackendRenderTargetDesc graphInfo;
        SKSurface graphSurface = null;

        public UVPlotter(SKGLView mCanvasView, TimeSpan mMinX, TimeSpan mMaxX, TimeSpan mDeltaX, float mMinY, float mMaxY, float mDeltaY, string mXAxisTitle, string mYAxisTitle)
        {
            this.canvasView = mCanvasView;
            this.minX = mMinX;
            this.maxX = mMaxX;
            this.deltaX = mDeltaX;
            this.minY = mMinY;
            this.maxY = mMaxY;
            this.deltaY = mDeltaY;
            this.xAxisTitle = mXAxisTitle;
            this.yAxisTitle = mYAxisTitle;
            this.predictedLinePaint = new SKPaint
            {
                Color = SKColors.LightBlue,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };

            this.measuredLinePaint = new SKPaint
            {
                Color = SKColors.DarkBlue,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };
        }

        public void SetPlotPoints(ArpansaUVResponse arpansaUV, List<UVIndex> mUvIndexes)
        {
            this.uvIndexes = mUvIndexes;
        }

        public void DrawGraph(SKPaintGLSurfaceEventArgs e)
        {
            GRBackendRenderTargetDesc viewInfo = e.RenderTarget;
            this.fullPlotSurface = e.Surface;
            SKCanvas canvas = this.fullPlotSurface.Canvas;


            //initalize graph grid
            /*
            SKIA tries to be too smart by updating GRBackendRenderTargetDesc.Width and GRBackendRenderTargetDesc.Height
            to reflect any scale applied to canvas. Need to remove any scaling to get pixel dimension correct.
            */
            canvas.Save();
            canvas.ResetMatrix();
            float pixelPerUnit = (float)(viewInfo.Width / this.canvasView.Width);
            float gridWidth = (float)(this.canvasView.Width - gridPosX);
            int gridWidthPixels = (int)(gridWidth * pixelPerUnit);
            float gridHeight = (float)(this.canvasView.Height - paddingHeight);
            int gridHeightPixels = (int)(gridHeight * pixelPerUnit);
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            this.graphSurface = SKSurface.Create(gridWidthPixels, gridHeightPixels, SKColorType.Rgba8888, SKAlphaType.Premul);
            this.graphSurface.Canvas.Scale(pixelPerUnit);
            canvas.Restore();

            //flip graph so origin is bottom left
            this.graphSurface.Canvas.Scale(1.0f, -1.0f);
            this.graphSurface.Canvas.Translate(0, -gridHeight);

            //calculate x and y scales
            float UVRange = (this.maxY + 1) - this.minY;
            this.unitsPerUVLevel = this.gridHeight / UVRange;

            //draw uv level dash lines
            this.DrawHorizontalDashes();

            //put axis titles on screen
            this.DrawAxes();

            //draw approximate plotline

            //draw measured plotline
            canvas.Save();
            canvas.ResetMatrix();
            canvas.DrawSurface(this.graphSurface, gridPosX * pixelPerUnit, gridPosY * pixelPerUnit);
            canvas.Restore();
        }

        private void DrawHorizontalDashes()
        {
            SKCanvas graphCavnas = this.graphSurface.Canvas;

            for(int i = 0; i < this.uvIndexes?.Count; i++)
            {
                float uvValue = uvIndexes[i].LowerValue;
                float unitsHigh = uvValue* this.unitsPerUVLevel;
                float RefLineYPos = unitsHigh;

                SKPaint refLinePaint = new SKPaint
                {
                    Color = uvIndexes[i].Colour,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f,
                    PathEffect = SKPathEffect.CreateDash(new float[] { 6f, 3f }, 0),
                    IsAntialias = true
                };

                // draw line
                graphCavnas.DrawLine(0, RefLineYPos, this.gridWidth, RefLineYPos, refLinePaint);

                // write text above each reference point
                SKPaint refTextPaint = new SKPaint
                {
                    Color = uvIndexes[i].Colour,
                    TextAlign = SKTextAlign.Right,
                    IsAntialias = true,
                    TextSize = 10f
                };

                float padding = 10f;
                float textPosX = this.gridWidth - padding;
                float textPosY = RefLineYPos + padding;

                DrawTextOnGraph(uvIndexes[i].DetailText, textPosX, textPosY, refTextPaint);
            }
        }

        private void DrawAxes()
        {
            bool VerticalOrientation = this.canvasView.Width < this.canvasView.Height ? true : false;

            SKCanvas canvas = this.fullPlotSurface.Canvas;

            SKPaint axisUnitPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextAlign = SKTextAlign.Right,
                IsAntialias = true,
                TextSize = 10f
            };

            SKPaint refLinePaint = new SKPaint
            {
                Color = SKColors.LightGray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                PathEffect = SKPathEffect.CreateDash(new float[] { 6f, 3f }, 0),
                IsAntialias = true
            };

            //y axis
            float gridBottomX = this.gridPosX;
            float gridBottomY = this.gridHeight;

            for (float UVNum = minY; UVNum <= maxY; ++UVNum)
            {
                float unitsFromGridBottom = UVNum * this.unitsPerUVLevel;

                float curUVPointX = gridBottomX;
                float curUVPointY = gridBottomY - unitsFromGridBottom;

                if (VerticalOrientation)
                {
                    // draw line
                    canvas.DrawLine(curUVPointX, curUVPointY, (float)this.canvasView.Width, curUVPointY, refLinePaint);
                }

                if (UVNum % this.deltaY == 0f || UVNum == 0f)
                {
                    // write text at each reference point
                    float paddingX = 5f;
                    float paddingY = (axisUnitPaint.TextSize * 0.3f);
                    canvas.DrawText(UVNum.ToString(), curUVPointX - paddingX, curUVPointY + paddingY, axisUnitPaint);

                    // draw line
                    canvas.DrawLine(curUVPointX, curUVPointY, (float)this.canvasView.Width, curUVPointY, refLinePaint);
                }
            }

            SKPaint axisLabelPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                TextSize = paddingHeight * 0.8f
            };

            float verAxisPosX = paddingHeight;
            float verAxisPosY = gridBottomY / 2f;
            canvas.Save();
            canvas.RotateDegrees(-90, verAxisPosX, verAxisPosY);
            canvas.DrawText("UV Level", verAxisPosX, verAxisPosY, axisLabelPaint);
            canvas.Restore();


            //x axis
        }

        //text on graph will appear unside down since the y axis is flipped
        private void DrawTextOnGraph(string message, float xUnits, float yUnits, SKPaint textPaint)
        {
            SKCanvas graphCavnas = this.graphSurface.Canvas;
            graphCavnas.Save();
            graphCavnas.Translate(xUnits, yUnits);
            graphCavnas.Scale(1f, -1f);
            graphCavnas.DrawText(message, 0f, 0f, textPaint);
            graphCavnas.Restore();
        }
    }
}
