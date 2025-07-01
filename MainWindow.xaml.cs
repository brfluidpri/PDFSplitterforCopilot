using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using iText.Kernel.Pdf;
using System.Windows.Media;
using ModernWpf.Controls;
using System.Windows.Shapes;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;

// Alias for ambiguous types
using IOPath = System.IO.Path;
using SyncfusionPdf = Syncfusion.Pdf.PdfDocument;

namespace PDFSplitterforCopilot
{
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private ObservableCollection<FileItem> fileItems = new ObservableCollection<FileItem>();
        
        // 모드별 페이지 수 기본값 저장
        private int _splitModePageCount = 10;  // 분할 모드 기본값
        private int _convertModePageCount = 1; // 변환 모드 기본값
        
        // 일괄 변환 모드 상태
        private bool _isBatchConvertMode = false;

        public bool HasFiles => fileItems.Count > 0;
        public bool NoFiles => fileItems.Count == 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public MainWindow()
        {
            InitializeComponent();
            
            // 윈도우 크기와 상태 명시적 설정 (전체화면 방지)
            this.WindowState = WindowState.Normal;
            this.Width = 900;
            this.Height = 600;
            this.MinWidth = 700;
            this.MinHeight = 500;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.CanResize;
            
            dgFiles.ItemsSource = fileItems;
            DataContext = this;
            fileItems.CollectionChanged += (s, e) => 
            {
                OnPropertyChanged(nameof(HasFiles));
                OnPropertyChanged(nameof(NoFiles));
            };
            
            // 초기 모드별 페이지 수 설정
            InitializeModeSettings();
            
            // Event handlers are already set in XAML, removing redundant assignments
            // btnAddFiles.Click += BtnAddFiles_Click;
            // btnProcess.Click += BtnProcess_Click;
            // btnRemoveSelected.Click += BtnRemoveSelected_Click;
        }

        /// <summary>
        /// 모드별 초기 설정을 수행합니다.
        /// </summary>
        private void InitializeModeSettings()
        {
            // 분할 모드를 기본값으로 설정
            tbModeSwitch.IsChecked = false;
            numPageCount.Text = _splitModePageCount.ToString();
            
            // 실행 버튼 텍스트 업데이트
            UpdateProcessButtonText();
        }

        /// <summary>
        /// 실행 버튼의 텍스트를 현재 모드에 맞게 업데이트합니다.
        /// </summary>
        private void UpdateProcessButtonText()
        {
            bool isConvertMode = tbModeSwitch.IsChecked == true;
            btnProcess.Content = isConvertMode ? "▶ 변환 실행" : "▶ 분할 실행";
        }

        /// <summary>
        /// 숫자 입력만 허용하는 텍스트 입력 검증
        /// </summary>
        private void NumPageCount_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // 숫자만 허용 (0-9)
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^[0-9]+$");
        }

        /// <summary>
        /// 페이지 수 텍스트 입력 완료 후 검증 (포커스를 잃었을 때)
        /// </summary>
        private void NumPageCount_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string originalText = textBox.Text;
                string newText = originalText;

                // 빈 문자열이거나 공백만 있는 경우
                if (string.IsNullOrWhiteSpace(originalText))
                {
                    newText = "1";
                }
                else if (int.TryParse(originalText, out int value))
                {
                    // 범위 체크 (1-50)
                    if (value < 1)
                    {
                        newText = "1";
                    }
                    else if (value > 50)
                    {
                        newText = "50";
                    }
                }
                else
                {
                    // 숫자가 아닌 경우 1로 설정
                    newText = "1";
                }

                // 텍스트가 변경된 경우에만 업데이트
                if (newText != originalText)
                {
                    textBox.Text = newText;
                    // LostFocus 이벤트이므로 커서 위치 설정 불필요
                }
            }
        }

        /// <summary>
        /// 현재 페이지 수 값을 가져옵니다.
        /// </summary>
        private int GetCurrentPageCount()
        {
            if (int.TryParse(numPageCount.Text, out int value) && value >= 1 && value <= 50)
            {
                return value;
            }
            return 1; // 기본값
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // 체크박스가 선택되었을 때의 처리 (필요시)
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // 체크박스가 해제되었을 때의 처리 (필요시)
        }

        private void BtnRemoveSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = fileItems.Where(f => f.IsSelected).ToList();
            
            foreach (var item in selectedItems)
            {
                fileItems.Remove(item);
            }
            
            txtStatus.Text = $"총 {fileItems.Count}개 파일 등록됨";
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(NoFiles));
        }

        public void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        public async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                await AddFilesToCollection(files);
            }
        }

        public async void BtnAddFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "PDF 및 Word 파일|*.pdf;*.doc;*.docx|PDF 파일|*.pdf|Word 파일|*.doc;*.docx"
            };

            if (dialog.ShowDialog() == true)
            {
                await AddFilesToCollection(dialog.FileNames);
            }
        }
        
        private async System.Threading.Tasks.Task AddFilesToCollection(string[] filePaths)
        {
            txtStatus.Text = "파일 검사 중...";
            
            foreach (string filePath in filePaths)
            {
                var fileItem = new FileItem
                {
                    FileName = System.IO.Path.GetFileName(filePath),
                    FilePath = filePath,
                    StatusColor = Brushes.Yellow,
                    StatusMessage = "검사 중...",
                    PageCount = "검사 중..."
                };

                fileItems.Add(fileItem);
                
                // 파일 검증을 비동기로 수행
                await ValidateFileAsync(fileItem);
            }
            
            txtStatus.Text = $"총 {fileItems.Count}개 파일 등록됨";
            OnPropertyChanged(nameof(HasFiles));
            OnPropertyChanged(nameof(NoFiles));
            
            // Attach remove button event handlers for new items
            AttachRemoveButtonHandlers();
        }

        private void AttachRemoveButtonHandlers()
        {
            // This will be called whenever the DataGrid is updated
            // The actual event handler attachment happens through the XAML template
            // We'll handle this through the BtnRemove_Click method which gets the item from the button's Tag
        }

        private async System.Threading.Tasks.Task ValidateFileAsync(FileItem fileItem)
        {
            try
            {
                string extension = System.IO.Path.GetExtension(fileItem.FilePath).ToLower();
                
                if (extension == ".pdf")
                {
                    await ValidatePdfFileAsync(fileItem);
                }
                else if (extension == ".doc" || extension == ".docx")
                {
                    // Syncfusion을 사용하므로 MS Word 설치 여부와 관계없이 변환 가능
                    fileItem.PageCount = "변환 후 확인";
                    fileItem.StatusColor = Brushes.Orange;
                    fileItem.StatusMessage = "Word→PDF 변환 필요";
                }
                else
                {
                    fileItem.StatusColor = Brushes.Red;
                    fileItem.StatusMessage = "지원하지 않는 형식";
                    fileItem.PageCount = "N/A";
                }
            }
            catch (Exception ex)
            {
                fileItem.StatusColor = Brushes.Red;
                fileItem.StatusMessage = $"오류: {ex.Message}";
                fileItem.PageCount = "N/A";
            }
        }

        private async System.Threading.Tasks.Task ValidatePdfFileAsync(FileItem fileItem)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {                try
                {
                    var reader = new PdfReader(fileItem.FilePath);
                    using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                    {
                        int pageCount = pdfDoc.GetNumberOfPages();
                        // Calculate blank pages ratio
                        int blankCount = 0;
                        for (int i = 1; i <= pageCount; i++)
                        {
                            var page = pdfDoc.GetPage(i);
                            if (page.GetContentBytes().Length == 0)
                                blankCount++;
                        }
                        double blankRatio = blankCount / (double)pageCount;

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.PageCount = pageCount.ToString();
                            fileItem.StatusColor = Brushes.Green;
                            fileItem.StatusMessage = "처리 가능";
                            fileItem.BlankPageRatio = blankRatio;
                        });
                    }
                }
                catch (iText.Kernel.Exceptions.BadPasswordException)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.StatusColor = Brushes.Red;
                        fileItem.StatusMessage = "암호가 필요함";
                        fileItem.PageCount = "N/A";
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.StatusColor = Brushes.Red;
                        fileItem.StatusMessage = $"오류: {ex.Message}";
                        fileItem.PageCount = "N/A";
                    });
                }
            });        }
          public async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            // 일괄 변환 모드 체크
            if (_isBatchConvertMode)
            {
                await ProcessBatchConvertAsync();
                return;
            }

            int pageSize = GetCurrentPageCount();
            if (pageSize <= 0)
            {
                MessageBox.Show("올바른 페이지 수를 입력하세요.", "오류");
                return;
            }

            // 모드 확인 (분할 또는 변환) - 토글 버튼 기반
            bool isConvertMode = tbModeSwitch.IsChecked == true;
            
            // 체크된 파일만 필터링
            var selectedFiles = fileItems.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                MessageBox.Show("처리할 파일을 선택해주세요.", "알림");
                return;
            }

            // 선택된 파일 중 유효한 파일만 필터링 (Orange는 Word 파일로 변환 가능한 상태)
            var validFiles = selectedFiles.Where(f => f.StatusColor == Brushes.Green || f.StatusColor == Brushes.Orange).ToList();
            
            // 처리 불가능한 파일들 확인 (Red 상태)
            var invalidFiles = selectedFiles.Where(f => f.StatusColor == Brushes.Red).ToList();
            
            if (invalidFiles.Any())
            {
                var result = MessageBox.Show(
                    $"다음 {invalidFiles.Count}개의 파일은 처리할 수 없습니다:\n\n" +
                    string.Join("\n", invalidFiles.Select(f => $"{f.FileName} - {f.StatusMessage}")) + "\n\n" +
                    "처리 가능한 파일만 계속 진행하시겠습니까?",
                    "처리 불가 파일 존재", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Warning);
                    
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            
            if (!validFiles.Any())
            {
                MessageBox.Show("처리할 수 있는 파일이 없습니다.", "알림");
                return;
            }

            // 확인창 표시 (모드에 따라 메시지 변경)
            string modeText = isConvertMode ? "변환" : "분할";
            string detailText = isConvertMode ? 
                $"각 파일당 1~{pageSize}페이지 추출" : 
                $"분할 페이지 수: {pageSize}페이지";
                
            var confirmResult = MessageBox.Show(
                $"총 {validFiles.Count}개의 파일을 {modeText}하시겠습니까?\n\n{detailText}", 
                $"{modeText} 확인", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            progressContainer.Visibility = Visibility.Visible;
            progressBar.Maximum = validFiles.Count;
            progressBar.Value = 0;

            btnProcess.IsEnabled = false;
            
            try
            {
                foreach (var fileItem in validFiles)
                {
                    txtStatus.Text = $"처리 중: {fileItem.FileName}";
                    
                    try
                    {
                        if (isConvertMode)
                        {
                            await ProcessConvertFileAsync(fileItem, pageSize);
                        }
                        else
                        {
                            await ProcessFileAsync(fileItem, pageSize);
                        }
                        LogMessage($"파일 처리 완료: {fileItem.FileName}");
                    }
                    catch (Exception fileEx)
                    {
                        LogMessage($"파일 처리 실패: {fileItem.FileName}", fileEx, "ERROR");
                        
                        // 개별 파일 처리 실패 시 상태 업데이트하고 계속 진행
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.StatusColor = Brushes.Red;
                            fileItem.StatusMessage = $"처리 실패: {fileEx.Message}";
                            if (fileItem.Steps != null && fileItem.Steps.Any())
                            {
                                var lastStep = fileItem.Steps.Last();
                                lastStep.Status = StepStatus.Error;
                                lastStep.Message = $"오류: {fileEx.Message}";
                            }
                        });
                        
                        // 사용자에게 개별 파일 오류 알림 (선택 사항)
                        var continueResult = MessageBox.Show(
                            $"파일 '{fileItem.FileName}' 처리 중 오류가 발생했습니다:\n\n{fileEx.Message}\n\n다음 파일을 계속 처리하시겠습니까?",
                            "파일 처리 오류",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                            
                        if (continueResult == MessageBoxResult.No)
                        {
                            break; // 사용자가 중단을 선택하면 루프 종료
                        }
                    }
                    
                    progressBar.Value++;
                }

                string completionMessage = isConvertMode ? 
                    $"모든 파일 변환 완료! 각 파일당 1~{pageSize}페이지가 추출되었습니다." :
                    "모든 파일 분할 완료!";
                    
                txtStatus.Text = completionMessage;
                MessageBox.Show(isConvertMode ? 
                    $"변환 작업이 완료되었습니다.\n각 파일당 1~{pageSize}페이지가 추출되었습니다." :
                    "분할 작업이 완료되었습니다.", 
                    "완료");
            }
            catch (Exception ex)
            {
                string detailedErrorMessage = $"처리 중 오류가 발생했습니다:\n{ex.GetType().FullName}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                MessageBox.Show(detailedErrorMessage, "오류 상세 정보");
                txtStatus.Text = "처리 중 오류 발생";
            }
            finally
            {
                progressContainer.Visibility = Visibility.Collapsed;
                btnProcess.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task ProcessFileAsync(FileItem fileItem, int pageSize)
        {
            // 고정된 5단계 프로세스
            int totalSteps = 5;
            
            // Initialize progress tracking
            Application.Current.Dispatcher.Invoke(() =>
            {
                fileItem.TotalSteps = totalSteps;
                fileItem.CurrentStep = 0;
                fileItem.StepMessage = "준비 중...";
                
                // Initialize steps collection with 5 fixed steps
                fileItem.Steps.Clear();
                string[] stepNames = { "파일 검증", "문서 준비", "PDF 읽기", "분할 처리", "완료" };
                for (int i = 0; i < totalSteps; i++)
                {
                    fileItem.Steps.Add(new StepItem
                    {
                        Name = stepNames[i],
                        Status = StepStatus.Pending,
                        Message = "대기 중..."
                    });
                }
            });            
            await System.Threading.Tasks.Task.Run(() =>
            {
                int currentStep = 0;
                string pdfPath = fileItem.FilePath;
                
                // Step 1: 파일 검증
                Application.Current.Dispatcher.Invoke(() =>
                {
                    fileItem.UpdateStep(currentStep, "파일 형식 검증 중...", StepStatus.InProgress);
                });
                
                try
                {
                    string extension = System.IO.Path.GetExtension(fileItem.FilePath).ToLower();
                    if (extension != ".pdf" && extension != ".doc" && extension != ".docx")
                    {
                        throw new NotSupportedException("지원하지 않는 파일 형식입니다.");
                    }
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, $"파일 검증 완료 ({extension})", StepStatus.Completed);
                    });
                    currentStep++;
                    
                    // Step 2: 문서 준비 (Word 파일인 경우 PDF로 변환)
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, "문서 준비 중...", StepStatus.InProgress);
                    });
                    
                    if (extension != ".pdf")
                    {
                        pdfPath = ConvertWordToPdfFile(fileItem.FilePath);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, "Word → PDF 변환 완료", StepStatus.Completed);
                        });
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, "PDF 파일 준비 완료", StepStatus.Completed);
                        });
                    }
                    currentStep++;
                    
                    // Step 3: PDF 읽기
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, "PDF 파일 읽는 중...", StepStatus.InProgress);
                    });
                      int totalPages = 0;
                    using (var reader = new PdfReader(pdfPath))
                    using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                    {
                        totalPages = pdfDoc.GetNumberOfPages();
                    }
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, $"PDF 읽기 완료 ({totalPages}페이지)", StepStatus.Completed);
                    });
                    currentStep++;
                    
                    // Step 4: 분할 처리
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, "PDF 분할 중...", StepStatus.InProgress);
                    });
                    
                    SplitPdfFile(pdfPath, pageSize, fileItem, totalPages);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, "PDF 분할 완료", StepStatus.Completed);
                    });
                    currentStep++;
                    
                    // Step 5: 완료
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, "모든 작업 완료", StepStatus.Completed);
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, $"오류: {ex.Message}", StepStatus.Error);
                    });
                    throw;
                }
            });
        }        private void SplitPdfFile(string pdfPath, int pageSize, FileItem fileItem, int totalPages)
        {            using (var reader = new PdfReader(pdfPath))
            using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
            {
                string outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pdfPath) ?? "", "output_split");
                
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                for (int startPage = 1; startPage <= totalPages; startPage += pageSize)
                {
                    int endPage = Math.Min(startPage + pageSize - 1, totalPages);
                    
                    string outputFileName = $"{System.IO.Path.GetFileNameWithoutExtension(pdfPath)}_page{startPage:D2}-{endPage:D2}.pdf";
                    string outputFilePath = System.IO.Path.Combine(outputDir, outputFileName);
                    
                    using (var writer = new iText.Kernel.Pdf.PdfWriter(outputFilePath))
                    using (var newPdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
                    {
                        pdfDoc.CopyPagesTo(startPage, endPage, newPdfDoc);
                    }
                }
            }
        }

        /// <summary>
        /// Syncfusion을 사용하여 Word 파일을 PDF로 변환합니다. (Office 설치 불필요)
        /// </summary>
        /// <param name="wordFilePath">변환할 Word 파일 경로</param>
        /// <returns>생성된 PDF 파일 경로</returns>
        private string ConvertWordToPdfFileWithSyncfusion(string wordFilePath)
        {
            try
            {
                LogMessage($"ConvertWordToPdfFileWithSyncfusion 시작 - Word: {wordFilePath}");
                
                // 파일 존재 확인
                if (!File.Exists(wordFilePath))
                {
                    throw new FileNotFoundException($"Word 파일을 찾을 수 없습니다: {wordFilePath}");
                }
                
                string outputPath = System.IO.Path.ChangeExtension(wordFilePath, ".pdf");
                LogMessage($"예상 출력 경로: {outputPath}");
                
                // 출력 파일이 이미 존재하는 경우 백업 생성
                if (File.Exists(outputPath))
                {
                    string backupPath = System.IO.Path.ChangeExtension(outputPath, $".backup_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    File.Move(outputPath, backupPath);
                    LogMessage($"기존 PDF 파일을 백업으로 이동: {backupPath}");
                }
                
                // Word 문서 파일 스트림 열기 (읽기 전용, 공유 읽기 허용)
                using (FileStream fileStream = new FileStream(wordFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    LogMessage($"Word 파일 스트림 열기 성공");
                    
                    // 기존 Word 문서 로드
                    using (WordDocument wordDocument = new WordDocument(fileStream, FormatType.Automatic))
                    {
                        LogMessage($"Word 문서 로드 성공 - 섹션 수: {wordDocument.Sections.Count}");
                        
                        // DocIORenderer 인스턴스 생성
                        using (DocIORenderer renderer = new DocIORenderer())
                        {
                            LogMessage($"DocIORenderer 생성 성공");
                            
                            // Word 문서를 PDF 문서로 변환
                            using (Syncfusion.Pdf.PdfDocument pdfDocument = renderer.ConvertToPDF(wordDocument))
                            {
                                LogMessage($"PDF 변환 성공 - 페이지 수: {pdfDocument.Pages.Count}");
                                
                                // PDF 파일을 파일 시스템에 저장 (쓰기 전용)
                                using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    pdfDocument.Save(outputStream);
                                    LogMessage($"PDF 파일 저장 성공: {outputPath}");
                                }
                            }
                        }
                    }
                }
                
                // 파일이 실제로 생성되었는지 확인
                if (File.Exists(outputPath))
                {
                    var fileInfo = new FileInfo(outputPath);
                    LogMessage($"변환된 PDF 파일 확인: {outputPath}, 크기: {fileInfo.Length} bytes");
                    
                    // 파일 크기가 너무 작으면 경고
                    if (fileInfo.Length < 1024) // 1KB 미만
                    {
                        LogMessage($"경고: 생성된 PDF 파일 크기가 매우 작습니다 ({fileInfo.Length} bytes). 내용이 올바르지 않을 수 있습니다.", level: "WARN");
                    }
                }
                else
                {
                    LogMessage($"변환된 PDF 파일 생성 실패: {outputPath}", level: "ERROR");
                    throw new FileNotFoundException($"변환된 PDF 파일을 찾을 수 없습니다: {outputPath}");
                }
                
                LogMessage($"ConvertWordToPdfFileWithSyncfusion 완료 - 반환: {outputPath}");
                return outputPath;
            }
            catch (UnauthorizedAccessException ex)
            {
                LogMessage($"파일 접근 권한 오류: {wordFilePath}", ex, "ERROR");
                string errorMessage = $"파일에 접근할 수 없습니다.\n\n" +
                                    $"파일: {System.IO.Path.GetFileName(wordFilePath)}\n" +
                                    $"오류: 파일이 다른 프로그램에서 사용 중이거나 읽기 전용일 수 있습니다.\n\n" +
                                    $"해결 방법:\n" +
                                    $"• Word에서 파일을 닫고 다시 시도\n" +
                                    $"• 파일의 읽기 전용 속성 해제\n" +
                                    $"• 관리자 권한으로 실행";
                
                MessageBox.Show(errorMessage, "파일 접근 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (IOException ex)
            {
                LogMessage($"파일 I/O 오류: {wordFilePath}", ex, "ERROR");
                string errorMessage = $"파일 읽기/쓰기 중 오류가 발생했습니다.\n\n" +
                                    $"파일: {System.IO.Path.GetFileName(wordFilePath)}\n" +
                                    $"오류: {ex.Message}\n\n" +
                                    $"가능한 원인:\n" +
                                    $"• 디스크 공간 부족\n" +
                                    $"• 파일이 손상됨\n" +
                                    $"• 네트워크 연결 문제 (네트워크 드라이브의 경우)";
                
                MessageBox.Show(errorMessage, "파일 I/O 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                // 로그 파일에 오류 기록
                LogMessage($"Syncfusion Word→PDF 변환 실패: {wordFilePath}", ex, "ERROR");
                
                // 사용자에게 오류 메시지 표시
                string errorMessage = $"Word 파일을 PDF로 변환하는 중 오류가 발생했습니다.\n\n" +
                                    $"파일: {System.IO.Path.GetFileName(wordFilePath)}\n" +
                                    $"오류: {ex.Message}\n\n" +
                                    $"가능한 원인:\n" +
                                    $"• 파일이 손상되었거나 암호로 보호됨\n" +
                                    $"• 지원되지 않는 Word 문서 형식\n" +
                                    $"• Syncfusion 라이선스 문제\n" +
                                    $"• 메모리 부족\n\n" +
                                    $"상세한 오류 정보는 로그 파일을 확인해주세요.";
                
                MessageBox.Show(errorMessage, "Word→PDF 변환 오류", MessageBoxButton.OK, MessageBoxImage.Error);
                
                throw; // 상위 호출자에게 예외 전파
            }
        }

        /// <summary>
        /// Word 파일을 PDF로 변환합니다. (Syncfusion 방식 사용)
        /// </summary>
        /// <param name="wordFilePath">변환할 Word 파일 경로</param>
        /// <returns>생성된 PDF 파일 경로</returns>
        private string ConvertWordToPdfFile(string wordFilePath)
        {
            return ConvertWordToPdfFileWithSyncfusion(wordFilePath);
        }

        /// <summary>
        /// 로그 메시지를 파일에 기록합니다.
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="exception">예외 객체 (선택사항)</param>
        /// <param name="level">로그 레벨</param>
        private void LogMessage(string message, Exception? exception = null, string level = "INFO")
        {
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string logDirectory = System.IO.Path.Combine(baseDirectory, "logs");
                
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                string logFile = System.IO.Path.Combine(logDirectory, $"pdfsplitter-{DateTime.Now:yyyy-MM-dd}.log");
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}\n";
                
                if (exception != null)
                {
                    logEntry += $"Exception: {exception.GetType().FullName}: {exception.Message}\n";
                    if (exception.StackTrace != null)
                    {
                        logEntry += $"StackTrace: {exception.StackTrace}\n";
                    }
                    logEntry += "--------------------------------------------------\n";
                }
                
                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // 로깅 실패 시 무시 (무한 루프 방지)
            }
        }

        /// <summary>
        /// 오류를 로그 파일에 기록합니다. (하위 호환성을 위한 래퍼)
        /// </summary>
        /// <param name="message">오류 메시지</param>
        /// <param name="exception">예외 객체</param>
        private void LogError(string message, Exception exception)
        {
            LogMessage(message, exception, "ERROR");
        }

        #region 메뉴 이벤트 핸들러

        private void MenuItem_New_Click(object sender, RoutedEventArgs e)
        {
            // 새 파일 - 현재 파일 목록 초기화
            fileItems.Clear();
            txtStatus.Text = "✅ 준비됨";
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            // 파일 열기 - 기존 파일 추가 기능 호출
            BtnAddFiles_Click(sender, e);
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            // 저장 기능 (현재 구현하지 않음)
            MessageBox.Show("저장 기능은 아직 구현되지 않았습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            // 프로그램 종료
            this.Close();
        }

        private void MenuItem_Undo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("실행 취소 기능은 아직 구현되지 않았습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Cut_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("잘라내기 기능은 아직 구현되지 않았습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("복사 기능은 아직 구현되지 않았습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("붙여넣기 기능은 아직 구현되지 않았습니다.", "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_FontSize_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string fontSizeStr)
            {
                if (double.TryParse(fontSizeStr, out double fontSize))
                {
                    // Window의 FontSize를 변경하여 모든 컨트롤에 상속
                    this.FontSize = fontSize;
                    
                    // 메뉴 체크 상태 업데이트
                    UpdateFontSizeMenuItems(fontSize);
                }
            }
        }

        private void UpdateFontSizeMenuItems(double currentFontSize)
        {
            // 메뉴 찾기 - Grid에서 Menu 찾기
            if (this.Content is Grid grid)
            {
                var menu = FindVisualChild<Menu>(grid);
                if (menu != null)
                {
                    var optionsMenu = menu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Header.ToString() == "Options");
                    if (optionsMenu != null)
                    {
                        var fontSizeMenu = optionsMenu.Items.OfType<MenuItem>().FirstOrDefault(m => m.Header.ToString() == "Font Size");
                        if (fontSizeMenu != null)
                        {
                            // 모든 하위 메뉴 아이템의 체크 해제
                            foreach (MenuItem item in fontSizeMenu.Items.OfType<MenuItem>())
                            {
                                item.IsChecked = false;
                            }

                            // 현재 선택된 크기의 메뉴 아이템 체크
                            var targetMenuItem = fontSizeMenu.Items.OfType<MenuItem>()
                                .FirstOrDefault(m => m.Tag?.ToString() == currentFontSize.ToString());
                            if (targetMenuItem != null)
                            {
                                targetMenuItem.IsChecked = true;
                            }
                        }
                    }
                }
            }
        }        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        /// <summary>
        /// 변환 작업을 위한 개별 파일 처리 (첫 번째 부분만 추출)
        /// </summary>
        /// <param name="fileItem">처리할 파일 아이템</param>
        /// <param name="pageLimit">추출할 페이지 수</param>
        private async System.Threading.Tasks.Task ProcessConvertFileAsync(FileItem fileItem, int pageLimit)
        {
            // 고정된 4단계 프로세스 (변환/추출용)
            int totalSteps = 4;
            
            try
            {
                // 디버깅용 로그
                LogMessage($"ProcessConvertFileAsync 시작 - 파일: {fileItem.FileName}, 페이지 제한: {pageLimit}");
                
                // Initialize progress tracking
                Application.Current.Dispatcher.Invoke(() =>
                {
                    fileItem.TotalSteps = totalSteps;
                    fileItem.CurrentStep = 0;
                    fileItem.StepMessage = "준비 중...";
                    
                    // Initialize steps collection with 4 fixed steps
                    fileItem.Steps.Clear();
                    string[] stepNames = { "파일 검증", "PDF 준비", "페이지 추출", "완료" };
                    for (int i = 0; i < totalSteps; i++)
                    {
                        fileItem.Steps.Add(new StepItem
                        {
                            Name = stepNames[i],
                            Status = StepStatus.Pending,
                            Message = "대기 중..."
                        });
                    }
                });
                
                await System.Threading.Tasks.Task.Run(() =>
                {
                    int currentStep = 0;
                    string pdfPath = fileItem.FilePath;
                    
                    // Step 1: 파일 검증
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.UpdateStep(currentStep, "파일 형식 검증 중...", StepStatus.InProgress);
                    });
                    
                    try
                    {
                        string extension = System.IO.Path.GetExtension(fileItem.FilePath).ToLower();
                        LogMessage($"파일 확장자 확인: {extension}");
                        
                        if (extension != ".pdf" && extension != ".doc" && extension != ".docx")
                        {
                            throw new NotSupportedException("지원하지 않는 파일 형식입니다.");
                        }
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, $"파일 검증 완료 ({extension})", StepStatus.Completed);
                        });
                        currentStep++;
                        
                        // Step 2: PDF 준비 (Word 파일인 경우 PDF로 변환)
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, "PDF 준비 중...", StepStatus.InProgress);
                        });
                        
                        if (extension != ".pdf")
                        {
                            LogMessage($"Word 파일 변환 시작: {fileItem.FilePath}");
                            pdfPath = ConvertWordToPdfFile(fileItem.FilePath);
                            LogMessage($"Word 파일 변환 완료: {pdfPath}");
                            
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                fileItem.UpdateStep(currentStep, "Word → PDF 변환 완료", StepStatus.Completed);
                            });
                        }
                        else
                        {
                            LogMessage($"PDF 파일 처리: {pdfPath}");
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                fileItem.UpdateStep(currentStep, "PDF 파일 준비 완료", StepStatus.Completed);
                            });
                        }
                        currentStep++;
                        
                        // Step 3: 페이지 추출 (첫 번째 부분만)
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, $"1~{pageLimit}페이지 추출 중...", StepStatus.InProgress);
                        });
                        
                        LogMessage($"페이지 추출 시작: {pdfPath}, 제한: {pageLimit}");
                        int extractedPages = ExtractPdfPagesWithResult(pdfPath, pageLimit);
                        LogMessage($"페이지 추출 완료: {extractedPages}페이지");
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, $"페이지 추출 완료 ({extractedPages}페이지)", StepStatus.Completed);
                        });
                        currentStep++;
                        
                        // Step 4: 완료
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, "모든 작업 완료", StepStatus.Completed);
                            fileItem.StatusColor = Brushes.Green;
                            fileItem.StatusMessage = $"완료: 1~{extractedPages}페이지 추출됨";
                        });
                        
                        LogMessage($"ProcessConvertFileAsync 완료 - 파일: {fileItem.FileName}");
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"ProcessConvertFileAsync 오류 - 파일: {fileItem.FileName}", ex, "ERROR");
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.UpdateStep(currentStep, $"오류: {ex.Message}", StepStatus.Error);
                            fileItem.StatusColor = Brushes.Red;
                            fileItem.StatusMessage = $"오류: {ex.Message}";
                        });
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                LogMessage($"ProcessConvertFileAsync 최상위 오류 - 파일: {fileItem.FileName}", ex, "ERROR");
                throw;
            }
        }

        /// <summary>
        /// PDF 파일에서 1페이지부터 지정된 페이지까지만 분리합니다. (결과 반환 버전)
        /// </summary>
        /// <param name="pdfPath">원본 PDF 파일 경로</param>
        /// <param name="pageLimit">분리할 페이지 수 (1부터 이 숫자까지)</param>
        /// <returns>실제로 분리된 페이지 수</returns>
        private int ExtractPdfPagesWithResult(string pdfPath, int pageLimit)
        {
            try
            {
                LogMessage($"ExtractPdfPagesWithResult 시작 - PDF: {pdfPath}, 제한: {pageLimit}");
                
                using (var reader = new PdfReader(pdfPath))
                using (var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader))
                {
                    int totalPages = pdfDoc.GetNumberOfPages();
                    int extractPages = Math.Min(pageLimit, totalPages);
                    
                    LogMessage($"총 페이지: {totalPages}, 추출할 페이지: {extractPages}");
                    
                    string outputDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pdfPath) ?? "", "output_split");
                    
                    LogMessage($"출력 디렉토리: {outputDir}");
                    
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                        LogMessage($"출력 디렉토리 생성됨: {outputDir}");
                    }
                    
                    string outputFileName = $"{System.IO.Path.GetFileNameWithoutExtension(pdfPath)}_page01-{extractPages:D2}.pdf";
                    string outputFilePath = System.IO.Path.Combine(outputDir, outputFileName);
                    
                    LogMessage($"출력 파일 경로: {outputFilePath}");
                    
                    using (var writer = new iText.Kernel.Pdf.PdfWriter(outputFilePath))
                    using (var newPdfDoc = new iText.Kernel.Pdf.PdfDocument(writer))
                    {
                        // 1페이지부터 extractPages까지 복사
                        pdfDoc.CopyPagesTo(1, extractPages, newPdfDoc);
                        LogMessage($"페이지 복사 완료: 1~{extractPages}");
                    }
                    
                    // 파일이 실제로 생성되었는지 확인
                    if (File.Exists(outputFilePath))
                    {
                        var fileInfo = new FileInfo(outputFilePath);
                        LogMessage($"출력 파일 생성 확인: {outputFilePath}, 크기: {fileInfo.Length} bytes");
                        
                        // 파일 크기가 너무 작으면 경고
                        if (fileInfo.Length < 1024) // 1KB 미만
                        {
                            LogMessage($"경고: 생성된 PDF 파일 크기가 매우 작습니다 ({fileInfo.Length} bytes). 내용이 올바르지 않을 수 있습니다.", level: "WARN");
                        }
                    }
                    else
                    {
                        LogMessage($"출력 파일 생성 실패: {outputFilePath}", level: "ERROR");
                        throw new FileNotFoundException($"추출된 PDF 파일을 찾을 수 없습니다: {outputFilePath}");
                    }
                    
                    LogMessage($"ExtractPdfPagesWithResult 완료 - 반환값: {extractPages}");
                    return extractPages;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ExtractPdfPagesWithResult 오류 - PDF: {pdfPath}", ex, "ERROR");
                throw;
            }
        }

        #endregion

        #region 도움말 메뉴 이벤트 핸들러

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PDF Splitter for Copilot v1.1\n\nCopilot의 RAG검색이 정확하도록 PDF 분할도구입니다.\n\n새로운 기능:\n• Syncfusion을 통한 Word→PDF 변환\n• 라이선스 키 관리 시스템\n• CLI 테스트 유틸리티", 
                   "정보", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuItem_Welcome_Click(object sender, RoutedEventArgs e)
        {
            var welcomeMessage = @"🎉 PDF Splitter for Copilot에 오신 것을 환영합니다!

    📖 사용 방법:
    • 파일 추가 버튼을 클릭하거나 PDF 파일을 드래그하여 추가하세요
    • 분할할 페이지 수를 설정하세요 (기본값: 10페이지)
    • 분할 실행 버튼을 클릭하여 처리를 시작하세요

    ✨ 주요 기능:
    • 대용량 PDF 파일 자동 분할
    • 진행 상황 실시간 모니터링
    • 다중 파일 동시 처리 지원
    • Fluent Design 기반 현대적 UI

    💡 팁: 
    • 파일 경로를 클릭하면 해당 폴더가 열립니다
    • 진행 상황 아이콘을 클릭하면 상세 정보를 확인할 수 있습니다

    📞 문의사항이 있으시면 Help > About을 참조하세요.";
            
            MessageBox.Show(welcomeMessage, "환영합니다!", 
                   MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StatusBrush_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 상태 아이콘 클릭 시 상세 정보 표시 및 결과물 폴더 열기 옵션 제공
            if (sender is Rectangle rect && rect.Tag is FileItem fileItem)
            {
                string message = $"파일명: {fileItem.FileName}\n상태: {fileItem.StatusMessage}";
                if (fileItem.Steps != null && fileItem.Steps.Any())
                {
                    message += "\n\n[처리 단계]";
                    foreach (var step in fileItem.Steps)
                    {
                        message += $"\n- {step.Name}: {step.Status} ({step.Message})";
                    }
                }
                
                // 처리 완료된 파일의 경우 결과물 폴더 열기 옵션 제공
                if (fileItem.StatusColor == Brushes.Green && 
                    (fileItem.StatusMessage.Contains("완료") || fileItem.StatusMessage.Contains("추출됨")))
                {
                    message += "\n\n결과물 폴더를 열어보시겠습니까?";
                    var result = MessageBox.Show(message, "상세 상태 정보", 
                        MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        OpenOutputFolder(fileItem.FilePath);
                    }
                }
                else
                {
                    MessageBox.Show(message, "상세 상태 정보", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void FilePath_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.Tag is string filePath)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show("파일 경로가 유효하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    string? directoryPath = System.IO.Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", directoryPath);
                    }
                    else
                    {
                        MessageBox.Show("폴더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"폴더를 여는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        /// <summary>
        /// 모드 토글 버튼 클릭 이벤트 핸들러
        /// </summary>
        private void TbModeSwitch_Click(object sender, RoutedEventArgs e)
        {
            // 토글 전의 모드 상태를 기준으로 현재 페이지 수를 저장
            // 주의: 클릭 이벤트 시점에서는 아직 IsChecked 값이 토글되기 전 상태임
            bool wasConvertMode = tbModeSwitch.IsChecked == false; // 토글 전 상태의 반대가 현재 모드
            
            // 현재 페이지 수를 이전 모드에 저장
            if (int.TryParse(numPageCount.Text, out int currentPageCount))
            {
                if (wasConvertMode)
                {
                    _convertModePageCount = currentPageCount;
                }
                else
                {
                    _splitModePageCount = currentPageCount;
                }
            }
            
            // 새로운 모드의 페이지 수 복원 (토글 후 상태)
            RestoreModePageCount();
            
            // 실행 버튼 텍스트 업데이트
            UpdateProcessButtonText();
        }

        /// <summary>
        /// 현재 모드에 따른 페이지 수를 저장합니다.
        /// </summary>
        private void SaveCurrentPageCount()
        {
            bool isConvertMode = tbModeSwitch.IsChecked == true;
            if (isConvertMode)
            {
                // 변환 모드인 경우
                if (int.TryParse(numPageCount.Text, out int convertPageCount))
                {
                    _convertModePageCount = convertPageCount;
                }
            }
            else
            {
                // 분할 모드인 경우
                if (int.TryParse(numPageCount.Text, out int splitPageCount))
                {
                    _splitModePageCount = splitPageCount;
                }
            }
        }

        /// <summary>
        /// 저장된 모드에 따른 페이지 수를 복원합니다.
        /// </summary>
        private void RestoreModePageCount()
        {
            bool isConvertMode = tbModeSwitch.IsChecked == true;
            numPageCount.Text = isConvertMode ? _convertModePageCount.ToString() : _splitModePageCount.ToString();
        }

        /// <summary>
        /// 지정된 파일의 결과물 폴더(output_split)를 탐색기로 엽니다.
        /// </summary>
        /// <param name="originalFilePath">원본 파일 경로</param>
        private void OpenOutputFolder(string originalFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(originalFilePath))
                {
                    MessageBox.Show("파일 경로가 유효하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 원본 파일의 디렉토리 경로 가져오기
                string? sourceDirectory = System.IO.Path.GetDirectoryName(originalFilePath);
                if (string.IsNullOrEmpty(sourceDirectory))
                {
                    MessageBox.Show("원본 파일의 디렉토리를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // output_split 폴더 경로 생성
                string outputDir = System.IO.Path.Combine(sourceDirectory, "output_split");
                
                LogMessage($"결과물 폴더 열기 시도: {outputDir}");

                if (Directory.Exists(outputDir))
                {
                    // 폴더가 존재하면 탐색기로 열기
                    System.Diagnostics.Process.Start("explorer.exe", outputDir);
                    LogMessage($"결과물 폴더 열기 성공: {outputDir}");
                }
                else
                {
                    // 폴더가 없으면 안내 메시지
                    MessageBox.Show($"결과물 폴더를 찾을 수 없습니다.\n\n경로: {outputDir}\n\n파일이 아직 처리되지 않았거나 처리 중 오류가 발생했을 수 있습니다.", 
                        "폴더 없음", MessageBoxButton.OK, MessageBoxImage.Information);
                    LogMessage($"결과물 폴더가 존재하지 않음: {outputDir}", level: "WARN");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"결과물 폴더 열기 오류: {originalFilePath}", ex, "ERROR");
                MessageBox.Show($"결과물 폴더를 여는 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 일괄 변환 모드 체크박스 체크 이벤트
        /// </summary>
        private void CbBatchConvert_Checked(object sender, RoutedEventArgs e)
        {
            _isBatchConvertMode = true;
            
            // 변환 모드로 토글
            tbModeSwitch.IsChecked = true;
            
            // 모드 토글 버튼 비활성화 (일괄 변환 모드에서는 변환 모드 고정)
            tbModeSwitch.IsEnabled = false;
            
            // 페이지 수 입력 비활성화
            numPageCount.IsEnabled = false;
            numPageCount.Text = "전체";
            
            // 실행 버튼 텍스트 업데이트
            btnProcess.Content = "▶ 일괄 변환 실행";
        }

        /// <summary>
        /// 일괄 변환 모드 체크박스 해제 이벤트
        /// </summary>
        private void CbBatchConvert_Unchecked(object sender, RoutedEventArgs e)
        {
            _isBatchConvertMode = false;
            
            // 모드 토글 버튼 활성화
            tbModeSwitch.IsEnabled = true;
            
            // 페이지 수 입력 활성화
            numPageCount.IsEnabled = true;
            
            // 기존 모드 복원
            RestoreModePageCount();
            
            // 실행 버튼 텍스트 업데이트
            UpdateProcessButtonText();
        }

        /// <summary>
        /// 일괄 변환 모드에서 Word 파일을 PDF로 변환합니다.
        /// </summary>
        private async Task ProcessBatchConvertAsync()
        {
            // 체크된 파일만 필터링
            var selectedFiles = fileItems.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                MessageBox.Show("변환할 파일을 선택해주세요.", "알림");
                return;
            }

            // Word 파일만 필터링
            var wordFiles = selectedFiles.Where(f => 
                f.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                f.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase)).ToList();

            if (!wordFiles.Any())
            {
                MessageBox.Show("변환할 Word 파일(.doc, .docx)이 선택되지 않았습니다.", "알림");
                return;
            }

            // 비 Word 파일이 포함된 경우 경고
            var nonWordFiles = selectedFiles.Except(wordFiles).ToList();
            if (nonWordFiles.Any())
            {
                var result = MessageBox.Show(
                    $"선택된 파일 중 {nonWordFiles.Count}개는 Word 파일이 아니므로 변환할 수 없습니다.\n\n" +
                    $"Word 파일 {wordFiles.Count}개만 PDF로 변환하시겠습니까?",
                    "일괄 변환 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            // 최종 확인
            var confirmResult = MessageBox.Show(
                $"총 {wordFiles.Count}개의 Word 파일을 PDF로 변환하시겠습니까?\n\n" +
                "모든 페이지가 변환됩니다.",
                "일괄 변환 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirmResult != MessageBoxResult.Yes)
            {
                return;
            }

            progressContainer.Visibility = Visibility.Visible;
            progressBar.Maximum = wordFiles.Count;
            progressBar.Value = 0;

            btnProcess.IsEnabled = false;

            try
            {
                foreach (var fileItem in wordFiles)
                {
                    txtStatus.Text = $"변환 중: {fileItem.FileName}";

                    try
                    {
                        await ProcessWordToPdfAsync(fileItem);
                        LogMessage($"Word → PDF 변환 완료: {fileItem.FileName}");
                    }
                    catch (Exception fileEx)
                    {
                        LogMessage($"Word → PDF 변환 실패: {fileItem.FileName}", fileEx, "ERROR");

                        // 개별 파일 처리 실패 시 상태 업데이트하고 계속 진행
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.StatusColor = Brushes.Red;
                            fileItem.StatusMessage = $"변환 실패: {fileEx.Message}";
                            if (fileItem.Steps != null && fileItem.Steps.Any())
                            {
                                var lastStep = fileItem.Steps.Last();
                                lastStep.Status = StepStatus.Error;
                                lastStep.Message = $"오류: {fileEx.Message}";
                            }
                        });

                        // 사용자에게 개별 파일 오류 알림
                        var continueResult = MessageBox.Show(
                            $"파일 '{fileItem.FileName}' 변환 중 오류가 발생했습니다:\n\n{fileEx.Message}\n\n다음 파일을 계속 처리하시겠습니까?",
                            "파일 변환 오류",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (continueResult == MessageBoxResult.No)
                        {
                            break; // 사용자가 중단을 선택하면 루프 종료
                        }
                    }

                    progressBar.Value++;
                }

                txtStatus.Text = "일괄 변환 완료!";
                MessageBox.Show(
                    $"일괄 변환 작업이 완료되었습니다.\n총 {wordFiles.Count}개의 Word 파일이 PDF로 변환되었습니다.",
                    "완료");
            }
            catch (Exception ex)
            {
                string detailedErrorMessage = $"일괄 변환 중 오류가 발생했습니다:\n{ex.GetType().FullName}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                MessageBox.Show(detailedErrorMessage, "오류 상세 정보");
                txtStatus.Text = "일괄 변환 중 오류 발생";
            }
            finally
            {
                progressContainer.Visibility = Visibility.Collapsed;
                btnProcess.IsEnabled = true;
            }
        }

        /// <summary>
        /// Word 파일을 PDF로 변환하는 메서드
        /// </summary>
        private async Task ProcessWordToPdfAsync(FileItem fileItem)
        {
            // 고정된 4단계 프로세스
            int totalSteps = 4;

            // Initialize progress tracking
            Application.Current.Dispatcher.Invoke(() =>
            {
                fileItem.TotalSteps = totalSteps;
                fileItem.CurrentStep = 0;
                fileItem.StepMessage = "Word → PDF 변환 준비 중...";

                // Initialize steps collection with 4 fixed steps
                fileItem.Steps.Clear();
                string[] stepNames = { "Word 문서 읽기", "PDF 변환 준비", "PDF 생성", "완료" };
                for (int i = 0; i < totalSteps; i++)
                {
                    fileItem.Steps.Add(new StepItem
                    {
                        Name = stepNames[i],
                        Status = StepStatus.Pending,
                        Message = "대기 중..."
                    });
                }
            });

            await Task.Run(() =>
            {
                try
                {
                    // Step 1: Word 문서 읽기
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        fileItem.CurrentStep = 1;
                        fileItem.StepMessage = "Word 문서를 읽는 중...";
                        fileItem.Steps[0].Status = StepStatus.InProgress;
                        fileItem.Steps[0].Message = "Word 문서 로딩 중...";
                    });

                    // Word 문서 로드
                    using (WordDocument wordDocument = new WordDocument(fileItem.FullPath))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.Steps[0].Status = StepStatus.Completed;
                            fileItem.Steps[0].Message = "Word 문서 로딩 완료";
                        });

                        // Step 2: PDF 변환 준비
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            fileItem.CurrentStep = 2;
                            fileItem.StepMessage = "PDF 변환 준비 중...";
                            fileItem.Steps[1].Status = StepStatus.InProgress;
                            fileItem.Steps[1].Message = "PDF 렌더러 초기화 중...";
                        });

                        // DocIORenderer 생성
                        using (DocIORenderer renderer = new DocIORenderer())
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                fileItem.Steps[1].Status = StepStatus.Completed;
                                fileItem.Steps[1].Message = "PDF 렌더러 준비 완료";
                            });

                            // Step 3: PDF 생성
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                fileItem.CurrentStep = 3;
                                fileItem.StepMessage = "PDF 파일 생성 중...";
                                fileItem.Steps[2].Status = StepStatus.InProgress;
                                fileItem.Steps[2].Message = "PDF 변환 진행 중...";
                            });

                            // Word를 PDF로 변환
                            SyncfusionPdf pdfDocument = renderer.ConvertToPDF(wordDocument);

                            // 출력 파일 경로 생성
                            string? directoryPath = IOPath.GetDirectoryName(fileItem.FullPath);
                            if (string.IsNullOrEmpty(directoryPath))
                            {
                                directoryPath = IOPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
                            }
                            
                            string outputPath = IOPath.Combine(directoryPath,
                                IOPath.GetFileNameWithoutExtension(fileItem.FileName) + "_converted.pdf");

                            // PDF 저장
                            using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                            {
                                pdfDocument.Save(outputStream);
                            }

                            pdfDocument.Close(true);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                fileItem.Steps[2].Status = StepStatus.Completed;
                                fileItem.Steps[2].Message = $"PDF 생성 완료: {IOPath.GetFileName(outputPath)}";
                            });

                            // Step 4: 완료
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                fileItem.CurrentStep = 4;
                                fileItem.StepMessage = "Word → PDF 변환 완료!";
                                fileItem.Steps[3].Status = StepStatus.Completed;
                                fileItem.Steps[3].Message = "변환 작업 완료";
                                fileItem.StatusColor = Brushes.Green;
                                fileItem.StatusMessage = "변환 완료";
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 오류 발생한 단계 표시
                        if (fileItem.CurrentStep > 0 && fileItem.CurrentStep <= fileItem.Steps.Count)
                        {
                            fileItem.Steps[fileItem.CurrentStep - 1].Status = StepStatus.Error;
                            fileItem.Steps[fileItem.CurrentStep - 1].Message = $"오류: {ex.Message}";
                        }

                        fileItem.StatusColor = Brushes.Red;
                        fileItem.StatusMessage = $"변환 실패: {ex.Message}";
                        fileItem.StepMessage = "변환 실패";
                    });

                    throw; // 예외를 다시 던져서 상위에서 처리하도록 함
                }
            });
        }
    }
}