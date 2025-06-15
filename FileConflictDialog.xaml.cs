// FileConflictDialog.xaml.cs
using System.IO;
using System.Windows;

namespace PDFSplitterforCopilot
{
    /// <summary>
    /// 파일 중복 시 사용자 선택을 위한 다이얼로그
    /// </summary>
    public partial class FileConflictDialog : Window
    {
        /// <summary>
        /// 사용자가 선택한 결과
        /// </summary>
        public FileConflictResult Result { get; private set; } = FileConflictResult.Cancel;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="filePath">중복된 파일의 경로</param>
        public FileConflictDialog(string filePath)
        {
            InitializeComponent();
            
            // 파일 정보 표시
            txtFileName.Text = Path.GetFileName(filePath);
            txtFilePath.Text = filePath;
        }

        /// <summary>
        /// 덮어쓰기 버튼 클릭
        /// </summary>
        private void BtnOverwrite_Click(object sender, RoutedEventArgs e)
        {
            Result = FileConflictResult.Overwrite;
            DialogResult = true;
        }

        /// <summary>
        /// 건너뛰기 버튼 클릭
        /// </summary>
        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            Result = FileConflictResult.Skip;
            DialogResult = true;
        }

        /// <summary>
        /// 모두 덮어쓰기 버튼 클릭
        /// </summary>
        private void BtnOverwriteAll_Click(object sender, RoutedEventArgs e)
        {
            Result = FileConflictResult.OverwriteAll;
            DialogResult = true;
        }

        /// <summary>
        /// 모두 건너뛰기 버튼 클릭
        /// </summary>
        private void BtnSkipAll_Click(object sender, RoutedEventArgs e)
        {
            Result = FileConflictResult.SkipAll;
            DialogResult = true;
        }

        /// <summary>
        /// 취소 버튼 클릭
        /// </summary>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = FileConflictResult.Cancel;
            DialogResult = false;
        }
    }
}
