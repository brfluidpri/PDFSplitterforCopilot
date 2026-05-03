using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PDFSplitterforCopilot
{
    internal static class RagChunkExportService
    {
        private const int ParentTargetTokens = 1600;
        private const int ParentMaxTokens = 2000;
        private const int ChildTargetTokens = 400;
        private const double ChildOverlapRatio = 0.20;
        private const string ChunkPolicyVersion = "rag-jsonl-v1";

        private static readonly Regex HeadingPattern = new(
            @"^((\d+(\.\d+)*[.)]?)|([IVXLC]+[.)])|(Chapter|Section|Article|Appendix)\s+\d+|제\s*\d+\s*(장|절|조))\s+.+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string ExportPdfChunks(string pdfPath, string outputDirectory, int? pageStart = null, int? pageEnd = null)
        {
            Directory.CreateDirectory(outputDirectory);

            string sourceFileName = Path.GetFileName(pdfPath);
            string sourceFileSha256 = ComputeFileSha256(pdfPath);
            string createdAtUtc = DateTime.UtcNow.ToString("O");

            using var reader = new PdfReader(pdfPath);
            using var pdfDoc = new PdfDocument(reader);

            int firstPage = Math.Max(1, pageStart ?? 1);
            int lastPage = Math.Min(pdfDoc.GetNumberOfPages(), pageEnd ?? pdfDoc.GetNumberOfPages());
            string baseName = Path.GetFileNameWithoutExtension(pdfPath);
            string outputPath = Path.Combine(outputDirectory, $"{baseName}_rag_chunks.jsonl");

            if (IsExistingExportCurrent(outputPath, sourceFileSha256, firstPage, lastPage))
            {
                return outputPath;
            }

            var blocks = ExtractBlocks(pdfDoc, firstPage, lastPage);
            var parents = BuildParentChunks(baseName, blocks);
            var records = BuildJsonlRecords(parents, baseName, sourceFileName, sourceFileSha256, firstPage, lastPage, createdAtUtc);

            using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));

            foreach (var record in records)
            {
                writer.WriteLine(JsonSerializer.Serialize(record));
            }

            return outputPath;
        }

        private static List<TextBlock> ExtractBlocks(PdfDocument pdfDoc, int firstPage, int lastPage)
        {
            var blocks = new List<TextBlock>();
            var sectionPath = new List<string>();

            for (int pageNumber = firstPage; pageNumber <= lastPage; pageNumber++)
            {
                string pageText = PdfTextExtractor.GetTextFromPage(
                    pdfDoc.GetPage(pageNumber),
                    new SimpleTextExtractionStrategy());

                foreach (string paragraph in SplitParagraphs(pageText))
                {
                    if (IsHeading(paragraph))
                    {
                        UpdateSectionPath(sectionPath, paragraph);
                    }

                    blocks.Add(new TextBlock
                    {
                        Text = paragraph,
                        PageStart = pageNumber,
                        PageEnd = pageNumber,
                        SectionPath = sectionPath.ToArray(),
                        IsHeading = IsHeading(paragraph)
                    });
                }
            }

            return blocks;
        }

        private static IEnumerable<string> SplitParagraphs(string pageText)
        {
            if (string.IsNullOrWhiteSpace(pageText))
            {
                yield break;
            }

            var current = new StringBuilder();
            string[] lines = pageText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            foreach (string rawLine in lines)
            {
                string line = Regex.Replace(rawLine.Trim(), @"\s+", " ");
                if (line.Length == 0)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString().Trim();
                        current.Clear();
                    }

                    continue;
                }

                if (LooksLikeTableLine(line))
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString().Trim();
                        current.Clear();
                    }

                    yield return line;
                    continue;
                }

                if (current.Length > 0)
                {
                    current.Append(' ');
                }

                current.Append(line);
            }

            if (current.Length > 0)
            {
                yield return current.ToString().Trim();
            }
        }

        private static bool LooksLikeTableLine(string line)
        {
            int separators = line.Count(c => c == '|' || c == '\t');
            int longSpaces = Regex.Matches(line, @"\s{2,}").Count;
            return separators >= 2 || longSpaces >= 2;
        }

        private static bool IsHeading(string text)
        {
            if (text.Length > 180)
            {
                return false;
            }

            if (HeadingPattern.IsMatch(text))
            {
                return true;
            }

            bool hasSentencePunctuation = text.EndsWith(".", StringComparison.Ordinal)
                || text.EndsWith(";", StringComparison.Ordinal)
                || text.EndsWith(",", StringComparison.Ordinal);
            return text.Length <= 80
                && !hasSentencePunctuation
                && text.Count(char.IsLetterOrDigit) >= 4
                && text == text.ToUpperInvariant();
        }

        private static void UpdateSectionPath(List<string> sectionPath, string heading)
        {
            int depth = GetHeadingDepth(heading);
            while (sectionPath.Count >= depth)
            {
                sectionPath.RemoveAt(sectionPath.Count - 1);
            }

            sectionPath.Add(heading);
        }

        private static int GetHeadingDepth(string heading)
        {
            var match = Regex.Match(heading, @"^\d+(\.\d+)*");
            if (!match.Success)
            {
                return 1;
            }

            return Math.Min(match.Value.Count(c => c == '.') + 1, 6);
        }

        private static List<ParentChunk> BuildParentChunks(string documentId, IReadOnlyList<TextBlock> blocks)
        {
            var parents = new List<ParentChunk>();
            var current = new List<TextBlock>();

            foreach (var block in blocks)
            {
                int currentTokens = EstimateTokens(current.Select(item => item.Text));
                int blockTokens = EstimateTokens(block.Text);
                bool startsNewSection = block.IsHeading && current.Count > 0 && currentTokens >= ParentTargetTokens / 2;
                bool exceedsMax = current.Count > 0 && currentTokens + blockTokens > ParentMaxTokens;

                if (startsNewSection || exceedsMax)
                {
                    parents.Add(CreateParentChunk(documentId, parents.Count + 1, current));
                    current.Clear();
                }

                current.Add(block);
            }

            if (current.Count > 0)
            {
                parents.Add(CreateParentChunk(documentId, parents.Count + 1, current));
            }

            return parents;
        }

        private static ParentChunk CreateParentChunk(string documentId, int ordinal, IReadOnlyList<TextBlock> blocks)
        {
            string parentId = $"{documentId}:parent:{ordinal:D4}";
            return new ParentChunk
            {
                ChunkId = parentId,
                Text = string.Join(Environment.NewLine + Environment.NewLine, blocks.Select(block => block.Text)),
                PageStart = blocks.Min(block => block.PageStart),
                PageEnd = blocks.Max(block => block.PageEnd),
                SectionPath = blocks.LastOrDefault(block => block.SectionPath.Length > 0)?.SectionPath ?? Array.Empty<string>(),
                Children = BuildChildChunks(documentId, parentId, blocks, ordinal)
            };
        }

        private static List<ChildChunk> BuildChildChunks(string documentId, string parentId, IReadOnlyList<TextBlock> blocks, int parentOrdinal)
        {
            var children = new List<ChildChunk>();
            var current = new List<TextBlock>();
            var overlap = new List<TextBlock>();

            foreach (var block in blocks)
            {
                int currentTokens = EstimateTokens(current.Select(item => item.Text));
                int blockTokens = EstimateTokens(block.Text);

                if (current.Count > 0 && currentTokens + blockTokens > ChildTargetTokens)
                {
                    children.Add(CreateChildChunk(documentId, parentId, parentOrdinal, children.Count + 1, current));
                    overlap = TakeOverlap(current, ChildTargetTokens);
                    current = new List<TextBlock>(overlap);
                }

                current.Add(block);
            }

            if (current.Count > 0 && current.Except(overlap).Any())
            {
                children.Add(CreateChildChunk(documentId, parentId, parentOrdinal, children.Count + 1, current));
            }

            return children;
        }

        private static List<TextBlock> TakeOverlap(IReadOnlyList<TextBlock> blocks, int targetTokens)
        {
            int overlapTarget = Math.Max(1, (int)Math.Round(targetTokens * ChildOverlapRatio));
            var selected = new List<TextBlock>();
            int tokens = 0;

            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                selected.Insert(0, blocks[i]);
                tokens += EstimateTokens(blocks[i].Text);
                if (tokens >= overlapTarget)
                {
                    break;
                }
            }

            return selected;
        }

        private static ChildChunk CreateChildChunk(string documentId, string parentId, int parentOrdinal, int childOrdinal, IReadOnlyList<TextBlock> blocks)
        {
            return new ChildChunk
            {
                ChunkId = $"{documentId}:child:{parentOrdinal:D4}:{childOrdinal:D3}",
                ParentId = parentId,
                Text = string.Join(Environment.NewLine + Environment.NewLine, blocks.Select(block => block.Text)),
                PageStart = blocks.Min(block => block.PageStart),
                PageEnd = blocks.Max(block => block.PageEnd),
                SectionPath = blocks.LastOrDefault(block => block.SectionPath.Length > 0)?.SectionPath ?? Array.Empty<string>()
            };
        }

        private static IEnumerable<object> BuildJsonlRecords(
            IEnumerable<ParentChunk> parents,
            string documentId,
            string sourceFileName,
            string sourceFileSha256,
            int exportPageStart,
            int exportPageEnd,
            string createdAtUtc)
        {
            foreach (var parent in parents)
            {
                yield return new
                {
                    doc_id = documentId,
                    chunk_id = parent.ChunkId,
                    chunk_type = "parent",
                    parent_id = (string?)null,
                    text = parent.Text,
                    chunk_hash = ComputeStringSha256(parent.Text),
                    chunk_policy_version = ChunkPolicyVersion,
                    source_file = sourceFileName,
                    source_file_sha256 = sourceFileSha256,
                    token_estimate = EstimateTokens(parent.Text),
                    section_path = parent.SectionPath,
                    source_page_start = parent.PageStart,
                    source_page_end = parent.PageEnd,
                    export_page_start = exportPageStart,
                    export_page_end = exportPageEnd,
                    created_at_utc = createdAtUtc,
                    retrieval_policy = new
                    {
                        role = "answer_context",
                        target_tokens = ParentTargetTokens,
                        max_tokens = ParentMaxTokens
                    }
                };

                foreach (var child in parent.Children)
                {
                    yield return new
                    {
                        doc_id = documentId,
                        chunk_id = child.ChunkId,
                        chunk_type = "child",
                        parent_id = child.ParentId,
                        text = child.Text,
                        chunk_hash = ComputeStringSha256(child.Text),
                        chunk_policy_version = ChunkPolicyVersion,
                        source_file = sourceFileName,
                        source_file_sha256 = sourceFileSha256,
                        token_estimate = EstimateTokens(child.Text),
                        section_path = child.SectionPath,
                        source_page_start = child.PageStart,
                        source_page_end = child.PageEnd,
                        export_page_start = exportPageStart,
                        export_page_end = exportPageEnd,
                        created_at_utc = createdAtUtc,
                        retrieval_policy = new
                        {
                            role = "precision_match",
                            target_tokens = ChildTargetTokens,
                            overlap_ratio = ChildOverlapRatio,
                            recommended_pipeline = "query_rewrite_once + hybrid_bm25_vector_top_50 + rerank_top_10 + parent_expansion"
                        }
                    };
                }
            }
        }

        private static bool IsExistingExportCurrent(string outputPath, string sourceFileSha256, int exportPageStart, int exportPageEnd)
        {
            if (!File.Exists(outputPath))
            {
                return false;
            }

            string? firstLine = File.ReadLines(outputPath).FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
            if (string.IsNullOrWhiteSpace(firstLine))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(firstLine);
                JsonElement root = document.RootElement;
                return HasString(root, "source_file_sha256", sourceFileSha256)
                    && HasString(root, "chunk_policy_version", ChunkPolicyVersion)
                    && HasInt(root, "export_page_start", exportPageStart)
                    && HasInt(root, "export_page_end", exportPageEnd);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool HasString(JsonElement root, string propertyName, string expected)
        {
            return root.TryGetProperty(propertyName, out JsonElement value)
                && value.ValueKind == JsonValueKind.String
                && string.Equals(value.GetString(), expected, StringComparison.Ordinal);
        }

        private static bool HasInt(JsonElement root, string propertyName, int expected)
        {
            return root.TryGetProperty(propertyName, out JsonElement value)
                && value.TryGetInt32(out int actual)
                && actual == expected;
        }

        private static string ComputeFileSha256(string path)
        {
            using var stream = File.OpenRead(path);
            byte[] hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string ComputeStringSha256(string text)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static int EstimateTokens(IEnumerable<string> texts)
        {
            return texts.Sum(EstimateTokens);
        }

        private static int EstimateTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            int cjkCharacters = text.Count(ch => ch >= 0xAC00 && ch <= 0xD7A3);
            int nonWhitespaceCharacters = text.Count(ch => !char.IsWhiteSpace(ch));
            return Math.Max(1, (int)Math.Ceiling((nonWhitespaceCharacters - cjkCharacters) / 4.0 + cjkCharacters / 2.0));
        }

        private sealed class TextBlock
        {
            public required string Text { get; init; }
            public required int PageStart { get; init; }
            public required int PageEnd { get; init; }
            public required string[] SectionPath { get; init; }
            public required bool IsHeading { get; init; }
        }

        private sealed class ParentChunk
        {
            public required string ChunkId { get; init; }
            public required string Text { get; init; }
            public required int PageStart { get; init; }
            public required int PageEnd { get; init; }
            public required string[] SectionPath { get; init; }
            public required List<ChildChunk> Children { get; init; }
        }

        private sealed class ChildChunk
        {
            public required string ChunkId { get; init; }
            public required string ParentId { get; init; }
            public required string Text { get; init; }
            public required int PageStart { get; init; }
            public required int PageEnd { get; init; }
            public required string[] SectionPath { get; init; }
        }
    }
}
