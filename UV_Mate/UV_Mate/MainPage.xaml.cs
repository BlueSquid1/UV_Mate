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
    public partial class MainPage : TabbedPage
    {
        public MainPage ()
        {
            InitializeComponent();

            ArpansaViewModel arpansaModel = new ArpansaViewModel();
            ArpansaRealtimeFeed arpansaService = new ArpansaRealtimeFeed();

            this.Children.Add(new GraphPage(arpansaModel, arpansaService));
            this.Children.Add(new LocationPage(arpansaModel, arpansaService));
        }
    }
}