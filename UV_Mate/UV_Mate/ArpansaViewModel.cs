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
        //async event
        public event Func<object, EventArgs, Task> ArpansaUpdateEvent;

        private bool isBusy;
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

        private List<MeasuredLocation> measuredLocations = null;
        public List<MeasuredLocation> MeasureLocations
        {
            get
            {
                return measuredLocations;
            }

            set
            {
                if (this.measuredLocations == value)
                {
                    return;
                }
                this.measuredLocations = value;
                OnPropertyChanged("MeasureLocations");
            }
        }

        private int locIndexValue = -1;
        public int LocIndexValue
        {
            get
            {
                return this.locIndexValue;
            }
            set
            {
                this.locIndexValue = value;
                OnPropertyChanged("LocIndexValue");
            }
        }

        //storage place for graph data
        private ArpansaUVData arpansaUVData;
        public ArpansaUVData ArpansaUVData
        {
            get
            {
                return this.arpansaUVData;
            }
            set
            {
                if (this.arpansaUVData == value)
                {
                    return;
                }
                this.arpansaUVData = value;
                OnPropertyChanged("ArpansaUVData");
            }
        }

        private ICommand refreshCommand;
        public ICommand RefreshCommand
        {
            get
            {
                if( this.refreshCommand == null )
                {
                    this.refreshCommand = new Command( async () => await ExecuteRefreshCommand());
                }
                return this.refreshCommand;
            }
        }

        private async Task ExecuteRefreshCommand()
        {
            if (IsBusy == true)
            {
                //already busy. Cancel event
                return;
            }

            IsBusy = true;

            //fire an async event
            Delegate[] invocationList = ArpansaUpdateEvent.GetInvocationList();
            Task[] handlerTasks = new Task[invocationList.Length];

            for (int i = 0; i < invocationList.Length; i++)
            {
                handlerTasks[i] = ((Func<object, EventArgs, Task>)invocationList[i])(this, EventArgs.Empty);
            }
            await Task.WhenAll(handlerTasks);

            IsBusy = false;
        }


        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
                return;


            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ArpansaUVData : ArpansaUVResponse
    {
        public List<UVIndex> ReferenceUVs { get; set; }

        public ArpansaUVData(ArpansaUVResponse arpansaResponse)
        {
            this.CurrentDateTime = arpansaResponse.CurrentDateTime;
            this.CurrentUVIndex = arpansaResponse.CurrentUVIndex;
            this.GraphData = arpansaResponse.GraphData;
            this.id = arpansaResponse.id;
            this.MaximumUVLevel = arpansaResponse.MaximumUVLevel;
            this.MaximumUVLevelDateTime = arpansaResponse.MaximumUVLevelDateTime;
            this.TableData = arpansaResponse.TableData;

            ReferenceUVs = null;
        }
    }
}