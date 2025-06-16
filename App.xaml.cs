using System;
using System.IO;
using System.Windows;
using Syncfusion.Licensing;

namespace PDFSplitterforCopilot
{
    public partial class App : Application
    {        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Syncfusion 라이선스 키 등록
            RegisterSyncfusionLicense();
            
            // 실행 파일 위치에 logs 폴더 생성
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(baseDirectory, "logs");
            
            // logs 디렉토리 존재 확인 및 생성
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            
            // 기본 로깅 파일 생성
            string logFile = Path.Combine(logDirectory, $"pdfsplitter-{DateTime.Now:yyyy-MM-dd}.log");
            LogMessage("애플리케이션이 시작되었습니다.", logFile);
            
            // 예외 처리
            Current.DispatcherUnhandledException += (s, args) =>
            {
                LogMessage($"처리되지 않은 예외 발생: {args.Exception.Message}", logFile);
                args.Handled = true;
            };
            
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                LogMessage($"치명적인 예외 발생: {args.ExceptionObject}", logFile);
            };
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(baseDirectory, "logs");
            string logFile = Path.Combine(logDirectory, $"pdfsplitter-{DateTime.Now:yyyy-MM-dd}.log");
            LogMessage("애플리케이션이 종료되었습니다.", logFile);
            base.OnExit(e);
        }

        /// <summary>
        /// Syncfusion 라이선스 키를 등록합니다.
        /// 다음 우선 순위로 라이선스 키를 찾습니다:
        /// 1. 빌드 시 임베드된 라이선스 키
        /// 2. 환경 변수 (SYNCFUSION_LICENSE_KEY)
        /// 3. 설정 파일 (license.config)
        /// </summary>
        private void RegisterSyncfusionLicense()
        {
            string? licenseKey = null;
            
            try
            {
                // 1. 빌드 시 임베드된 라이선스 키 확인 (최우선)
                licenseKey = LicenseConfig.EmbeddedLicenseKey;
                if (!string.IsNullOrEmpty(licenseKey))
                {
                    SyncfusionLicenseProvider.RegisterLicense(licenseKey);
                    LogMessage("빌드 시 임베드된 Syncfusion 라이선스 키를 로드했습니다.", GetLogFilePath());
                    return;
                }
                
                // 2. 환경 변수에서 라이선스 키 확인
                licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
                if (!string.IsNullOrEmpty(licenseKey))
                {
                    SyncfusionLicenseProvider.RegisterLicense(licenseKey);
                    LogMessage("환경 변수에서 Syncfusion 라이선스 키를 로드했습니다.", GetLogFilePath());
                    return;
                }
                
                // 3. 설정 파일에서 라이선스 키 확인 (하위 호환성)
                licenseKey = LoadLicenseFromConfigFile();
                if (!string.IsNullOrEmpty(licenseKey))
                {
                    SyncfusionLicenseProvider.RegisterLicense(licenseKey);
                    LogMessage("설정 파일에서 Syncfusion 라이선스 키를 로드했습니다.", GetLogFilePath());
                    return;
                }
                
                // 4. 라이선스 키가 없는 경우 평가판 모드로 실행
                LogMessage("Syncfusion 라이선스 키가 설정되지 않았습니다. 평가판 모드로 실행됩니다.", GetLogFilePath());
                ShowLicenseInfo();
            }
            catch (Exception ex)
            {
                LogMessage($"Syncfusion 라이선스 등록 중 오류 발생: {ex.Message}", GetLogFilePath());
            }
        }        /// <summary>
        /// 설정 파일에서 라이선스 키를 로드합니다.
        /// </summary>
        /// <returns>라이선스 키 또는 null</returns>
        private string? LoadLicenseFromConfigFile()
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string configFile = Path.Combine(baseDirectory, "license.config");
                
                if (File.Exists(configFile))
                {
                    // 라이선스 파일의 각 줄을 읽어서 주석이 아닌 첫 번째 줄을 찾음
                    var lines = File.ReadAllLines(configFile, System.Text.Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        // 빈 줄이 아니고 주석(#으로 시작)이 아닌 줄을 라이선스 키로 간주
                        if (!string.IsNullOrEmpty(trimmedLine) && !trimmedLine.StartsWith("#"))
                        {
                            LogMessage($"설정 파일에서 라이선스 키를 찾았습니다 (길이: {trimmedLine.Length})", GetLogFilePath());
                            return trimmedLine;
                        }
                    }
                    
                    LogMessage("설정 파일에서 유효한 라이선스 키를 찾지 못했습니다 (주석만 있거나 빈 파일)", GetLogFilePath());
                }
                else
                {
                    LogMessage("license.config 파일을 찾을 수 없습니다.", GetLogFilePath());
                }
            }
            catch (Exception ex)
            {
                LogMessage($"설정 파일 로드 중 오류: {ex.Message}", GetLogFilePath());
            }
            
            return null;
        }

        /// <summary>
        /// 라이선스 정보를 사용자에게 안내합니다.
        /// </summary>
        private void ShowLicenseInfo()
        {
            string message = @"🔑 Syncfusion 라이선스 안내

현재 평가판 모드로 실행됩니다. (워터마크 포함)

라이선스 키를 설정하는 방법:

1️⃣ 환경 변수 설정 (권장):
   • 변수명: SYNCFUSION_LICENSE_KEY
   • 값: 귀하의 라이선스 키

2️⃣ 설정 파일 생성:
   • 파일: license.config (실행 파일과 같은 폴더)
   • 내용: 라이선스 키만 입력

📝 라이선스 키 얻기:
   • 무료: Syncfusion Community License 신청
   • 유료: Syncfusion 공식 웹사이트에서 구매
   • 평가: 30일 무료 평가판

🔗 자세한 정보: https://www.syncfusion.com/products/communitylicense";

            MessageBox.Show(message, "Syncfusion 라이선스 안내", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 로그 파일 경로를 반환합니다.
        /// </summary>
        /// <returns>로그 파일 전체 경로</returns>
        private string GetLogFilePath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string logDirectory = Path.Combine(baseDirectory, "logs");
            return Path.Combine(logDirectory, $"pdfsplitter-{DateTime.Now:yyyy-MM-dd}.log");
        }

        public void ProcessFile(string fileName, string filePath)
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logDirectory = Path.Combine(baseDirectory, "logs");
                string logFile = Path.Combine(logDirectory, $"pdfsplitter-{DateTime.Now:yyyy-MM-dd}.log");
                LogMessage($"작업 시작: {fileName}", logFile);
                // 파일 처리 로직 추가
            }
            catch (Exception exception)
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logDirectory = Path.Combine(baseDirectory, "logs");
                string logFile = Path.Combine(logDirectory, $"pdfsplitter-{DateTime.Now:yyyy-MM-dd}.log");
                LogMessage($"파일 처리 중 오류 발생: {filePath} - {exception.Message}", logFile);
            }
        }

        private void LogMessage(string message, string logFile)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [INFO] {message}{Environment.NewLine}";
                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // 로깅 실패 시 무시
            }
        }
    }
}
