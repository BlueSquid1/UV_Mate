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

        public LocationPage (ArpansaViewModel mApransaModel)
		{
			InitializeComponent();

            this.arpansaModel = mApransaModel;

            this.BindingContext = this.arpansaModel;

            this.Appearing += LocationPage_Appearing;
        }

        private void LocationPage_Appearing(object sender, EventArgs e)
        {
            
        }
    }
}