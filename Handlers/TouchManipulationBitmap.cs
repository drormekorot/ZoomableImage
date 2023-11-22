using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TouchTracking;
using Xamarin.Forms;

namespace AAA.Handlers
{
    public class TouchManipulationBitmap
    {
        private enum AnimationDirection
        {
            In,
            Out,
            Static
        }

        public delegate void InvalidateBitmapRequiredEvent();
        public delegate Task HotspotClickedEvent(string id);
        public delegate void ZoomOccurredEvent(bool status);
        
        public static SKBitmap Bitmap;

        public TouchManipulationManager TouchManager { set; get; }
        public HotspotManager HotspotManager { set; get; }

        public static SKMatrix Matrix { set; get; }

        private readonly Dictionary<long, TouchManipulationInfo> touchDictionary = new Dictionary<long, TouchManipulationInfo>();
        
        private readonly InvalidateBitmapRequiredEvent invalidateBitmapEvent;

        private readonly HotspotClickedEvent hotspotClickedEvent;

        private readonly ZoomOccurredEvent zoomOccurredEvent;

        bool isClickEvent;

        static int touchCount = 0;

        private readonly TouchManipulationInfo lastPressedPoints = new TouchManipulationInfo();

        public static float SCALE_MIN = 1;
        public static float SCALE_MEDIAN = 1;
        public static float SCALE_MAX = 6;

        public static float SCALE_ZOOMED = 1.2f;

        public static float CanvasHeight;
        public static float CanvasWidth;

        private readonly float widthRatio;
        private readonly float heightRatio;

        private bool zoomOccurred;
        public bool ZoomOccurred 
        { 
            get => zoomOccurred; 
            set 
            {
                if (zoomOccurred != value)
                    zoomOccurredEvent?.Invoke(value);
                    
                zoomOccurred = value;
            } 
        }

        public TouchManipulationBitmap(string imageStr, float canvasHeight, float canvasWidth, HotspotClickedEvent hotspotClickedEvent, InvalidateBitmapRequiredEvent invalidateBitmapEvent, ZoomOccurredEvent zoomOccurredEvent)
        {
            Bitmap = SKBitmap.Decode(Convert.FromBase64String(imageStr));
            Matrix = SKMatrix.CreateIdentity();
            
            CanvasHeight = canvasHeight;
            CanvasWidth = canvasWidth;

            widthRatio = CanvasWidth / Bitmap.Width;
            heightRatio = CanvasHeight / Bitmap.Height;

            TouchManager = new TouchManipulationManager();
            HotspotManager = new HotspotManager(HotspotClicked);

            this.hotspotClickedEvent = hotspotClickedEvent;
            this.invalidateBitmapEvent = invalidateBitmapEvent;
            this.zoomOccurredEvent = zoomOccurredEvent;
        }

        public void AddHotspots(List<DynamicMapPointObj> points)
        {
            IsDebugCheck(ref isDebug);

			foreach (DynamicMapPointObj point in points)
			{
				AddHotspot(new Hotspot
				{
					Id = point.PointId,
					Rect = new SKRect((float)point.CoordX * widthRatio, (float)point.CoordY * heightRatio, (float)point.CoordX * widthRatio + (float)point.PointSizeX, (float)point.CoordY * heightRatio + (float)point.PointSizeY)
				});
			}            
        }

        public void Paint(SKImageInfo info, SKCanvas canvas)
        {
            canvas.Save();
            SKMatrix matrix = Matrix;
            canvas.Concat(ref matrix);
            float scale = Math.Min(info.Width / CanvasWidth, info.Height / CanvasHeight);

            float x = (info.Width - scale * CanvasWidth) / 2;
            float y = info.Height - scale * CanvasHeight;

            SKRect destRect = new SKRect(x, y, x + scale * CanvasWidth, y + scale * CanvasHeight);
            canvas.DrawBitmap(Bitmap, destRect);

            bool isDebug = false;
            IsDebugCheck(ref isDebug);

            if (isDebug)
            {
                SKPaint textPaint = new SKPaint
                {
                    Color = SKColors.White
                };

                foreach (Hotspot hotspot in HotspotManager.Hotspots)
                {
                    canvas.DrawText(hotspot.Id, new SKPoint(hotspot.Rect.Left, hotspot.Rect.Top + ((hotspot.Rect.Bottom - hotspot.Rect.Top) / 2)), textPaint);
                    canvas.DrawRect(hotspot.Rect, hotspot.Paint);
                }
            }

            canvas.Restore();
        }

        public void ProcessTouchEvent(long id, TouchActionType type, SKPoint location)
        {
            switch (type)
            {
                case TouchActionType.Pressed:
                    touchDictionary.Add(id, new TouchManipulationInfo
                    {
                        PreviousPoint = location,
                        NewPoint = location
                    });
                    if (touchCount == 0)
                    {
                        TimeSpan tt = new TimeSpan(0, 0, 0, 0, 250);
                        Device.StartTimer(tt, HandleClicked);
                    }
                    touchCount++;
                    isClickEvent = false;
                    lastPressedPoints.PreviousPoint = lastPressedPoints.NewPoint;
                    lastPressedPoints.NewPoint = location;
                    break;

                case TouchActionType.Moved:
                    TouchManipulationInfo info = touchDictionary[id];
                    info.NewPoint = location;
                    Manipulate();
                    info.PreviousPoint = info.NewPoint;
                    break;

                case TouchActionType.Released:                    
                    isClickEvent = true;
                    touchDictionary[id].NewPoint = location;
                    Manipulate();
                    touchDictionary.Remove(id);
                    break;

                case TouchActionType.Cancelled:
                    touchDictionary.Remove(id);
                    break;
            }
        }

        private void AddHotspot(Hotspot hotspot)
        {
            HotspotManager.AddHotspot(hotspot);
        }

        private void Manipulate()
        {
            TouchManipulationInfo[] infos = new TouchManipulationInfo[touchDictionary.Count];
            touchDictionary.Values.CopyTo(infos, 0);
            SKMatrix touchMatrix = SKMatrix.CreateIdentity();

            if (infos.Length == 1)
            {
                SKPoint prevPoint = infos[0].PreviousPoint;
                SKPoint newPoint = infos[0].NewPoint;

                touchMatrix = TouchManager.OneFingerManipulate(prevPoint, newPoint);
            }
            else if (infos.Length >= 2)
            {
                int pivotIndex = infos[0].NewPoint == infos[0].PreviousPoint ? 0 : 1;
                SKPoint pivotPoint = infos[pivotIndex].NewPoint;
                SKPoint newPoint = infos[1 - pivotIndex].NewPoint;
                SKPoint prevPoint = infos[1 - pivotIndex].PreviousPoint;

                touchMatrix = TouchManager.TwoFingerManipulate(prevPoint, newPoint, pivotPoint);
            }

            SKMatrix matrix = Matrix;
            matrix = matrix.PostConcat(touchMatrix);
            Matrix = matrix;

            ZoomOccurred = Matrix.ScaleX > SCALE_ZOOMED;
        }

        private bool HandleClicked()
        {
            if (!isClickEvent || touchCount == 0)
            {
                touchCount = 0;
                return false;
            }

            if (touchCount == 1)
            {                
                HotspotManager.InvokeHotspotClicked(lastPressedPoints.NewPoint, Matrix);
            }
            else if (touchCount >= 2)
            {
                // Calculate the distance between the start of the press and the moved point 
                var delta = lastPressedPoints.NewPoint - lastPressedPoints.PreviousPoint;

                // If the distance is larger than a threshold the touchpoints are dragging and not double tap
                if (delta.LengthSquared < 1000.0f)
                {
                    SKPoint newTranslatedPoint = lastPressedPoints.NewPoint;
                    float endScale = Matrix.ScaleX < SCALE_MAX ? SCALE_MAX : SCALE_MIN;

                    AnimateMovement(newTranslatedPoint, endScale == SCALE_MAX ? AnimationDirection.In : AnimationDirection.Out);
                }
            }

            touchCount = 0;
            return false;
        }

        private void HotspotClicked(string id, SKPoint hotspotCenterPoint)
        {
            AnimateMovement(hotspotCenterPoint, Matrix.ScaleX >= SCALE_MAX ? AnimationDirection.Static : AnimationDirection.In);
            hotspotClickedEvent?.Invoke(id);
        }

        private void AnimateMovement(SKPoint point, AnimationDirection animationDirection)
        {
            float scale = animationDirection == AnimationDirection.In ? 1.1f : animationDirection == AnimationDirection.Out ? 0.9f : 1.0f;
            int frames = 0;

            Device.StartTimer(TimeSpan.FromMilliseconds(1), () =>
            {
                frames++;
                SKMatrix autoMatrix = SKMatrix.CreateIdentity();

                if (animationDirection == AnimationDirection.In)
                {
                    autoMatrix = SKMatrix.CreateIdentity();
                    if (Matrix.ScaleX >= SCALE_MAX)
                    {
                        ZoomOccurred = Matrix.ScaleX > SCALE_ZOOMED;
                        return false;
                    }
                        
                    autoMatrix = TouchManager.AutoManipulation(point, scale);
                }

                if (animationDirection == AnimationDirection.Out)
                {
                    autoMatrix = Matrix;
                    if (Matrix.ScaleX <= SCALE_MIN)
                    {
                        ZoomOccurred = Matrix.ScaleX > SCALE_ZOOMED;
                        return false;
                    }

                    autoMatrix = TouchManager.AutoManipulation(point, scale);
                }

                SKMatrix matrix = Matrix;
                matrix = matrix.PostConcat(autoMatrix);
                Matrix = matrix;
                invalidateBitmapEvent?.Invoke();
                return true;
            });
        }
    }
}
