using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace AAA.Handlers
{
    public class TouchManipulationManager
    {
        private SKPoint bottomRight;

        public TouchManipulationManager()
        {
            bottomRight = new SKPoint(TouchManipulationBitmap.CanvasWidth, TouchManipulationBitmap.CanvasHeight);
        }

        public SKMatrix OneFingerManipulate(SKPoint prevPoint, SKPoint newPoint)
        {
            // if movement will take us out of bounds of image, stop the movement in that direction
            SKPoint scaledBottomRight = TouchManipulationBitmap.Matrix.MapVector(bottomRight);
            
            SKPoint delta = newPoint - prevPoint;

            float translatedX = -TouchManipulationBitmap.Matrix.TransX + TouchManipulationBitmap.CanvasWidth;
            float translatedY = -TouchManipulationBitmap.Matrix.TransY + TouchManipulationBitmap.CanvasHeight;

            if ((delta.X > 0 && TouchManipulationBitmap.Matrix.TransX >= 0) // moving left
                ||
                (delta.X < 0 && translatedX >= scaledBottomRight.X)) // moving right
            {
                delta.X = 0;
            }

            if ((delta.Y > 0 && TouchManipulationBitmap.Matrix.TransY >= 0) // moving up
                ||
                (delta.Y < 0 && translatedY >= scaledBottomRight.Y)) // moving down
            {
                delta.Y = 0;
            }

            // multiply the rotation matrix by a translation matrix
            SKMatrix touchMatrix = SKMatrix.CreateIdentity();
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(delta.X, delta.Y));

            return touchMatrix;
        }

        public SKMatrix TwoFingerManipulate(SKPoint prevPoint, SKPoint newPoint, SKPoint pivotPoint)
        {
            SKMatrix touchMatrix = SKMatrix.CreateIdentity();
            SKPoint oldVector = prevPoint - pivotPoint;
            SKPoint newVector = newPoint - pivotPoint;

            float scale = Magnitude(newVector) / Magnitude(oldVector);

            // if the scale crossed MIN_SCALE and the manipulation is below median (keep on scaling out) - do nothing
            if (TouchManipulationBitmap.Matrix.ScaleX <= TouchManipulationBitmap.SCALE_MIN && scale <= TouchManipulationBitmap.SCALE_MEDIAN) return touchMatrix;
            // if the scale crossed MAX_SCALE and the manipulation is above median (keep on scaling in) - do nothing
            if (TouchManipulationBitmap.Matrix.ScaleX >= TouchManipulationBitmap.SCALE_MAX && scale >= TouchManipulationBitmap.SCALE_MEDIAN) return touchMatrix;

            if (!float.IsNaN(scale) && !float.IsInfinity(scale))
            {
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateScale(scale, scale, pivotPoint.X, pivotPoint.Y));
            }

            SKPoint topLeft = TouchManipulationBitmap.Matrix.MapPoint(new SKPoint(0, 0));
            SKPoint bottomRight = TouchManipulationBitmap.Matrix.MapPoint(new SKPoint(TouchManipulationBitmap.CanvasWidth, TouchManipulationBitmap.CanvasHeight));

            // top left
            if (topLeft.X > 0 && topLeft.Y > 0)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(-topLeft.X, -topLeft.Y));
            // left
            else if (topLeft.X > 0)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(-topLeft.X, 0));
            //top
            else if (topLeft.Y > 0)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(0, -topLeft.Y));

            // bottom right
            if (bottomRight.X < TouchManipulationBitmap.CanvasWidth && bottomRight.Y < TouchManipulationBitmap.CanvasHeight)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(TouchManipulationBitmap.CanvasWidth - bottomRight.X, TouchManipulationBitmap.CanvasHeight - bottomRight.Y));
            // right
            else if (bottomRight.X < TouchManipulationBitmap.CanvasWidth)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(TouchManipulationBitmap.CanvasWidth - bottomRight.X, 0));
            // bottom
            else if (bottomRight.Y < TouchManipulationBitmap.CanvasHeight)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(0, TouchManipulationBitmap.CanvasHeight - bottomRight.Y));

            return touchMatrix;
        }

        public SKMatrix AutoManipulation(SKPoint destinationPoint, float scale)
        {
            SKMatrix touchMatrix = SKMatrix.CreateIdentity();
            touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateScale(scale, scale, destinationPoint.X, destinationPoint.Y));

            SKPoint topLeft = TouchManipulationBitmap.Matrix.MapPoint(new SKPoint(0, 0));
            SKPoint bottomRight = TouchManipulationBitmap.Matrix.MapPoint(new SKPoint(TouchManipulationBitmap.CanvasWidth, TouchManipulationBitmap.CanvasHeight));

            // top left
            if (topLeft.X > 0 && topLeft.Y > 0)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(-topLeft.X, -topLeft.Y));
            // left
            else if (topLeft.X > 0)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(-topLeft.X, 0));
            //top
            else if (topLeft.Y > 0)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(0, -topLeft.Y));

            // bottom right
            if (bottomRight.X < TouchManipulationBitmap.CanvasWidth && bottomRight.Y < TouchManipulationBitmap.CanvasHeight)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(TouchManipulationBitmap.CanvasWidth - bottomRight.X, TouchManipulationBitmap.CanvasHeight - bottomRight.Y));
            // right
            else if (bottomRight.X < TouchManipulationBitmap.CanvasWidth)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(TouchManipulationBitmap.CanvasWidth - bottomRight.X, 0));
            // bottom
            else if (bottomRight.Y < TouchManipulationBitmap.CanvasHeight)
                touchMatrix = touchMatrix.PostConcat(SKMatrix.CreateTranslation(0, TouchManipulationBitmap.CanvasHeight - bottomRight.Y));

            return touchMatrix;
        }

        private float Magnitude(SKPoint point)
        {
            return (float)Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2));
        }
    }
}
