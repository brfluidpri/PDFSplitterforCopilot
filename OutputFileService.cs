using System.IO;

namespace PDFSplitterforCopilot
{
    internal static class OutputFileService
    {
        public static string GetOutputDirectory(string sourcePath)
        {
            return Path.Combine(Path.GetDirectoryName(sourcePath) ?? "", "output_split");
        }

        public static string GetFixedSplitOutputPath(string pdfPath, int startPage, int endPage)
        {
            string outputDir = GetOutputDirectory(pdfPath);
            string outputFileName = $"{Path.GetFileNameWithoutExtension(pdfPath)}_page{startPage:D2}-{endPage:D2}.pdf";
            return Path.Combine(outputDir, outputFileName);
        }

        public static string GetContextSplitOutputPath(string pdfPath, ContextSplitRange range)
        {
            string outputDir = GetOutputDirectory(pdfPath);
            int startPage = range.IncludedStartPage > 0 ? range.IncludedStartPage : range.StartPage;
            int endPage = range.IncludedEndPage > 0 ? range.IncludedEndPage : range.EndPage;
            string outputFileName = $"{Path.GetFileNameWithoutExtension(pdfPath)}_part{range.Part:D2}_p{startPage:D2}-{endPage:D2}.pdf";
            return Path.Combine(outputDir, outputFileName);
        }

        public static string GetExtractedPdfOutputPath(string pdfPath, int extractedPages)
        {
            string outputDir = GetOutputDirectory(pdfPath);
            string outputFileName = $"{Path.GetFileNameWithoutExtension(pdfPath)}_page01-{extractedPages:D2}.pdf";
            return Path.Combine(outputDir, outputFileName);
        }

        public static string GetRagJsonlOutputPath(string pdfPath)
        {
            return Path.Combine(GetOutputDirectory(pdfPath), $"{Path.GetFileNameWithoutExtension(pdfPath)}_rag_chunks.jsonl");
        }
    }
}
