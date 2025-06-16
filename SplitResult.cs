// SplitResult.cs
using System.Collections.Generic;

namespace PDFSplitterforCopilot
{
    /// <summary>
    /// PDF 분할 작업의 결과를 나타내는 클래스
    /// </summary>
    public class SplitResult
    {
        /// <summary>새로 생성된 파일 수</summary>
        public int CreatedCount { get; set; } = 0;
        
        /// <summary>덮어쓴 파일 수</summary>
        public int OverwrittenCount { get; set; } = 0;
        
        /// <summary>건너뛴 파일 수</summary>
        public int SkippedCount { get; set; } = 0;
        
        /// <summary>작업이 취소되었는지 여부</summary>
        public bool IsCancelled { get; set; } = false;
        
        /// <summary>총 처리된 파일 수</summary>
        public int TotalProcessed => CreatedCount + OverwrittenCount;
        
        /// <summary>총 파일 수 (건너뛴 것 포함)</summary>
        public int TotalFiles => CreatedCount + OverwrittenCount + SkippedCount;
        
        /// <summary>
        /// 결과 요약 메시지 생성
        /// </summary>
        public string GetSummaryMessage()
        {
            if (IsCancelled)
            {
                return "분할 작업이 취소되었습니다.";
            }
            
            var parts = new List<string>();
            
            if (CreatedCount > 0)
                parts.Add($"생성: {CreatedCount}개");
            
            if (OverwrittenCount > 0)
                parts.Add($"덮어씌움: {OverwrittenCount}개");
            
            if (SkippedCount > 0)
                parts.Add($"건너뜀: {SkippedCount}개");
            
            if (parts.Count == 0)
                return "처리된 파일이 없습니다.";
            
            return string.Join(", ", parts);
        }
    }
}
