// FileConflictResult.cs
namespace PDFSplitterforCopilot
{
    /// <summary>
    /// 파일 중복 시 사용자 선택을 나타내는 열거형
    /// </summary>
    public enum FileConflictResult
    {
        /// <summary>현재 파일 덮어쓰기</summary>
        Overwrite,
        
        /// <summary>현재 파일 건너뛰기</summary>
        Skip,
        
        /// <summary>모든 중복 파일 덮어쓰기</summary>
        OverwriteAll,
        
        /// <summary>모든 중복 파일 건너뛰기</summary>
        SkipAll,
        
        /// <summary>취소</summary>
        Cancel
    }
}
