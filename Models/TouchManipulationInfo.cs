using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace AAA.Models
{
    public class TouchManipulationInfo
    {
        public SKPoint PreviousPoint { set; get; }

        public SKPoint NewPoint { set; get; }
    }
}
