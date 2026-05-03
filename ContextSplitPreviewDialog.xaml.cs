using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PDFSplitterforCopilot
{
    public partial class ContextSplitPreviewDialog : Window
    {
        private readonly string _sourceBaseName;
        private readonly int _totalPages;
        private readonly int _overlapPages;
        private readonly ObservableCollection<EditableContextSplitRange> _ranges;

        public bool IsConfirmed { get; private set; }
        public IReadOnlyList<ContextSplitRange> AcceptedRanges { get; private set; } = Array.Empty<ContextSplitRange>();

        public ContextSplitPreviewDialog(ContextSplitProposal proposal, int overlapPages)
        {
            InitializeComponent();

            _sourceBaseName = Path.GetFileNameWithoutExtension(proposal.SourceFileName);
            _totalPages = proposal.TotalPages;
            _overlapPages = Math.Max(0, overlapPages);
            _ranges = new ObservableCollection<EditableContextSplitRange>(
                proposal.Ranges.Select(range => new EditableContextSplitRange(range, _sourceBaseName)));

            txtSummary.Text = $"{proposal.SourceFileName} - {proposal.TotalPages} pages, {proposal.Ranges.Count} proposed parts, boundary overlap {_overlapPages} page(s)";
            dgRanges.ItemsSource = _ranges;
            ValidateRanges();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            dgRanges.CommitEdit(DataGridEditingUnit.Cell, true);
            dgRanges.CommitEdit(DataGridEditingUnit.Row, true);

            if (!ValidateRanges())
            {
                return;
            }

            AcceptedRanges = _ranges
                .OrderBy(range => range.StartPage)
                .Select((range, index) => new ContextSplitRange
                {
                    Part = index + 1,
                    StartPage = range.StartPage,
                    EndPage = range.EndPage,
                    IncludedStartPage = range.IncludedStartPage,
                    IncludedEndPage = range.IncludedEndPage,
                    Title = range.SafeTitle,
                    Reason = range.Reason,
                    Confidence = range.Confidence
                })
                .ToList();

            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }

        private void DgRanges_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(ValidateRanges, DispatcherPriority.Background);
        }

        private void DgRanges_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(ValidateRanges, DispatcherPriority.Background);
        }

        private bool ValidateRanges()
        {
            foreach (var range in _ranges)
            {
                range.Warning = string.Empty;
            }

            if (_ranges.Count == 0)
            {
                txtValidation.Text = "At least one range is required.";
                return false;
            }

            var ordered = _ranges.OrderBy(range => range.StartPage).ToList();
            int expectedStart = 1;
            for (int i = 0; i < ordered.Count; i++)
            {
                var range = ordered[i];
                if (range.StartPage < 1 || range.EndPage > _totalPages || range.StartPage > range.EndPage)
                {
                    range.Warning = $"Range must stay within 1-{_totalPages}.";
                }
                else if (range.StartPage != expectedStart)
                {
                    range.Warning = $"Expected start page {expectedStart}.";
                }

                if (range.Confidence < 0.6 && string.IsNullOrWhiteSpace(range.Warning))
                {
                    range.Warning = "Low confidence. Review this range.";
                }

                expectedStart = Math.Max(expectedStart, range.EndPage + 1);
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].SetIncludedRange(
                    i == 0 ? ordered[i].StartPage : Math.Max(1, ordered[i].StartPage - _overlapPages),
                    i == ordered.Count - 1 ? ordered[i].EndPage : Math.Min(_totalPages, ordered[i].EndPage + _overlapPages));
            }

            bool hasBlockingError = ordered.Any(range =>
                range.StartPage < 1
                || range.EndPage > _totalPages
                || range.StartPage > range.EndPage)
                || ordered.Where(range => string.IsNullOrWhiteSpace(range.Warning) || range.Warning.StartsWith("Expected", StringComparison.Ordinal))
                    .Any(range => range.Warning.StartsWith("Expected", StringComparison.Ordinal))
                || ordered.Last().EndPage != _totalPages
                || ordered.First().StartPage != 1;

            if (hasBlockingError)
            {
                txtValidation.Text = $"Fix page ranges so they cover pages 1-{_totalPages} exactly once without gaps or overlaps.";
                return false;
            }

            txtValidation.Text = ordered.Any(range => range.Confidence < 0.6)
                ? "Low-confidence rows are highlighted. You can still create PDFs after reviewing them."
                : string.Empty;
            return true;
        }

        private sealed class EditableContextSplitRange : INotifyPropertyChanged
        {
            private int _startPage;
            private int _endPage;
            private string _title;
            private string _reason;
            private string _warning = string.Empty;
            private string _outputFileName;
            private int _includedStartPage;
            private int _includedEndPage;
            private readonly string _sourceBaseName;

            public EditableContextSplitRange(ContextSplitRange range, string sourceBaseName)
            {
                _sourceBaseName = sourceBaseName;
                Part = range.Part;
                _startPage = range.StartPage;
                _endPage = range.EndPage;
                _title = range.Title;
                _reason = range.Reason;
                Confidence = range.Confidence;
                _includedStartPage = range.IncludedStartPage > 0 ? range.IncludedStartPage : range.StartPage;
                _includedEndPage = range.IncludedEndPage > 0 ? range.IncludedEndPage : range.EndPage;
                _outputFileName = BuildOutputFileName();
            }

            public int Part { get; }

            public int StartPage
            {
                get => _startPage;
                set
                {
                    _startPage = value;
                    OnPropertyChanged(nameof(StartPage));
                    OnPropertyChanged(nameof(SemanticRange));
                    RefreshOutputFileName();
                }
            }

            public int EndPage
            {
                get => _endPage;
                set
                {
                    _endPage = value;
                    OnPropertyChanged(nameof(EndPage));
                    OnPropertyChanged(nameof(SemanticRange));
                    RefreshOutputFileName();
                }
            }

            public string Title
            {
                get => _title;
                set
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                    RefreshOutputFileName();
                }
            }

            public string Reason
            {
                get => _reason;
                set
                {
                    _reason = value;
                    OnPropertyChanged(nameof(Reason));
                }
            }

            public double Confidence { get; }
            public string ConfidenceText => $"{Confidence:P0}";
            public string SemanticRange => $"{StartPage}-{EndPage}";
            public string IncludedRange => $"{IncludedStartPage}-{IncludedEndPage}";
            public bool HasWarning => !string.IsNullOrWhiteSpace(Warning);
            public string SafeTitle => SanitizeTitle(Title);
            public int IncludedStartPage => _includedStartPage;
            public int IncludedEndPage => _includedEndPage;

            public string Warning
            {
                get => _warning;
                set
                {
                    _warning = value;
                    OnPropertyChanged(nameof(Warning));
                    OnPropertyChanged(nameof(HasWarning));
                }
            }

            public string OutputFileName
            {
                get => _outputFileName;
                private set
                {
                    _outputFileName = value;
                    OnPropertyChanged(nameof(OutputFileName));
                }
            }

            public void RefreshOutputFileName()
            {
                OutputFileName = BuildOutputFileName();
            }

            public void SetIncludedRange(int startPage, int endPage)
            {
                _includedStartPage = startPage;
                _includedEndPage = endPage;
                OnPropertyChanged(nameof(IncludedStartPage));
                OnPropertyChanged(nameof(IncludedEndPage));
                OnPropertyChanged(nameof(IncludedRange));
                RefreshOutputFileName();
            }

            private string BuildOutputFileName()
            {
                return $"{_sourceBaseName}_part{Part:D2}_p{IncludedStartPage:D2}-{IncludedEndPage:D2}.pdf";
            }

            private static string SanitizeTitle(string? title)
            {
                string value = string.IsNullOrWhiteSpace(title) ? "Context" : title.Trim();
                foreach (char invalidChar in Path.GetInvalidFileNameChars())
                {
                    value = value.Replace(invalidChar, '_');
                }

                value = Regex.Replace(value, @"\s+", "_");
                return value.Length > 60 ? value.Substring(0, 60) : value;
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
