using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace UV_Mate
{
    //I got sick of calculating alpha values in bits so I extended the WithAlpha method of SKColor
    public static class MSKColor
    {
        //percentage is between 0.0 and 1.0
        public static SKColor WithAlpha(this SKColor colour, float percentage)
        {
            byte alphaByte = (byte)(255 * percentage);
            return colour.WithAlpha(alphaByte);
        }
    }
}
