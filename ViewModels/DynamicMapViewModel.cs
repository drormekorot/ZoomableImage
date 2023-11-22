using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Forms;

namespace AAA.ViewModels
{
    public class DynamicMapViewModel
    {
        public delegate void MapSelectedCallback();

        readonly IDynamicMapsStore<DynamicMapObj> DynamicMapsStore = DependencyService.Get<IDynamicMapsStore<DynamicMapObj>>();

        readonly MapSelectedCallback mapSelectedCallback;

        public DynamicMapObj SelectedMap { get; set; }

        public DynamicMapPointObj SelectedPoint { get; set; }

        public ObservableCollection<DynamicMapObj> Maps { get; set; }

        public Command<DynamicMapObj> ItemSelected { get; set; }

        public DynamicMapViewModel(MapSelectedCallback mapSelectedCallback)
        {
            Maps = new ObservableCollection<DynamicMapObj>();
            this.mapSelectedCallback = mapSelectedCallback;

            ItemSelected = new Command<DynamicMapObj>(MapSelected);       
        }

        public async Task LoadPrimaryMapAsync()
        {
            IEnumerable<DynamicMapObj> primaryData = await DynamicMapsStore.GetPrimaryItemsAsync(Site);

            foreach (DynamicMapObj map in primaryData)
            {
                if (Maps.SingleOrDefault(x => x.Id == map.Id) is null)
                    Maps.Add(map);
            }

            if (Maps.Count == 0)
                return;

            SelectedMap = Maps[0];
        }

        private void MapSelected(DynamicMapObj item)
        {
            DynamicMapObj selectedMap = Maps.SingleOrDefault(x => x.Id == item.Id);
            
            SelectedMap = selectedMap;
			SelectedMap.IsSelected = true;
			mapSelectedCallback?.Invoke();          
        }

        public async Task RefreshMapAsync()
        {
            if (SelectedMap is null)
            {
                await LoadPrimaryMapAsync();
                return;
            }
        }
    }
}
