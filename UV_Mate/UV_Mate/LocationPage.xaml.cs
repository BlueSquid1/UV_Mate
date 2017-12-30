using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace UV_Mate
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class LocationPage : ContentPage
	{
        ArpansaViewModel arpansaModel;
        ArpansaRealtimeFeed arpansaService;

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
                Console.WriteLine("handlinf the MeasureLocations binding");
                this.locPicker.IsEnabled = true;
                this.loadingLabel.IsVisible = false;
            }
            else if(e.PropertyName == "LocIndexValue" && this.arpansaModel.MeasureLocations != null)
            {
                Console.WriteLine("handling the LocIndexValue binding");
                if (arpansaModel.LocIndexValue < 0 || arpansaModel.LocIndexValue >= arpansaModel.MeasureLocations.Count)
                {
                    Console.WriteLine("Invalid LocIndexValue");
                    return;
                }
                MeasuredLocation selectedLoc = arpansaModel.MeasureLocations[arpansaModel.LocIndexValue];

                //State
                this.state.Text = selectedLoc.CategoryName;
                this.stateStack.IsVisible = true;

                
                //latitude
                this.lat.Text = selectedLoc.SiteLatitude.ToString();
                this.latStack.IsVisible = true;

                //longitude
                this.longitude.Text = selectedLoc.SiteLongitude.ToString();
                this.longStack.IsVisible = true;
                
            }
            
        }
    }
}