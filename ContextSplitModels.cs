using System.Collections.Generic;

namespace PDFSplitterforCopilot
{
    public sealed class ContextSplitRange
    {
        public required int Part { get; init; }
        public required int StartPage { get; init; }
        public required int EndPage { get; init; }
        public int IncludedStartPage { get; init; }
        public int IncludedEndPage { get; init; }
        public required string Title { get; init; }
        public required string Reason { get; init; }
        public required double Confidence { get; init; }

        public string PageRange => $"{StartPage}-{EndPage}";
        public string IncludedPageRange => IncludedStartPage > 0 && IncludedEndPage > 0
            ? $"{IncludedStartPage}-{IncludedEndPage}"
            : PageRange;
        public string ConfidenceText => $"{Confidence:P0}";
    }

    public sealed class ContextSplitProposal
    {
        public required string SourceFileName { get; init; }
        public required int TotalPages { get; init; }
        public required IReadOnlyList<ContextSplitRange> Ranges { get; init; }
    }
}
