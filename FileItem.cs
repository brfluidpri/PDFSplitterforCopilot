// FileItem.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace PDFSplitterforCopilot
{    public class FileItem : INotifyPropertyChanged
    {
        private string _fileName = "";
        private string _filePath = "";
        private string _pageCount = "";
        private Brush _statusColor = Brushes.LightGray;
        private string _statusMessage = "";
        private bool _isSelected = true; // 기본값을 true로 설정
        private int _progress;
        private int _currentStep;
        private int _totalSteps;
        private string _stepMessage = "";
        private ObservableCollection<StepItem> _steps = new ObservableCollection<StepItem>();
        private double _blankPageRatio;

        public FileItem()
        {
            // 기본적으로 선택되도록 설정
            _isSelected = true;
        }

        public string FileName { get => _fileName; set { _fileName = value; OnPropertyChanged(nameof(FileName)); } }
        public string FilePath { get => _filePath; set { _filePath = value; OnPropertyChanged(nameof(FilePath)); } }
        public string PageCount { get => _pageCount; set { _pageCount = value; OnPropertyChanged(nameof(PageCount)); } }
        public Brush StatusColor { get => _statusColor; set { _statusColor = value; OnPropertyChanged(nameof(StatusColor)); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
        public int Progress { get => _progress; private set { _progress = value; OnPropertyChanged(nameof(Progress)); } }
        public int CurrentStep { get => _currentStep; set { _currentStep = value; OnPropertyChanged(nameof(CurrentStep)); UpdateProgress(); } }
        public int TotalSteps { get => _totalSteps; set { _totalSteps = value; OnPropertyChanged(nameof(TotalSteps)); UpdateProgress(); } }
        public string StepMessage { get => _stepMessage; set { _stepMessage = value; OnPropertyChanged(nameof(StepMessage)); } }
        public ObservableCollection<StepItem> Steps { get => _steps; set { _steps = value; OnPropertyChanged(nameof(Steps)); } }
        public double BlankPageRatio { get => _blankPageRatio; set { _blankPageRatio = value; OnPropertyChanged(nameof(BlankPageRatio)); OnPropertyChanged(nameof(BlankPageRatioText)); } }

        public string BlankPageRatioText => $"{BlankPageRatio:P1}";

        public void UpdateStep(int stepIndex, string message, StepStatus status)
        {
            if (stepIndex >= 0 && stepIndex < Steps.Count)
            {
                Steps[stepIndex].Status = status;
                Steps[stepIndex].Message = message;
                CurrentStep = stepIndex + 1;
                StepMessage = message;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void UpdateProgress()
        {
            Progress = TotalSteps > 0 ? (int)Math.Round(CurrentStep / (double)TotalSteps * 100) : 0;
        }
    }
}
