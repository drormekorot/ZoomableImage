using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace AAA.Handlers
{
    public class HotspotManager
    {
        public delegate void MapClickedEvent(string id, SKPoint hotspotCenterPoint);

        public List<Hotspot> Hotspots { get; set; }

        private readonly MapClickedEvent mapClickedEvent;

        public HotspotManager(MapClickedEvent mapClickedEvent)
        {
            Hotspots = new List<Hotspot>();
            this.mapClickedEvent = mapClickedEvent;
        }

        public void AddHotspot(Hotspot hotspot)
        {
            Hotspots.Add(hotspot);
        }

        public void InvokeHotspotClicked(SKPoint point, SKMatrix matrix)
        {
            foreach (Hotspot hotspot in Hotspots)
            {                
                SKRect rect = matrix.MapRect(hotspot.Rect);

                if (rect.Contains(point))
                {
                    mapClickedEvent?.Invoke(hotspot.Id, new SKPoint(rect.Left + ((rect.Right - rect.Left) / 2), rect.Top + ((rect.Bottom - rect.Top) / 2)));
                    return;
                }
            }            
        }
    }
}
