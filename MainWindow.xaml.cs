using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using iText.Kernel.Pdf;
using System.Windows.Media;
using ModernWpf.Controls;
using System.Windows.Shapes;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;

namespace PDFSplitterforCopilot
{
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        private ObservableCollection<FileItem> fileItems = new ObservableCollection<FileItem>();

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
            dgFiles.ItemsSource = fileItems;
            DataContext = this;
            fileItems.CollectionChanged += (s, e) => 
            {
                OnPropertyChanged(nameof(HasFiles));
                OnPropertyChanged(nameof(NoFiles));
            };
            
            // Event handlers are already set in XAML, removing redundant assignments
            // btnAddFiles.Click += BtnAddFiles_Click;
            // btnProcess.Click += BtnProcess_Click;
            // btnRemoveSelected.Click += BtnRemoveSelected_Click;
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
                }                else if (extension == ".doc" || extension == ".docx")
                {
                    if (IsWordInstalled())
                    {
                        // Word 파일은 일단 변환 가능으로 표시
                        fileItem.PageCount = "변환 후 확인";
                        fileItem.StatusColor = Brushes.Orange;
                        fileItem.StatusMessage = "Word→PDF 변환 필요";
                    }
                    else
                    {
                        fileItem.PageCount = "N/A";
                        fileItem.StatusColor = Brushes.Red;
                        fileItem.StatusMessage = "MS Word가 설치되지 않음 - PDF만 지원";
                    }
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
            int pageSize = (int)numPageCount.Value;
            if (pageSize <= 0)
            {
                MessageBox.Show("올바른 페이지 수를 입력하세요.", "오류");
                return;
            }

            // 체크된 파일만 필터링
            var selectedFiles = fileItems.Where(f => f.IsSelected).ToList();
            if (!selectedFiles.Any())
            {
                MessageBox.Show("처리할 파일을 선택해주세요.", "알림");
                return;
            }

            // 선택된 파일 중 유효한 파일만 필터링
            var validFiles = selectedFiles.Where(f => f.StatusColor == Brushes.Green || f.StatusColor == Brushes.Orange).ToList();
            
            // Word 파일이 있는지 확인
            var wordFiles = selectedFiles.Where(f => f.StatusColor == Brushes.Red && 
                f.StatusMessage.Contains("MS Word가 설치되지 않음")).ToList();
            
            if (wordFiles.Any())
            {
                var result = MessageBox.Show(
                    $"다음 {wordFiles.Count}개의 Word 파일은 Microsoft Office가 설치되지 않아 처리할 수 없습니다:\n\n" +
                    string.Join("\n", wordFiles.Select(f => f.FileName)) + "\n\n" +
                    "PDF 파일만 처리하시겠습니까?",
                    "Word 파일 처리 불가", 
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

            // 확인창 표시
            var confirmResult = MessageBox.Show(
                $"총 {validFiles.Count}개의 파일을 분할하시겠습니까?\n\n분할 페이지 수: {pageSize}페이지", 
                "분할 확인", 
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
                    
                    await ProcessFileAsync(fileItem, pageSize);
                    
                    progressBar.Value++;
                }

                txtStatus.Text = "모든 파일 처리 완료!";
                MessageBox.Show("분할 작업이 완료되었습니다.", "완료");
            }
            catch (Exception ex)
            {
                string detailedErrorMessage = $"처리 중 오류가 발생했습니다:\n{ex.GetType().FullName}: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                MessageBox.Show(detailedErrorMessage, "오류 상세 정보");                txtStatus.Text = "처리 중 오류 발생";
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
        }        private string ConvertWordToPdfFileWithInterop(string wordFilePath)
        {
            // 사전 확인
            if (!IsWordInstalled())
            {
                throw new InvalidOperationException(
                    "Microsoft Office Word가 설치되지 않았습니다.\n\n" +
                    "Word 파일(.doc, .docx)을 처리하려면 다음 중 하나가 필요합니다:\n" +
                    "• Microsoft Office (Word 포함)\n" +
                    "• Microsoft 365\n" +
                    "• Office 2019/2021\n\n" +
                    "대안: Word 파일을 수동으로 PDF로 변환 후 사용해 주세요.");
            }

            Microsoft.Office.Interop.Word.Application? wordApp = null;
            Microsoft.Office.Interop.Word.Document? doc = null;
            
            try
            {
                wordApp = new Microsoft.Office.Interop.Word.Application();
                wordApp.Visible = false;
                
                doc = wordApp.Documents.Open(wordFilePath);
                
                string outputPath = System.IO.Path.ChangeExtension(wordFilePath, ".pdf");
                doc.SaveAs2(outputPath, Microsoft.Office.Interop.Word.WdSaveFormat.wdFormatPDF);
                
                return outputPath;
            }
            catch (System.IO.FileNotFoundException ex) when (ex.Message.Contains("office"))
            {
                throw new InvalidOperationException(
                    "Microsoft Office Word 구성 요소를 찾을 수 없습니다.\n\n" +
                    "해결 방법:\n" +
                    "1. Microsoft Office 재설치\n" +
                    "2. Office 복구 실행\n" +
                    "3. PDF 파일만 사용", ex);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                throw new InvalidOperationException(
                    $"Word 실행 중 COM 오류 발생:\n{ex.Message}\n\n" +
                    "Word를 다시 시작하거나 시스템을 재부팅해 보세요.", ex);
            }
            finally
            {
                try
                {
                    doc?.Close();
                    wordApp?.Quit();
                    
                    if (doc != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
                    if (wordApp != null) System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                }
                catch
                {
                    // COM 해제 오류는 무시
                }
            }
        }/// <summary>
        /// Microsoft Office Word가 설치되어 있는지 확인합니다.
        /// </summary>
        /// <returns>Word가 설치되어 있으면 true, 그렇지 않으면 false</returns>
        private bool IsWordInstalled()
        {
            try
            {
                // COM 개체 생성을 실제로 시도해보기
                Type? wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null) return false;
                
                // 실제 인스턴스 생성 테스트
                object? wordApp = Activator.CreateInstance(wordType);
                if (wordApp != null)
                {
                    // 즉시 종료
                    wordType.InvokeMember("Quit", System.Reflection.BindingFlags.InvokeMethod, null, wordApp, null);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                // 로깅을 위해 오류 정보 저장
                System.Diagnostics.Debug.WriteLine($"Word 설치 확인 실패: {ex.Message}");
                return false;
            }
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
                string outputPath = System.IO.Path.ChangeExtension(wordFilePath, ".pdf");
                
                // Word 문서 파일 스트림 열기
                using (FileStream fileStream = new FileStream(wordFilePath, FileMode.Open))
                {
                    // 기존 Word 문서 로드
                    using (WordDocument wordDocument = new WordDocument(fileStream, FormatType.Automatic))
                    {
                        // DocIORenderer 인스턴스 생성
                        using (DocIORenderer renderer = new DocIORenderer())
                        {
                            // Word 문서를 PDF 문서로 변환
                            using (Syncfusion.Pdf.PdfDocument pdfDocument = renderer.ConvertToPDF(wordDocument))
                            {
                                // PDF 파일을 파일 시스템에 저장
                                using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    pdfDocument.Save(outputStream);
                                }
                            }
                        }
                    }
                }
                
                return outputPath;
            }
            catch (Exception ex)
            {
                // 로그 파일에 오류 기록
                LogError($"Syncfusion Word→PDF 변환 실패: {wordFilePath}", ex);
                
                // 사용자에게 오류 메시지 표시
                string errorMessage = $"Word 파일을 PDF로 변환하는 중 오류가 발생했습니다.\n\n" +
                                    $"파일: {System.IO.Path.GetFileName(wordFilePath)}\n" +
                                    $"오류: {ex.Message}\n\n" +
                                    $"가능한 원인:\n" +
                                    $"• 파일이 손상되었거나 암호로 보호됨\n" +
                                    $"• 지원되지 않는 Word 문서 형식\n" +
                                    $"• 파일이 다른 프로그램에서 사용 중\n\n" +
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
        /// 오류를 로그 파일에 기록합니다.
        /// </summary>
        /// <param name="message">오류 메시지</param>
        /// <param name="exception">예외 객체</param>
        private void LogError(string message, Exception exception)
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
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [ERROR] {message}\n" +
                                $"Exception: {exception.GetType().FullName}: {exception.Message}\n" +
                                $"StackTrace: {exception.StackTrace}\n" +
                                $"--------------------------------------------------\n";
                
                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // 로깅 실패 시 무시 (무한 루프 방지)
            }
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
        }        private void MenuItem_About_Click(object sender, RoutedEventArgs e)        {
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
        }        private void StatusBrush_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 상태 아이콘 클릭 시 상세 정보 표시
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
                  // 완료된 파일의 경우 output_split 폴더 열기 옵션 제공
                bool isCompleted = fileItem.Steps != null && fileItem.Steps.Any() && 
                                 fileItem.Steps.Last().Status == StepStatus.Completed;
                
                if (isCompleted && !string.IsNullOrEmpty(fileItem.FilePath))
                {
                    var result = MessageBox.Show(message + "\n\n분할된 파일들이 저장된 폴더를 열어보시겠습니까?", 
                        "상세 상태 정보", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            string? directoryPath = System.IO.Path.GetDirectoryName(fileItem.FilePath);
                            if (!string.IsNullOrEmpty(directoryPath))
                            {
                                string outputSplitPath = System.IO.Path.Combine(directoryPath, "output_split");
                                if (Directory.Exists(outputSplitPath))
                                {
                                    System.Diagnostics.Process.Start("explorer.exe", outputSplitPath);
                                }
                                else
                                {
                                    MessageBox.Show("output_split 폴더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"폴더 열기 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
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
                if (string.IsNullOrEmpty(filePath)) // Check if filePath is null or empty
                {
                    MessageBox.Show("파일 경로가 유효하지 않습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // 파일 경로에서 디렉토리 경로 추출
                    string? directoryPath = System.IO.Path.GetDirectoryName(filePath); // Use string? for nullable
                    if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
                    {
                        // 탐색기로 해당 폴더 열기
                        System.Diagnostics.Process.Start("explorer.exe", directoryPath);
                    }
                    else
                    {
                        MessageBox.Show("폴더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (ArgumentException aex) // Catch specific exceptions if filePath is invalid for Path.GetDirectoryName
                {
                    MessageBox.Show($"파일 경로 관련 오류가 발생했습니다: {aex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"폴더를 여는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
