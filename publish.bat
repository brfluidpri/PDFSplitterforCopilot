@echo off
chcp 65001 >nul 2>&1
setlocal enabledelayedexpansion

REM 배포 경로 설정 - 단순화된 경로로 변경
set "PUBLISH_PATH=publish"
set "EXE_PATH=%PUBLISH_PATH%\PDFSplitterforCopilot.exe"

echo 🚀 PDF Splitter 배포 중...
echo.

REM 이전 배포 파일 정리
if exist "%PUBLISH_PATH%" (
    echo 📁 이전 배포 파일 정리 중...
    rmdir /s /q "%PUBLISH_PATH%" 2>nul
    if exist "%PUBLISH_PATH%" (
        echo ❌ 이전 파일 삭제 실패. 다른 프로세스가 사용 중일 수 있습니다.
        pause
        exit /b 1
    )
    echo ✅ 이전 파일 정리 완료
)

echo 🔑 라이선스 키 임베드 중...
powershell -ExecutionPolicy Bypass -File "EmbedLicense.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo ⚠️ 라이선스 키 임베드에 실패했지만 계속 진행합니다.
)
echo.

echo 🔨 프로젝트 빌드 및 배포 중...
echo.

REM 단일 실행 파일로 배포 (단순화된 명령어)
dotnet publish PDFSplitterforCopilot.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -o ./publish

echo.
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ================================================
    echo ✅ 배포 완료!
    echo 📍 실행 파일 위치: %EXE_PATH%
    echo ================================================
    echo.
    
    REM 파일 크기 확인
    if exist "%EXE_PATH%" (
        for %%I in ("%EXE_PATH%") do (
            set "filesize=%%~zI"
            set /a "filesizeMB=!filesize!/1024/1024"
            echo 📊 파일 크기: !filesize! bytes (~!filesizeMB!MB)
        )
    ) else (
        echo ❌ 실행 파일을 찾을 수 없습니다.
        pause
        exit /b 1
    )
    
    echo.
    :ask_test
    set /p test="🧪 테스트 실행하시겠습니까? (y/n) [기본값: y]: "
    if "!test!"=="" set "test=y"
    if /i "!test!"=="y" (
        echo 🏃 테스트 실행 중...
        "%EXE_PATH%"
    ) else if /i "!test!"=="n" (
        echo 👋 배포가 완료되었습니다.
    ) else (
        echo ❌ y 또는 n을 입력해주세요.
        goto ask_test
    )
) else (
    echo.
    echo ================================================
    echo ❌ 배포 실패! 오류 코드: %ERRORLEVEL%
    echo ================================================
    echo 다음을 확인해주세요:
    echo 1️⃣ .NET 8.0 SDK가 설치되어 있는지
    echo 2️⃣ 프로젝트 파일에 오류가 없는지
    echo 3️⃣ 다른 프로세스가 파일을 사용 중인지
    echo 4️⃣ 디스크 공간이 충분한지
    echo.
    pause
    exit /b %ERRORLEVEL%
)

endlocal
