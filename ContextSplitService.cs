using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PDFSplitterforCopilot
{
    internal static class ContextSplitService
    {
        private const int MaxPreviewCharactersPerPage = 1800;
        private const string DefaultModel = "gpt-4o-mini";

        private static readonly HttpClient HttpClient = new()
        {
            BaseAddress = new Uri("https://api.openai.com/v1/")
        };

        private static readonly Regex HeadingPattern = new(
            @"^((\d+(\.\d+)*[.)]?)|([IVXLC]+[.)])|(Chapter|Section|Article|Appendix)\s+\d+|제\s*\d+\s*(장|절|조))\s+.+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static async Task<ContextSplitProposal> CreateProposalAsync(string pdfPath, int targetPagesPerPart)
        {
            string? apiKey = OpenAISettingsService.GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key is required for Context Split (LLM). Set it in Options > OpenAI API Settings.");
            }

            var pages = ExtractPageSummaries(pdfPath);
            if (pages.Count == 0)
            {
                throw new InvalidOperationException("No extractable page text was found in the PDF.");
            }

            string model = OpenAISettingsService.GetModel();

            var request = BuildRequest(model, pages, targetPagesPerPart);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "responses");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await HttpClient.SendAsync(httpRequest);
            string responseText = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI API request failed ({(int)response.StatusCode}): {TrimForError(responseText)}");
            }

            string json = ExtractOutputText(responseText);
            var ranges = ParseAndValidateRanges(json, pages.Count);

            return new ContextSplitProposal
            {
                SourceFileName = Path.GetFileName(pdfPath),
                TotalPages = pages.Count,
                Ranges = ranges
            };
        }

        private static List<PageSummary> ExtractPageSummaries(string pdfPath)
        {
            var pages = new List<PageSummary>();
            using var reader = new PdfReader(pdfPath);
            using var pdfDoc = new PdfDocument(reader);

            for (int pageNumber = 1; pageNumber <= pdfDoc.GetNumberOfPages(); pageNumber++)
            {
                string text = PdfTextExtractor.GetTextFromPage(
                    pdfDoc.GetPage(pageNumber),
                    new SimpleTextExtractionStrategy());
                text = NormalizeWhitespace(text);

                pages.Add(new PageSummary
                {
                    Page = pageNumber,
                    HeadingHints = ExtractHeadingHints(text),
                    TextPreview = text.Length > MaxPreviewCharactersPerPage
                        ? text.Substring(0, MaxPreviewCharactersPerPage)
                        : text
                });
            }

            return pages;
        }

        private static string NormalizeWhitespace(string text)
        {
            return Regex.Replace(text.Replace("\r\n", "\n").Replace('\r', '\n'), @"[ \t]+", " ").Trim();
        }

        private static string[] ExtractHeadingHints(string pageText)
        {
            return pageText
                .Split('\n')
                .Select(line => Regex.Replace(line.Trim(), @"\s+", " "))
                .Where(line => line.Length is > 0 and <= 160)
                .Where(line => HeadingPattern.IsMatch(line) || LooksLikeHeading(line))
                .Take(5)
                .ToArray();
        }

        private static bool LooksLikeHeading(string line)
        {
            bool endsLikeSentence = line.EndsWith(".", StringComparison.Ordinal)
                || line.EndsWith(",", StringComparison.Ordinal)
                || line.EndsWith(";", StringComparison.Ordinal);
            return !endsLikeSentence
                && line.Length <= 90
                && line.Count(char.IsLetterOrDigit) >= 4
                && line == line.ToUpperInvariant();
        }

        private static object BuildRequest(string model, IReadOnlyList<PageSummary> pages, int targetPagesPerPart)
        {
            int totalPages = pages.Count;
            string userInput = JsonSerializer.Serialize(new
            {
                total_pages = totalPages,
                target_pages_per_part = targetPagesPerPart,
                rules = new[]
                {
                    "Cover every page from 1 to total_pages exactly once.",
                    "Do not overlap page ranges.",
                    "Prefer semantic boundaries from headings, topic changes, or table-of-contents-like transitions.",
                    "Keep ranges near target_pages_per_part unless a strong semantic boundary suggests otherwise.",
                    "Use concise, file-system-safe titles."
                },
                pages = pages.Select(page => new
                {
                    page = page.Page,
                    heading_hints = page.HeadingHints,
                    text_preview = page.TextPreview
                })
            });

            return new
            {
                model,
                input = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "You propose semantic PDF split ranges. Return only JSON matching the schema."
                    },
                    new
                    {
                        role = "user",
                        content = userInput
                    }
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "pdf_context_split",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "ranges" },
                            properties = new
                            {
                                ranges = new
                                {
                                    type = "array",
                                    minItems = 1,
                                    items = new
                                    {
                                        type = "object",
                                        additionalProperties = false,
                                        required = new[] { "start_page", "end_page", "title", "reason", "confidence" },
                                        properties = new
                                        {
                                            start_page = new { type = "integer", minimum = 1, maximum = totalPages },
                                            end_page = new { type = "integer", minimum = 1, maximum = totalPages },
                                            title = new { type = "string" },
                                            reason = new { type = "string" },
                                            confidence = new { type = "number", minimum = 0, maximum = 1 }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        private static string ExtractOutputText(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);
            JsonElement root = document.RootElement;

            if (root.TryGetProperty("output_text", out JsonElement outputText)
                && outputText.ValueKind == JsonValueKind.String)
            {
                return outputText.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("output", out JsonElement output)
                && output.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement outputItem in output.EnumerateArray())
                {
                    if (!outputItem.TryGetProperty("content", out JsonElement content)
                        || content.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (JsonElement contentItem in content.EnumerateArray())
                    {
                        if (contentItem.TryGetProperty("text", out JsonElement text)
                            && text.ValueKind == JsonValueKind.String)
                        {
                            return text.GetString() ?? string.Empty;
                        }
                    }
                }
            }

            throw new InvalidOperationException("OpenAI response did not contain structured output text.");
        }

        private static IReadOnlyList<ContextSplitRange> ParseAndValidateRanges(string json, int totalPages)
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("ranges", out JsonElement rangesElement)
                || rangesElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("Context split response did not include a ranges array.");
            }

            var ranges = new List<ContextSplitRange>();
            int expectedStart = 1;
            int part = 1;

            foreach (JsonElement item in rangesElement.EnumerateArray())
            {
                int startPage = item.GetProperty("start_page").GetInt32();
                int endPage = item.GetProperty("end_page").GetInt32();
                if (startPage != expectedStart)
                {
                    throw new InvalidOperationException($"Context split ranges must be contiguous. Expected page {expectedStart}, got {startPage}.");
                }

                if (endPage < startPage || endPage > totalPages)
                {
                    throw new InvalidOperationException($"Invalid context split range: {startPage}-{endPage}.");
                }

                ranges.Add(new ContextSplitRange
                {
                    Part = part++,
                    StartPage = startPage,
                    EndPage = endPage,
                    IncludedStartPage = startPage,
                    IncludedEndPage = endPage,
                    Title = SanitizeTitle(item.GetProperty("title").GetString()),
                    Reason = item.GetProperty("reason").GetString() ?? string.Empty,
                    Confidence = Math.Clamp(item.GetProperty("confidence").GetDouble(), 0, 1)
                });

                expectedStart = endPage + 1;
            }

            if (ranges.Count == 0 || expectedStart != totalPages + 1)
            {
                throw new InvalidOperationException($"Context split ranges must cover pages 1-{totalPages} exactly once.");
            }

            return ranges;
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

        private static string TrimForError(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return "empty response";
            }

            return responseText.Length > 500 ? responseText.Substring(0, 500) : responseText;
        }

        private sealed class PageSummary
        {
            public required int Page { get; init; }
            public required string[] HeadingHints { get; init; }
            public required string TextPreview { get; init; }
        }
    }
}
