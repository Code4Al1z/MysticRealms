@echo off
setlocal enabledelayedexpansion

echo.
echo =========================================
echo   Git Skip-Worktree Manager
echo   For Unity projects
echo =========================================
echo.
echo [1] Auto-detect TMP font assets (whole project)
echo [2] Scan a specific folder
echo [3] Undo skip-worktree for a folder
echo [4] Show all skip-worktree files
echo.
set /p MODE="Enter choice (1/2/3/4): "

if "%MODE%"=="1" goto MODE1
if "%MODE%"=="2" goto MODE2
if "%MODE%"=="3" goto MODE3
if "%MODE%"=="4" goto MODE4
echo Invalid choice.
goto END

:MODE1
echo.
echo Scanning Assets/ for TMP font assets...
echo.
set SKIPPED=0
set ALREADY=0
set NOTTRACKED=0
for /r "Assets" %%F in (*.asset) do (
    set "FULL=%%F"
    set "REL=!FULL:%CD%\=!"
    set "REL=!REL:\=/!"
    findstr /c:"atlasPopulationMode" "%%F" >nul 2>&1
    if !errorlevel!==0 (
        git ls-files --error-unmatch "!REL!" >nul 2>&1
        if !errorlevel!==0 (
            git ls-files -v "!REL!" | findstr /b "S " >nul 2>&1
            if !errorlevel!==0 (
                echo [ALREADY SET] !REL!
                set /a ALREADY+=1
            ) else (
                git update-index --skip-worktree "!REL!"
                echo [SET] !REL!
                set /a SKIPPED+=1
            )
        ) else (
            echo [NOT ON GITHUB] !REL!
            set /a NOTTRACKED+=1
        )
    )
)
echo.
echo Newly skipped:       %SKIPPED%
echo Already skipped:     %ALREADY%
echo Not on GitHub:       %NOTTRACKED%
goto END

:MODE2
echo.
set /p FOLDER="Folder path (e.g. Assets/_Project/Fonts): "
if not exist "%FOLDER%" (
    echo Folder not found.
    goto END
)
echo.
set SKIPPED=0
set ALREADY=0
set NOTTRACKED=0
for /r "%FOLDER%" %%F in (*.asset) do (
    set "FULL=%%F"
    set "REL=!FULL:%CD%\=!"
    set "REL=!REL:\=/!"
    git ls-files --error-unmatch "!REL!" >nul 2>&1
    if !errorlevel!==0 (
        git ls-files -v "!REL!" | findstr /b "S " >nul 2>&1
        if !errorlevel!==0 (
            echo [ALREADY SET] !REL!
            set /a ALREADY+=1
        ) else (
            git update-index --skip-worktree "!REL!"
            echo [SET] !REL!
            set /a SKIPPED+=1
        )
    ) else (
        echo [NOT ON GITHUB] !REL!
        set /a NOTTRACKED+=1
    )
)
echo.
echo Newly skipped:       %SKIPPED%
echo Already skipped:     %ALREADY%
echo Not on GitHub:       %NOTTRACKED%
goto END

:MODE3
echo.
set /p FOLDER="Folder to restore tracking for: "
if not exist "%FOLDER%" (
    echo Folder not found.
    goto END
)
echo.
set UNDONE=0
for /r "%FOLDER%" %%F in (*.asset) do (
    set "FULL=%%F"
    set "REL=!FULL:%CD%\=!"
    set "REL=!REL:\=/!"
    git update-index --no-skip-worktree "!REL!" >nul 2>&1
    echo [RESTORED] !REL!
    set /a UNDONE+=1
)
echo.
echo Restored tracking for %UNDONE% file(s).
goto END

:MODE4
echo.
echo Files currently marked as skip-worktree:
echo.
git ls-files -v | findstr /b "S "
echo.

:END
echo.
pause
