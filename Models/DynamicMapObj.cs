using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AAA.Models
{
    public class DynamicMapObj : BaseViewModel
    {
        private bool isReady;
        private bool isSelected;

        public string Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }        
        public Timestamp LastUpdated { get; set; }
        public DateTime LastUpdatedDateTime { get { return LastUpdated.ToDateTime(); } }
        public List<DynamicMapPointObj> Points { get; set; }

        public bool IsReady { get { return isReady; } set { SetProperty(ref isReady, value); } }
        public bool IsSelected { get { return isSelected; } set { SetProperty(ref isSelected, value); } }
    }
}
