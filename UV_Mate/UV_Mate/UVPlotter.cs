using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace UV_Mate
{
    [Preserve(AllMembers = true)]
    class UVPlotter
    {
        //stores the SKview object. Used to force a manual canvas refresh
        private SKGLView canvasView;

        //SKSurface contains all objects needed to draw to the canvas
        private SKSurface fullPlotSurface;

        //variables associated with the graph
        private string xAxisTitle;
        private float minY;
        private float maxY;
        private float deltaY;

        private string yAxisTitle;
        private TimeSpan minX;
        private TimeSpan maxX;
        private TimeSpan deltaX;

        //variable used to space out text on x and y axes
        private static float paddingHeight = 30f;

        //paint colours for graph lines
        private SKPaint predictedLinePaint;
        private SKPaint measuredLinePaint;
        //paints for area under graph line
        private SKPaint measuredAreaPaint;

        private SKPaint currentUVTextPaint;

        private Color backgroundColor;

        //model which stores UV datapoints
        private ArpansaViewModel arpansaModel;

        private float unitsPerUVLevel;
        private float unitsPerMinute;

        //Skia by default draws from top left. Make a graph surface which has an origin in bottom left.
        private SKSurface graphSurface = null;
        private float gridPosX = paddingHeight;
        private float gridPosY = 0f;
        private float gridWidth;
        private float gridHeight;

        //constructor
        public UVPlotter(SKGLView mCanvasView, ArpansaViewModel mArpansaModel, TimeSpan mMinX, TimeSpan mMaxX, TimeSpan mDeltaX, float mMinY, float mMaxY, float mDeltaY, string mXAxisTitle, string mYAxisTitle, Color backgroundColor)
        {
            this.canvasView = mCanvasView;
            this.arpansaModel = mArpansaModel;
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
                Color = SKColors.Gray.WithAlpha(0.4f),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3f,
                IsAntialias = true
            };
            this.measuredLinePaint = new SKPaint
            {
                Color = SKColors.DarkBlue,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3f,
                IsAntialias = true
            };
            this.measuredAreaPaint = new SKPaint
            {
                Color = new SKColor(20, 20, 100).WithAlpha(0.1f),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            this.currentUVTextPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 20f,
                IsAntialias = true
            };

            this.backgroundColor = backgroundColor;

            this.arpansaModel.PropertyChanged += ArpansaModel_PropertyChanged;
        }

        private void ArpansaModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "ArpansaUVData")
            {
                //graph data has changed. redraw canvas
                this.canvasView.InvalidateSurface();
            }
        }

        //method responsible for drawing on the canvas
        public void DrawGraph(SKPaintGLSurfaceEventArgs e)
        {
            GRBackendRenderTargetDesc viewInfo = e.RenderTarget;
            this.fullPlotSurface = e.Surface;
            SKCanvas canvas = this.fullPlotSurface.Canvas;
            
            /*
            initalize graph grid
            
            SKIA tries to be too smart by updating GRBackendRenderTargetDesc.Width and GRBackendRenderTargetDesc.Height
            to reflect any scale applied to canvas. Need to remove any scaling to get pixel dimensions correct.
            */
            canvas.Save();
            //undo any scaling so canvas dimensions are back in pixels
            canvas.ResetMatrix();
            float pixelPerUnit = (float)(viewInfo.Width / this.canvasView.Width);
            //get grid width in xamarin units
            float gridWidth = (float)(this.canvasView.Width - gridPosX);
            //get grid width in pixels
            int gridWidthPixels = (int)(gridWidth * pixelPerUnit);
            //get grid height in xamarin units
            float gridHeight = (float)(this.canvasView.Height - paddingHeight);
            //get grid height in pixels
            int gridHeightPixels = (int)(gridHeight * pixelPerUnit);
            this.gridWidth = gridWidth;
            this.gridHeight = gridHeight;
            //create the surface in pixels
            this.graphSurface = SKSurface.Create(gridWidthPixels, gridHeightPixels, SKColorType.Rgba8888, SKAlphaType.Premul);
            //scale surface so it's no in xamarin units
            this.graphSurface.Canvas.Scale(pixelPerUnit);
            canvas.Restore();

            //flip graph so origin is bottom left
            this.graphSurface.Canvas.Scale(1.0f, -1.0f);
            this.graphSurface.Canvas.Translate(0, -gridHeight);

            //calculate x and y scales
            float UVRange = (this.maxY + 1) - this.minY;
            this.unitsPerUVLevel = this.gridHeight / UVRange;

            TimeSpan timeRange = this.maxX - this.minX;
            this.unitsPerMinute = (float)(this.gridWidth / timeRange.TotalMinutes);

            //fill in background
            this.DrawBackground();

            //put axis titles on screen
            this.DrawAxes();

            //draw measured and approximated plot lines
            this.DrawUVPlots();

            //draw uv level dash lines
            this.DrawHorizontalDashes();

            //Write current UV
            this.DrawCurrentUV();
            
            /*
            Again Skia tries to be smart by always updating GRBackendRenderTargetDesc.Width and GRBackendRenderTargetDesc.Height
            to reflect any scale applied to canvas. Need to remove all scaling so all units are pixels before
            drawing graph surface to the canvas.
            */
            canvas.Save();
            canvas.ResetMatrix();
            canvas.DrawSurface(this.graphSurface, gridPosX * pixelPerUnit, gridPosY * pixelPerUnit);
            canvas.Restore();
        }

        private void DrawBackground()
        {
            this.fullPlotSurface.Canvas.DrawColor(SKColor.Parse(this.backgroundColor.ToHex()));
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

            //draw y axis
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

            SKPaint xAxisUnitPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                TextSize = 14f
            };
            
            //for each x mark
            for (TimeSpan timeValue = minX; timeValue <= maxX; timeValue = timeValue.Add(TimeSpan.FromHours(1.0d)))
            {
                int hourValue = timeValue.Hours;
                int minuteValue = timeValue.Minutes;
                string timeText = hourValue.ToString() + ":" + minuteValue.ToString("D2");

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
            ArpansaUVData arpansaData = this.arpansaModel.ArpansaUVData;
            if (arpansaData?.GraphData == null || arpansaData.GraphData.Length <= 0)
            {
                //no graph data
                return;
            }
            if(arpansaData?.ReferenceUVs == null || arpansaData.ReferenceUVs.Count <= 0)
            {
                //need reference points for background colour
                return;
            }

            GraphData[] graphData = arpansaData.GraphData;
            SKCanvas graphCanvas = this.graphSurface.Canvas;
            SKPath approximatePath = null;
            SKPath measuredPath = null;

            //collect data for the UV plot lines
            TimeSpan timeValue = DateTimeStringToTime(graphData[0].Date);

            //approximate UV
            float? startForecast = graphData[0].Forecast;
            if (startForecast != null)
            {
                float approxX = (float)(timeValue.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
                float approxY = (float)startForecast * this.unitsPerUVLevel;
                approximatePath = new SKPath();
                approximatePath.MoveTo(approxX, approxY);
            }

            //measured UV
            float? startMeasured = graphData[0].Measured;
            if(startMeasured != null)
            {
                float measuredX = (float)(timeValue.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
                float measuredY = (float)graphData[0].Measured * this.unitsPerUVLevel;
                measuredPath = new SKPath();
                measuredPath.MoveTo(measuredX, measuredY);
            }
            
            //for each plot point
            for (int i = 1; i < graphData.Length; i++)
            {
                //get time
                TimeSpan curTimeValue = DateTimeStringToTime(graphData[i].Date);

                //approximate UV
                float? curApproxUV = graphData[i].Forecast;
                if (curApproxUV != null && approximatePath != null)
                {
                    float curApproxX = (float)(curTimeValue.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
                    float curApproxY = (float)curApproxUV * this.unitsPerUVLevel;
                    approximatePath.LineTo(curApproxX, curApproxY);
                }

                //measured UV
                float? curMeasuredUV = graphData[i].Measured;
                if (curMeasuredUV != null && measuredPath != null)
                {
                    float curMeasuredX = (float)(curTimeValue.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
                    float curMeasuredY = (float)curMeasuredUV * this.unitsPerUVLevel;
                    measuredPath.LineTo(curMeasuredX, curMeasuredY);
                }
            }

            //create an awesome rainbow shader
            SKPoint graphBottom = new SKPoint(0.0f, 0.0f);
            UVIndex highestUVRef = arpansaData.ReferenceUVs[arpansaData.ReferenceUVs.Count - 1];
            float highestIVValue = highestUVRef.LowerValue;
            SKPoint highRefPoint = new SKPoint(0.0f, highestIVValue * this.unitsPerUVLevel);

            List<SKColor> colourList = new List<SKColor>();
            List<float> posList = new List<float>();
            foreach (var uvInfo in arpansaData.ReferenceUVs)
            {
                colourList.Add(uvInfo.Colour.WithAlpha(0.4f));
                posList.Add(uvInfo.LowerValue / highestIVValue);

            }
            SKShader uvShader = SKShader.CreateLinearGradient(graphBottom, highRefPoint, colourList.ToArray(), posList.ToArray(), SKShaderTileMode.Clamp);
            this.measuredLinePaint.Shader = uvShader;

            ////draw approximate and measured plot lines
            if (approximatePath != null)
            {
                graphCanvas.DrawPath(approximatePath, this.predictedLinePaint);
            }
            if (measuredPath != null)
            {
                graphCanvas.DrawPath(measuredPath, this.measuredLinePaint);

                //now draw area under plot lines
                SKPoint firstMeasuredPoint = measuredPath.GetPoint(0);
                SKPoint lastMeasuredPoint = measuredPath.GetPoint(measuredPath.PointCount - 1);
                measuredPath.LineTo(lastMeasuredPoint.X, 0);
                measuredPath.LineTo(firstMeasuredPoint.X, 0);
                measuredPath.Close();

                graphCanvas.DrawPath(measuredPath, this.measuredAreaPaint);
            }
        }

        private void DrawHorizontalDashes()
        {
            SKCanvas graphCavnas = this.graphSurface.Canvas;
            ArpansaUVData arpansaData = this.arpansaModel.ArpansaUVData;

            if (arpansaData?.ReferenceUVs == null)
            {
                //need reference UV levels
                return;
            }

            //for each refernce UV level
            for (int i = 0; i < arpansaData.ReferenceUVs.Count; i++)
            {
                float uvValue = arpansaData.ReferenceUVs[i].LowerValue;
                float unitsHigh = uvValue* this.unitsPerUVLevel;
                float RefLineYPos = unitsHigh;

                SKPaint refLinePaint = new SKPaint
                {
                    Color = arpansaData.ReferenceUVs[i].Colour.WithAlpha(0.5f),
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
                    Color = arpansaData.ReferenceUVs[i].Colour,
                    TextAlign = SKTextAlign.Right,
                    IsAntialias = true,
                    TextSize = 15f
                };

                float padding = 10f;
                float textPosX = this.gridWidth - padding;
                float textPosY = RefLineYPos + padding;

                DrawTextOnGraph(arpansaData.ReferenceUVs[i].DetailText, textPosX, textPosY, refTextPaint);
            }
        }

        private void DrawCurrentUV()
        {
            SKCanvas graphCanvas = this.graphSurface.Canvas;
            ArpansaUVData arpansaData = this.arpansaModel.ArpansaUVData;

            if (arpansaData?.GraphData == null || arpansaData.GraphData.Length <= 0)
            {
                //need graph data
                return;
            }
            if (arpansaData?.CurrentUVIndex == null || arpansaData?.CurrentDateTime == null)
            {
                //need current data
                return;
            }
            if(arpansaData?.ReferenceUVs == null)
            {
                //need reference UV levels
                return;
            }

            //get latest datapoint
            float curUVLevel = float.Parse(arpansaData.CurrentUVIndex);

            TimeSpan lastMeasurementTime = DateTimeStringToTime(arpansaData.CurrentDateTime);

            float currentX = (float)(lastMeasurementTime.TotalMinutes - this.minX.TotalMinutes) * this.unitsPerMinute;
            float currentY = curUVLevel * this.unitsPerUVLevel;

            string uvString = curUVLevel.ToString("F1");


            //create a paint based of the current reading
            //paint colour will be set to the current UV reference colour
            UVIndex curUVIndex = null;
            for (int i = 0; i < arpansaData.ReferenceUVs.Count; i++)
            {
                if(curUVLevel >= arpansaData.ReferenceUVs[i].LowerValue)
                {
                    curUVIndex = arpansaData.ReferenceUVs[i];
                }
            }
            SKPaint curColourAreaPaint = new SKPaint
            {
                Color = curUVIndex.Colour.WithAlpha(0.8f),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            SKPaint curColourLinePaint = new SKPaint
            {
                Color = curUVIndex.Colour.WithAlpha(0.4f),
                Style = SKPaintStyle.Stroke,
                IsAntialias = true,
                StrokeWidth = 2f
            };

            //draw a nice circle on the endpoint
            float pointRadius = 3.6f;
            graphCanvas.DrawCircle(currentX, currentY, pointRadius, new SKPaint { Color = SKColors.White });
            graphCanvas.DrawCircle(currentX, currentY, pointRadius, curColourLinePaint);


            //write the current text a little up and to the right of the current point
            float textX = currentX + 12f;
            float textY = currentY + 12f;

            SKRect uvRectFrame = new SKRect();
            this.currentUVTextPaint.MeasureText(uvString, ref uvRectFrame);

            //do a check to see if current UV reading will appear off the screen
            float framePadding = 5f;
            if (textX + uvRectFrame.Width + framePadding >= this.gridWidth)
            {
                //being cut off on x axis
                GraphData closestPoint = this.GetClosestDataPointAtTime(this.maxX);

                textX = this.gridWidth - uvRectFrame.Width - framePadding - 4f;
            }
            if (textY + uvRectFrame.Height + framePadding >= this.gridHeight)
            {
                //being cut off on y axis
                textY = this.gridHeight - uvRectFrame.Height - framePadding - 4f;
            }

            uvRectFrame.Offset(textX, textY + uvRectFrame.Height);
            uvRectFrame.Inflate(framePadding, framePadding);

            graphCanvas.DrawRoundRect(uvRectFrame, 3f, 3f, curColourAreaPaint);
            this.DrawTextOnGraph(uvString, textX, textY, this.currentUVTextPaint);
        }

        //util method to find the closes datapoint for a given value on the x axis
        private GraphData GetClosestDataPointAtTime(TimeSpan targetTime)
        {
            ArpansaUVData arpansaData = this.arpansaModel.ArpansaUVData;

            if (arpansaData == null)
            {
                //no data to sort through
                return null;
            }

            GraphData[] uvData = arpansaData.GraphData;

            GraphData closestPoint = null;
            double closestDiff = double.PositiveInfinity;

            for (int i = 0; i < uvData.Length; i++)
            {
                TimeSpan uvTime = DateTimeStringToTime(uvData[i].Date);
                double diff = Math.Abs(targetTime.TotalMinutes - uvTime.TotalMinutes);
                if(diff < closestDiff)
                {
                    closestPoint = uvData[i];
                    closestDiff = diff;
                }
            }

            return closestPoint;
        }

        //util method to extract TimeSpan object from a string that contains the time
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

        //text on graph will appear unside down since the y axis is flipped. This util method will make sure text will appear upright in the graph cavnas
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
