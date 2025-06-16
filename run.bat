@echo off
SETLOCAL EnableDelayedExpansion
chcp 65001 >nul
TITLE PDF Splitter 완전 재시작

echo ===================================
echo   PDF Splitter 완전 재빌드 시스템
echo ===================================
echo.

REM 1. 모든 관련 프로세스 강제 종료
echo [1/5] 실행 중인 프로세스 종료...
taskkill /F /IM PDFSplitterforCopilot.exe /T 2>nul

if !ERRORLEVEL! EQU 0 (
    echo - 프로세스 종료 완료
    echo - 잠금 해제를 위한 대기...
    timeout /t 2 /nobreak >nul
) else (
    echo - 종료할 프로세스가 없음
)

REM 2. 빌드 폴더 초기화 (이전 빌드 파일 완전 삭제)
echo.
echo [2/5] 빌드 폴더 초기화...
rmdir /S /Q "bin" 2>nul
rmdir /S /Q "obj" 2>nul

REM 3. 패키지 복원 (NuGet 의존성)
echo.
echo [3/5] 패키지 복원...
dotnet restore PDFSplitterforCopilot.sln --no-cache
if !ERRORLEVEL! NEQ 0 (
    echo - 패키지 복원 실패
    goto ERROR
) else (
    echo - 패키지 복원 완료
)

REM 4. 프로젝트 빌드
echo.
echo [4/5] 프로젝트 빌드...
dotnet build PDFSplitterforCopilot.sln --verbosity minimal
if !ERRORLEVEL! NEQ 0 (
    echo - 빌드 실패
    goto ERROR
) else (
    echo - 빌드 성공
)

REM 5. 애플리케이션 실행
echo.
echo [5/5] 애플리케이션 실행...
if exist "bin\Debug\net8.0-windows\PDFSplitterforCopilot.exe" (
    echo - 애플리케이션 시작 중...
    start /MAX "" "bin\Debug\net8.0-windows\PDFSplitterforCopilot.exe"
    ping -n 2 127.0.0.1 > nul
    echo - 애플리케이션이 새 창에서 실행되었습니다.
    goto SUCCESS
) else (
    echo - 실행 파일을 찾을 수 없습니다.
    goto ERROR
)

:SUCCESS
echo.
echo ===================================
echo    빌드 및 실행 성공!
echo ===================================
goto END

:ERROR
echo.
echo ===================================
echo    오류가 발생했습니다!
echo ===================================
goto END

:END
pause
