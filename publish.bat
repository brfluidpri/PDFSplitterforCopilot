@echo off
echo PDF Splitter 배포 중...

REM 이전 배포 파일 정리
if exist "bin\Release\net8.0-windows\win-x64\publish" (
    echo 이전 배포 파일 정리 중...
    rmdir /s /q "bin\Release\net8.0-windows\win-x64\publish"
)

echo 프로젝트 빌드 및 배포 중...
REM 자체 포함 배포 (모든 의존성 포함)
dotnet publish PDFSplitterforCopilot.sln -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ===============================================
    echo 배포 완료!
    echo 실행 파일 위치: bin\Release\net8.0-windows\win-x64\publish\PDFSplitterforCopilot.exe
    echo ===============================================
    echo.
    
    REM 파일 크기 확인
    for %%I in ("bin\Release\net8.0-windows\win-x64\publish\PDFSplitterforCopilot.exe") do echo 파일 크기: %%~zI bytes
    
    echo.
    set /p test="테스트 실행하시겠습니까? (y/n): "
    if /i "%test%"=="y" (
        echo 테스트 실행 중...
        "bin\Release\net8.0-windows\win-x64\publish\PDFSplitterforCopilot.exe"
    )
) else (
    echo.
    echo 배포 실패! 오류 코드: %ERRORLEVEL%
    echo 다음을 확인해주세요:
    echo 1. .NET 8.0 SDK가 설치되어 있는지
    echo 2. 프로젝트 파일에 오류가 없는지
    echo 3. 다른 프로세스가 파일을 사용 중인지
    pause
)
