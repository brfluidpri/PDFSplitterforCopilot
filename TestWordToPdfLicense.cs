using System;
using System.IO;
using System.Text;
using System.Linq;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;
using Syncfusion.Licensing;
using Serilog;

namespace PDFSplitterforCopilot
{
    /// <summary>
    /// CLI 테스트 프로그램: Syncfusion 라이선스 키 로딩 및 Word→PDF 변환 테스트
    /// </summary>
    public class TestWordToPdfLicense
    {
        private static string testInputFile = "WordSample01.docx";
        private static string testOutputFile = "WordSample01_Test.pdf";

        public static void Main(string[] args)
        {            // 로깅 설정
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("test_log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Console.WriteLine("=== Syncfusion 라이선스 키 및 Word→PDF 변환 테스트 ===");
                Console.WriteLine();                // 1. 라이선스 키 로딩 테스트
                Console.WriteLine("1. 라이선스 키 로딩 테스트...");
                string? licenseKey = LoadSyncfusionLicenseKey();
                
                if (string.IsNullOrEmpty(licenseKey))
                {
                    Console.WriteLine("❌ 라이선스 키를 찾을 수 없습니다.");
                    return;
                }

                Console.WriteLine($"✅ 라이선스 키 로딩 성공 (길이: {licenseKey.Length})");
                Console.WriteLine($"   키 앞 20자: {licenseKey.Substring(0, Math.Min(20, licenseKey.Length))}...");
                Console.WriteLine();                // 2. 라이선스 키 등록
                Console.WriteLine("2. Syncfusion 라이선스 키 등록...");
                try
                {
                    SyncfusionLicenseProvider.RegisterLicense(licenseKey);
                    Console.WriteLine("✅ 라이선스 키 등록 완료");
                    
                    // 라이선스 상태 확인
                    Console.WriteLine("   라이선스 상태 확인 중...");
                    
                    // 빈 문서로 먼저 테스트해서 라이선스 확인
                    using (var testDoc = new WordDocument())
                    {
                        testDoc.AddSection();
                        testDoc.LastSection.AddParagraph().AppendText("라이선스 테스트 문서");
                        
                        using (var renderer = new DocIORenderer())
                        {
                            using (var testPdf = renderer.ConvertToPDF(testDoc))
                            {
                                // PDF 메타데이터 확인
                                var creator = testPdf.DocumentInformation.Creator;
                                var producer = testPdf.DocumentInformation.Producer;
                                
                                Console.WriteLine($"   PDF Creator: {creator ?? "N/A"}");
                                Console.WriteLine($"   PDF Producer: {producer ?? "N/A"}");
                                
                                if (creator?.Contains("Syncfusion") == true && !creator.Contains("Evaluation"))
                                {
                                    Console.WriteLine("✅ 라이선스가 유효한 것으로 보입니다 (Creator 필드 기준)");
                                }
                                else if (creator?.Contains("Evaluation") == true || creator?.Contains("Trial") == true)
                                {
                                    Console.WriteLine("⚠️  평가판 라이선스가 감지됨 (Creator 필드 기준)");
                                }
                                else
                                {
                                    Console.WriteLine("❓ 라이선스 상태를 Creator 필드로 판단하기 어려움");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 라이선스 키 등록 실패: {ex.Message}");
                    Log.Error(ex, "라이선스 키 등록 실패");
                }
                Console.WriteLine();

                // 3. 테스트 파일 확인
                Console.WriteLine("3. 테스트 파일 확인...");
                if (!File.Exists(testInputFile))
                {
                    Console.WriteLine($"❌ 테스트 파일을 찾을 수 없습니다: {testInputFile}");
                    return;
                }
                Console.WriteLine($"✅ 테스트 파일 발견: {testInputFile}");
                Console.WriteLine();

                // 4. Word→PDF 변환 테스트
                Console.WriteLine("4. Word→PDF 변환 테스트...");
                bool conversionSuccess = ConvertWordToPdfWithSyncfusion(testInputFile, testOutputFile);

                if (conversionSuccess)
                {
                    Console.WriteLine("✅ Word→PDF 변환 성공");
                    
                    // 5. 출력 파일 분석
                    Console.WriteLine();
                    Console.WriteLine("5. 출력 파일 분석...");
                    AnalyzePdfFile(testOutputFile);
                }
                else
                {
                    Console.WriteLine("❌ Word→PDF 변환 실패");
                }

                Console.WriteLine();
                Console.WriteLine("=== 테스트 완료 ===");
                Console.WriteLine("Enter 키를 눌러서 종료하세요...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 테스트 중 오류 발생: {ex.Message}");
                Log.Error(ex, "테스트 중 오류 발생");
                Console.WriteLine("Enter 키를 눌러서 종료하세요...");
                Console.ReadLine();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }        /// <summary>
        /// Syncfusion 라이선스 키를 로딩합니다 (메인 앱과 동일한 로직)
        /// </summary>
        private static string? LoadSyncfusionLicenseKey()
        {
            try
            {
                // 1. 환경변수에서 로딩 시도
                string? licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
                if (!string.IsNullOrWhiteSpace(licenseKey))
                {
                    Log.Information("환경변수에서 Syncfusion 라이선스 키 로딩 완료");
                    Console.WriteLine("📝 환경변수에서 라이선스 키 로딩");
                    return licenseKey.Trim();
                }

                // 2. license.config 파일에서 로딩 시도
                string configPath = "license.config";
                if (File.Exists(configPath))
                {
                    var lines = File.ReadAllLines(configPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("#"))
                        {
                            Log.Information("license.config 파일에서 Syncfusion 라이선스 키 로딩 완료");
                            Console.WriteLine("📝 license.config 파일에서 라이선스 키 로딩");
                            return trimmedLine;
                        }
                    }
                }

                Console.WriteLine("📝 라이선스 키를 찾을 수 없음");
                Log.Warning("Syncfusion 라이선스 키를 찾을 수 없습니다");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Syncfusion 라이선스 키 로딩 중 오류");
                Console.WriteLine($"📝 라이선스 키 로딩 중 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Syncfusion을 사용하여 Word 파일을 PDF로 변환합니다
        /// </summary>
        private static bool ConvertWordToPdfWithSyncfusion(string wordFilePath, string pdfFilePath)
        {
            try
            {
                Console.WriteLine($"   입력 파일: {wordFilePath}");
                Console.WriteLine($"   출력 파일: {pdfFilePath}");

                using (var wordDocument = new WordDocument(wordFilePath, FormatType.Docx))
                {
                    using (var renderer = new DocIORenderer())
                    {
                        using (var pdfDocument = renderer.ConvertToPDF(wordDocument))
                        {
                            using (var fileStream = new FileStream(pdfFilePath, FileMode.Create, FileAccess.Write))
                            {
                                pdfDocument.Save(fileStream);
                            }
                        }
                    }
                }

                Log.Information($"Syncfusion을 사용한 Word→PDF 변환 완료: {wordFilePath} → {pdfFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Syncfusion Word→PDF 변환 실패: {wordFilePath}");
                Console.WriteLine($"   오류: {ex.Message}");
                return false;
            }
        }        /// <summary>
        /// 생성된 PDF 파일을 분석하여 워터마크 여부를 확인합니다
        /// </summary>
        private static void AnalyzePdfFile(string pdfFilePath)
        {
            try
            {
                if (!File.Exists(pdfFilePath))
                {
                    Console.WriteLine("❌ PDF 파일이 생성되지 않았습니다.");
                    return;
                }

                var fileInfo = new FileInfo(pdfFilePath);
                Console.WriteLine($"✅ PDF 파일 생성됨: {pdfFilePath}");
                Console.WriteLine($"   파일 크기: {fileInfo.Length:N0} bytes");
                Console.WriteLine($"   생성 시간: {fileInfo.CreationTime}");                // PDF 문서 로드해서 메타데이터 확인
                using (var fileStream = new FileStream(pdfFilePath, FileMode.Open, FileAccess.Read))
                {
                    using (var pdfDoc = new Syncfusion.Pdf.Parsing.PdfLoadedDocument(fileStream))
                    {
                        var docInfo = pdfDoc.DocumentInformation;
                        Console.WriteLine($"   PDF Title: {docInfo.Title ?? "N/A"}");
                        Console.WriteLine($"   PDF Creator: {docInfo.Creator ?? "N/A"}");
                        Console.WriteLine($"   PDF Producer: {docInfo.Producer ?? "N/A"}");
                        Console.WriteLine($"   PDF Subject: {docInfo.Subject ?? "N/A"}");
                        
                        // 워터마크 관련 텍스트 검사
                        bool hasWatermarkInMetadata = false;
                        var metadataTexts = new[] { docInfo.Title, docInfo.Creator, docInfo.Producer, docInfo.Subject };
                        
                        foreach (var text in metadataTexts)
                        {
                            if (!string.IsNullOrEmpty(text) && 
                                (text.Contains("Evaluation") || text.Contains("Trial") || 
                                 text.Contains("watermark", StringComparison.OrdinalIgnoreCase)))
                            {
                                hasWatermarkInMetadata = true;
                                break;
                            }
                        }
                        
                        if (hasWatermarkInMetadata)
                        {
                            Console.WriteLine("⚠️  PDF 메타데이터에서 평가판 관련 정보 발견");
                        }
                        else
                        {
                            Console.WriteLine("✅ PDF 메타데이터에서 평가판 관련 정보 없음");
                        }
                    }
                }

                // PDF 파일 내용 간단 분석 (텍스트 검색)
                var fileContent = File.ReadAllText(pdfFilePath, Encoding.Latin1);
                
                var watermarkKeywords = new[] { "Syncfusion", "Evaluation", "Trial", "watermark", "DEMO" };
                var foundKeywords = watermarkKeywords.Where(keyword => 
                    fileContent.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                
                if (foundKeywords.Any())
                {
                    Console.WriteLine($"⚠️  PDF 내용에서 다음 키워드 발견: {string.Join(", ", foundKeywords)}");
                    Console.WriteLine("   평가판 워터마크가 포함되어 있을 가능성이 있습니다.");
                }
                else
                {
                    Console.WriteLine("✅ PDF 내용에서 평가판 관련 키워드를 찾지 못했습니다.");
                }

                Console.WriteLine();
                Console.WriteLine($"📂 생성된 PDF 파일을 직접 열어서 워터마크를 확인해보세요: {Path.GetFullPath(pdfFilePath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ PDF 파일 분석 중 오류: {ex.Message}");
                Log.Error(ex, "PDF 파일 분석 중 오류");
            }
        }
    }
}
