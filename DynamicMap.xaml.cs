using Plugin.DeviceOrientation;
using Rg.Plugins.Popup.Services;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TouchTracking;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace AAA.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DynamicMap : ContentPage
    {
        private TouchManipulationBitmap touchManipulationBitmap;
        readonly DynamicMapViewModel ViewModel;
        private readonly List<long> touchIds = new List<long>();

        public DynamicMap()
        {
            InitializeComponent();
            ViewModel = new DynamicMapViewModel(InitializeMap);
        }

        protected override async void OnAppearing()
        {
            CrossDeviceOrientation.Current.LockOrientation(Plugin.DeviceOrientation.Abstractions.DeviceOrientations.Landscape);

            if (ViewModel.SelectedMap == null)
            {
                await ViewModel.LoadPrimaryMapAsync();

                Button_MoreMaps.IsVisible = ViewModel.Maps.Count > 1;

                InitializeMap();
            }
        }

        private async void InitializeMap()
        {
            if (ViewModel.SelectedMap != null)
            {
                touchManipulationBitmap = new TouchManipulationBitmap(ViewModel.SelectedMap.Image, CanvasView_Map.CanvasSize.Height, CanvasView_Map.CanvasSize.Width, HotspotClicked, RefreshMap, ToggleButtonsView);
                touchManipulationBitmap.AddHotspots(ViewModel.SelectedMap.Points);
                Label_MapName.Text = ViewModel.SelectedMap.Name;

                CanvasView_Map.InvalidateSurface();

                if (PopupNavigation.Instance.PopupStack.Any())
                    await PopupNavigation.Instance.PopAsync();
            }
        }

        private async Task HotspotClicked(string id)
        {
            ViewModel.SelectedPoint = ViewModel.SelectedMap.Points.First(x => x.PointId == id);

            // do something with ViewModel.SelectedPoint
        }

        private void RefreshMap()
        {
            CanvasView_Map.InvalidateSurface();
        }

        private void ToggleButtonsView(bool zoomed)
        {
            Animation animation = new Animation();

            if (zoomed)
            {
                animation.Add(0, 1, new Animation(x => Button_Back.TranslationX = x, 0, -100, easing: Easing.SpringOut));
                animation.Add(0, 1, new Animation(x => Button_Refresh.TranslationX = x, 0, 100, easing: Easing.SpringOut));
                animation.Add(0, 1, new Animation(x => Button_MoreMaps.TranslationX = x, 0, 100, easing: Easing.SpringOut));
                animation.Add(0, 1, new Animation(x => Label_MapName.TranslationY = x, 0, -100, easing: Easing.SpringOut));
            }
            else
            {
                animation.Add(0, 1, new Animation(x => Button_Back.TranslationX = x, -100, 0, easing: Easing.SpringOut));
                animation.Add(0, 1, new Animation(x => Button_Refresh.TranslationX = x, 100, 0, easing: Easing.SpringOut));
                animation.Add(0, 1, new Animation(x => Button_MoreMaps.TranslationX = x, 100, 0, easing: Easing.SpringOut));
                animation.Add(0, 1, new Animation(x => Label_MapName.TranslationY = x, -100, 0, easing: Easing.SpringOut));
            }

            animation.Commit(this, "ButtonsAnim");
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (touchManipulationBitmap == null)
                return;

            SKImageInfo info = e.Info;
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear();
            
            // Display the bitmap
            touchManipulationBitmap.Paint(info, canvas);
        }

        private void OnTouchEffectAction(object sender, TouchActionEventArgs e)
        {
            // Convert Xamarin.Forms point to pixels
            TouchTrackingPoint pt = e.Location;
            SKPoint point = new SKPoint((float)(CanvasView_Map.CanvasSize.Width * pt.X / CanvasView_Map.Width),
                            (float)(CanvasView_Map.CanvasSize.Height * pt.Y / CanvasView_Map.Height));
            switch (e.Type)
            {
                case TouchActionType.Pressed:
                    
                    touchIds.Add(e.Id);
                    touchManipulationBitmap.ProcessTouchEvent(e.Id, e.Type, point);
                    break;

                case TouchActionType.Moved:
                    if (touchIds.Contains(e.Id))
                    {
                        touchManipulationBitmap.ProcessTouchEvent(e.Id, e.Type, point);
                        CanvasView_Map.InvalidateSurface();
                    }

                    break;

                case TouchActionType.Released:
                case TouchActionType.Cancelled:
                    if (touchIds.Contains(e.Id))
                    {
                        touchManipulationBitmap.ProcessTouchEvent(e.Id, e.Type, point);
                        touchIds.Remove(e.Id);
                        CanvasView_Map.InvalidateSurface();
                    }
                    break;
            }
        }

        private async void Button_Back_Clicked(object sender, EventArgs e)
        {
            CrossDeviceOrientation.Current.LockOrientation(Plugin.DeviceOrientation.Abstractions.DeviceOrientations.Portrait);
            await Navigation.PopAsync();
        }

        private async void Button_Refresh_Clicked(object sender, EventArgs e)
        {
            await ViewModel.RefreshMapAsync();
            CanvasView_Map.InvalidateSurface();
        }

        private async void Button_Menu_Clicked(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PushAsync(new DynamicMapMenu(ViewModel));
        }
    }
}