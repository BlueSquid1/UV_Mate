using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace UV_Mate
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LocationPage : ContentPage
	{
        private ArpansaViewModel arpansaModel;
        private ArpansaRealtimeFeed arpansaService;

        public LocationPage (ArpansaViewModel mApransaModel, ArpansaRealtimeFeed mArpansaService)
		{
			InitializeComponent();

            this.arpansaModel = mApransaModel;
            this.arpansaService = mArpansaService;

            this.BindingContext = this.arpansaModel;

            this.arpansaModel.PropertyChanged += ArpansaModel_PropertyChanged;
        }

        private void ArpansaModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            
            if(e.PropertyName == "MeasureLocations")
            {
                this.locPicker.IsEnabled = true;
                this.loadingLabel.IsVisible = false;
            }
            else if(e.PropertyName == "LocIndexValue" && this.arpansaModel.MeasureLocations != null)
            {
                if (arpansaModel.LocIndexValue < 0 || arpansaModel.LocIndexValue >= arpansaModel.MeasureLocations.Count)
                {
                    Console.WriteLine("Invalid location selected");
                    return;
                }
                //save the new selected location
                Preferences.Set("LocIndexValue", arpansaModel.LocIndexValue.Value);
                MeasuredLocation selectedLoc = arpansaModel.MeasureLocations[arpansaModel.LocIndexValue.Value];

                //update state field
                this.state.Text = selectedLoc.CategoryName;
                this.stateStack.IsVisible = true;
                
                //update latitude field
                this.lat.Text = selectedLoc.SiteLatitude.ToString();
                this.latStack.IsVisible = true;

                //update longitude field
                this.longitude.Text = selectedLoc.SiteLongitude.ToString();
                this.longStack.IsVisible = true;
            }
            
        }
    }
}