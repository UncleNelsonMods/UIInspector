@echo off
set MOD_NAME=UIInspectorMod
set GAME_PATH=C:\Program Files (x86)\Steam\steamapps\common\Schedule I

echo Building %MOD_NAME%...
dotnet build -c Release /p:GamePath="%GAME_PATH%"

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Copying to Git repo root...
copy /Y "bin\Release\net6.0\%MOD_NAME%.dll" "."

echo Copying to game Mods folder...
copy /Y "bin\Release\net6.0\%MOD_NAME%.dll" "%GAME_PATH%\Mods\"

echo Build complete!
pause 