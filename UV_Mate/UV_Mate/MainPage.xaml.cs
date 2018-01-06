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
        private GraphPage graphPage;
        public MainPage ()
        {
            InitializeComponent();

            ArpansaViewModel arpansaModel = new ArpansaViewModel();
            ArpansaRealtimeFeed arpansaService = new ArpansaRealtimeFeed();
            this.graphPage = new GraphPage(arpansaModel, arpansaService);
            this.Children.Add(this.graphPage);
            this.Children.Add(new LocationPage(arpansaModel, arpansaService));

            this.CurrentPageChanged += MainPage_CurrentPageChanged;
        }

        private void MainPage_CurrentPageChanged(object sender, EventArgs e)
        {
            // refresh UV data when navigate between views
            this.graphPage.IsAppearing = true;
        }
    }
}