@echo off
cd /d "%~dp0"
echo === Debugging DocFX File Detection ===
echo.
echo Current directory:
cd
echo.
echo Looking for index.md:
if exist "index.md" (
    echo  index.md found
) else (
    echo  index.md NOT found
)
echo.
echo Looking for articles\intro.md:
if exist "articles\intro.md" (
    echo  articles\intro.md found
) else (
    echo  articles\intro.md NOT found
)
echo.
echo All .md files in current directory:
dir *.md /b
echo.
echo All .md files in articles:
dir articles\*.md /b 2>nul
pause