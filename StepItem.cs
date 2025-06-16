using System.ComponentModel;
using System.Windows.Media;

namespace PDFSplitterforCopilot
{
    public class StepItem : INotifyPropertyChanged
    {
        private string _name = "";
        private StepStatus _status;
        private string _message = "";

        public string Name 
        { 
            get => _name; 
            set { _name = value; OnPropertyChanged(nameof(Name)); } 
        }
        
        public StepStatus Status 
        { 
            get => _status; 
            set 
            { 
                _status = value; 
                OnPropertyChanged(nameof(Status)); 
                OnPropertyChanged(nameof(StatusBrush));
            } 
        }
        
        public string Message 
        { 
            get => _message; 
            set { _message = value; OnPropertyChanged(nameof(Message)); } 
        }

        public Brush StatusBrush
        {
            get
            {
                return Status switch
                {
                    StepStatus.Pending => Brushes.LightGray,
                    StepStatus.InProgress => Brushes.SkyBlue,
                    StepStatus.Completed => Brushes.LimeGreen,
                    StepStatus.Error => Brushes.Red,
                    _ => Brushes.LightGray
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
