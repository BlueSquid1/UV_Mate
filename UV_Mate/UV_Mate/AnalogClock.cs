using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace UV_Mate
{
    class AnalogClock
    {
        private SKGLView canvasView;
        private float radius;

        private float secondRot;
        private float minuteRot;
        private float hourRot;

        public AnalogClock(SKGLView mCanvasView, float mRadius = 100f)
        {
            this.canvasView = mCanvasView;
            this.radius = mRadius;

            this.OnClockTick();

            Device.StartTimer(TimeSpan.FromSeconds(1.0), OnClockTick);
        }

        private bool OnClockTick()
        {
            DateTime currentTime = DateTime.Now;

            this.secondRot = ((float) currentTime.Second / 60.0f) * 360.0f;
            this.minuteRot = ((float)currentTime.Minute / 60.0f) * 360.0f;
            this.hourRot = ((float)currentTime.Hour / 12.0f) * 360.0f;

            //force the cavas to refresh
            this.canvasView.InvalidateSurface();
            return true;
        }

        public void Draw( SKPaintGLSurfaceEventArgs e )
        {
            GRBackendRenderTargetDesc viewInfo = e.RenderTarget;
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;

            float circlePosX = (float)canvasView.Width * 0.5f;
            float circlePosY = (float)canvasView.Height * 0.5f;
            float circleRadius = this.radius;

            this.DrawClockFace(canvas, circlePosX, circlePosY, circleRadius);
            this.DrawHoursMarks(canvas, circlePosX, circlePosY, circleRadius);
            this.DrawClockHands(canvas, circlePosX, circlePosY, circleRadius);
        }

        private void DrawClockFace(SKCanvas canvas, float circlePosX, float circlePosY, float circleRadius)
        {
            SKPaint clockPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Silver,
                Style = SKPaintStyle.Fill
            };

            canvas.DrawCircle(circlePosX, circlePosY, circleRadius, clockPaint);

            SKPaint BorderPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4f
            };

            canvas.DrawCircle(circlePosX, circlePosY, circleRadius, BorderPaint);
        }

        private void DrawHoursMarks(SKCanvas canvas, float circlePosX, float circlePosY, float circleRadius)
        {
            SKPaint hourPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.DarkGray,
                Style = SKPaintStyle.Fill
            };

            canvas.Save();
            float rotPerHour = 360 / 12;
            for(int i = 0; i < 12; i++)
            {
                canvas.DrawCircle(circlePosX, circlePosY - circleRadius + 10f, 3f, hourPaint);
                canvas.RotateDegrees(rotPerHour, circlePosX, circlePosY);
            }
            canvas.Restore();
        }

        private void DrawClockHands(SKCanvas canvas, float circlePosX, float circlePosY, float circleRadius)
        {
            SKPaint secondsPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Red,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f
            };

            //don't want transforms to effect rest of canvas that's why use save and restores
            canvas.Save();
            canvas.RotateDegrees(this.secondRot, circlePosX, circlePosY);
            canvas.DrawLine(circlePosX, circlePosY, circlePosX, circlePosY - (circleRadius * 0.9f), secondsPaint);
            canvas.Restore();

            SKPaint minutePaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3f
            };

            canvas.Save();
            canvas.RotateDegrees(this.minuteRot, circlePosX, circlePosY);
            canvas.DrawLine(circlePosX, circlePosY, circlePosX, circlePosY - (circleRadius * 0.7f), minutePaint);
            canvas.Restore();

            SKPaint hourPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 8f
            };

            canvas.Save();
            canvas.RotateDegrees(this.hourRot, circlePosX, circlePosY);
            canvas.DrawLine(circlePosX, circlePosY, circlePosX, circlePosY - (circleRadius * 0.4f), hourPaint);
            canvas.Restore();


        }
    }
}
