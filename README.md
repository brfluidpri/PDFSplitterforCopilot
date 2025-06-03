# PDF Splitter for Copilot

## 프로젝트 초기 설정
```powershell
# 새 WPF 프로젝트 생성
dotnet new wpf -n PDFSplitterforCopilot
cd PDFSplitterforCopilot

# 필요한 패키지 설치 (PDF 및 Word 처리)
dotnet add package itext7 --version 8.0.4
dotnet add package itext7.bouncy-castle-adapter --version 8.0.4
dotnet add package Microsoft.Office.Interop.Word --version 15.0.4797.1004
dotnet add package FluentWPF --version 0.10.2
dotnet add package ModernWpfUI --version 0.9.6
dotnet add package System.Drawing.Common --version 9.0.5
```

## 프로그램 실행 방법

### 방법 1: BAT 파일 사용 (권장)
프로젝트 폴더에서 다음 파일 중 하나를 실행:
- `run.bat` - 프로젝트 빌드 후 실행
- `publish.bat` - 최종 배포파일 생성을 위한 실행

### 방법 2: PowerShell 사용
```powershell
# 프로젝트 폴더로 이동
cd .\PDFSplitterforCopilot

# 또는 직접 실행 파일 호출
Invoke-Item ".\bin\Debug\net8.0-windows\PDFSplitterforCopilot.exe"
```

### 방법 3: Visual Studio Code 작업 사용
1. VS Code에서 프로젝트 열기
2. Ctrl+Shift+B 누르기 (빌드 작업 실행)
3. bin\Debug\net8.0-windows 폴더에서 PDFSplitterforCopilot.exe 실행

## 배포

```powershell
# 단일 실행파일로 배포
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true 

## -p:PublishTrimmed=false 옵션을 유지하면 exe 실행파일이 정상적으로 실행되지 않는 경우를 발견, csproj 파일에 추가됨.

# 생성된 파일 위치: bin\Release\net8.0-windows\win-x64\publish\PDFSplitterforCopilot.exe
```