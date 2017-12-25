using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
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

        private static float paddingHeight = 30f;

        //Plot data
        private List<UVIndex> uvIndexes = null;
        private ArpansaUVResponse arpansaUVData = null;

        private float unitsPerUVLevel;
        private float unitsPerMinute;

        float gridPosX = paddingHeight;
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
                StrokeWidth = 2f,
                IsAntialias = true
            };

            this.measuredLinePaint = new SKPaint
            {
                Color = SKColors.DarkBlue,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f,
                IsAntialias = true
            };
        }

        public void SetPlotPoints(ArpansaUVResponse arpansaUV, List<UVIndex> mUvIndexes)
        {
            this.uvIndexes = mUvIndexes;
            this.arpansaUVData = arpansaUV;
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

            TimeSpan timeRange = (this.maxX.Add(TimeSpan.FromHours(1))) - this.minX;
            this.unitsPerMinute = (float)(this.gridWidth / timeRange.TotalMinutes);

            //draw uv level dash lines
            this.DrawHorizontalDashes();

            //put axis titles on screen
            this.DrawAxes();

            //draw approximate plotline
            this.DrawUVPlots();

            //Write current UV
            this.DrawCurrentUV();

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
                    TextSize = 15f
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

            SKPaint yAxisUnitPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextAlign = SKTextAlign.Right,
                IsAntialias = true,
                TextSize = 14f
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
                    float paddingY = (yAxisUnitPaint.TextSize * 0.3f);
                    canvas.DrawText(UVNum.ToString(), curUVPointX - paddingX, curUVPointY + paddingY, yAxisUnitPaint);

                    // draw line
                    canvas.DrawLine(curUVPointX, curUVPointY, (float)this.canvasView.Width, curUVPointY, refLinePaint);
                }
            }

            SKPaint axisLabelPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                TextSize = 18f
            };

            float verAxisPosX = axisLabelPaint.TextSize * 0.8f;
            float verAxisPosY = gridBottomY / 2f;
            canvas.Save();
            canvas.RotateDegrees(-90, verAxisPosX, verAxisPosY);
            canvas.DrawText("UV Level", verAxisPosX, verAxisPosY, axisLabelPaint);
            canvas.Restore();


            SKPaint timeAxisMarks = new SKPaint
            {
                Color = SKColors.LightGray,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            };

            //x axis
            SKPaint xAxisUnitPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                TextSize = 14f
            };

            for (TimeSpan timeValue = minX; timeValue <= maxX; timeValue = timeValue.Add(TimeSpan.FromHours(1.0d)))
            {
                int hourValue = timeValue.Hours;
                int minuteValue = timeValue.Minutes;
                string timeText = hourValue.ToString() + ":" + minuteValue.ToString("00");

                float displacementFromBottom = (float)(timeValue.TotalMinutes - minX.TotalMinutes) * this.unitsPerMinute;

                float curTimePointX = gridBottomX + displacementFromBottom;
                float curTimePointY = gridBottomY;

                float markLength = 5f;

                if (timeValue.TotalMinutes % this.deltaX.TotalMinutes == 0)
                {
                    //major mark
                    markLength = 10f;
                    canvas.DrawText(timeText, curTimePointX, curTimePointY + (paddingHeight), xAxisUnitPaint);
                }

                canvas.DrawLine(curTimePointX, curTimePointY, curTimePointX, curTimePointY + markLength, timeAxisMarks);
            }
        }

        private void DrawUVPlots()
        {
            if (this.arpansaUVData?.GraphData?.Length == null || this.arpansaUVData?.GraphData?.Length <= 0)
            {
                //No data to draw
                return;
            }

            GraphData[] graphData = this.arpansaUVData.GraphData;
            SKCanvas graphCanvas = this.graphSurface.Canvas;
            SKPath approximatePath = new SKPath();
            SKPath measuredPath = new SKPath();

            TimeSpan timeValue = DateTimeStringToTime(graphData[0].Date);

            float approxX = (float)(timeValue.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
            float approxY = (float)graphData[0].Forecast * this.unitsPerUVLevel;
            approximatePath.MoveTo(approxX, approxY);

            float measuredX = approxX;
            float measuredY = (float)graphData[0].Measured * this.unitsPerUVLevel; ;
            measuredPath.MoveTo(measuredX, measuredY);

            for (int i = 1; i < graphData.Length; i++)
            {
                //get time
                TimeSpan curTimeValue = DateTimeStringToTime(graphData[i].Date);
                float? curApproxUV = graphData[i].Forecast;

                //approximate UV
                float curApproxX = (float)(curTimeValue.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
                float curApproxY = (float)curApproxUV * this.unitsPerUVLevel;
                approximatePath.LineTo(curApproxX, curApproxY);

                //measured UV
                float? curMeasuredUV = graphData[i]?.Measured;
                if (curMeasuredUV != null)
                {
                    float curMeasuredX = curApproxX;
                    float curMeasuredY = (float)curMeasuredUV * this.unitsPerUVLevel;
                    measuredPath.LineTo(curMeasuredX, curMeasuredY);
                }
            }

            graphCanvas.DrawPath(approximatePath, predictedLinePaint);
            graphCanvas.DrawPath(measuredPath, measuredLinePaint);
        }

        private void DrawCurrentUV()
        {
            SKCanvas graphCanvas = this.graphSurface.Canvas;

            if (this.arpansaUVData?.GraphData?.Length == null || this.arpansaUVData?.GraphData?.Length <= 0)
            {
                //No data to draw
                return;
            }

            float curUVLevel = float.Parse(this.arpansaUVData.CurrentUVIndex);

            SKPaint textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 20f,
                IsAntialias = true
            };

            SKPaint framePaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Blue
            };

            TimeSpan lastMeasurementTime = DateTimeStringToTime(this.arpansaUVData.CurrentDateTime);

            float textX = (float)(lastMeasurementTime.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
            float textY = curUVLevel * this.unitsPerUVLevel;

            //add some padding to the location
            textX += 8f;
            textY += 8f;

            SKRect uvRectFrame = new SKRect();
            textPaint.MeasureText(curUVLevel.ToString(), ref uvRectFrame);
            uvRectFrame.Offset(textX, textY + uvRectFrame.Height);
            uvRectFrame.Inflate(5f, 5f);

            graphCanvas.DrawRoundRect(uvRectFrame, 3f, 3f, framePaint);
            this.DrawTextOnGraph(curUVLevel.ToString(), textX, textY, textPaint);
        }

        private TimeSpan DateTimeStringToTime(string dateTimeString)
        {
            //get time from string
            Regex timePattern = new Regex(@"[0-9]{1,2}:[0-9]{1,2}");
            String timeString = timePattern.Match(dateTimeString).Value;

            //get hours
            Regex hourPattern = new Regex(@"[0-9]{1,2}:");
            int hours = int.Parse(hourPattern.Match(timeString).Value.TrimEnd(':'));

            //get minutes
            Regex minutesPattern = new Regex(@":[0-9]{1,2}");
            int minutes = int.Parse(minutesPattern.Match(timeString).Value.TrimStart(':'));

            return new TimeSpan(hours, minutes, 0);
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
