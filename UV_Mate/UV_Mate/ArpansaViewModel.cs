using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace UV_Mate
{
    public class ArpansaViewModel : INotifyPropertyChanged
    {
        ArpansaRealtimeFeed arpansaModel;
        string longitude;
        string latitude;

        public event EventHandler<ArpansaUVResponse> ArpansaUpdateEvent;

        bool isBusy;
        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                if (isBusy == value)
                    return;

                isBusy = value;
                OnPropertyChanged("IsBusy");
            }
        }

        public string[] MeasureLocations { get; set; } = new[] { "Cat", "Dog" };

        public ArpansaViewModel()
        {
            this.arpansaModel = new ArpansaRealtimeFeed();
        }

        public async Task<ArpansaUVResponse> UpdateUVData(string longitude = "138.62", string latitude = "-34.92")
        {
            this.longitude = longitude;
            this.latitude = latitude;
            return await arpansaModel.GetUVData(longitude, latitude);
        }

        ICommand refreshCommand;

        public ICommand RefreshCommand
        {
            get { return refreshCommand ?? (refreshCommand = new Command(async () => await ExecuteRefreshCommand())); }
        }

        async Task ExecuteRefreshCommand()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;

            ArpansaUVResponse latestUVData = await this.UpdateUVData(this.longitude, this.latitude);

            //notify controller via an event
            ArpansaUpdateEvent?.Invoke(this, latestUVData);
            IsBusy = false;
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        public List<UVIndex> GenerateUVIndexs()
        {
            //recommend UV indexes as outlined by WHO
            //source: http://www.who.int/uv/publications/en/UVIGuide.pdf
            List<UVIndex> UVIndexes = new List<UVIndex>();

            UVIndexes.Add(new UVIndex(0f, "Low", SKColors.Green));
            UVIndexes.Add(new UVIndex(3f, "Moderate", new SKColor(190, 190, 25))); //yellow
            UVIndexes.Add(new UVIndex(6f, "High", SKColors.Orange));
            UVIndexes.Add(new UVIndex(8f, "Very High", SKColors.Red));
            UVIndexes.Add(new UVIndex(11f, "Extreme", SKColors.Purple));

            return UVIndexes;
        }

        public void GetMeasuredLocations()
        {
            this.arpansaModel.GetValidLocations();
        }
    }
}
