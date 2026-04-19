@echo off
echo ==========================================
echo    START NGROK - Food Tour Admin
echo ==========================================
echo.

REM Use full path to ngrok
set NGROK_PATH=C:\Users\PC\Downloads\ngrok.exe

if not exist "%NGROK_PATH%" (
    echo ❌ Khong tim thay ngrok.exe tai: %NGROK_PATH%
    echo.
    echo Hay tai ngrok tu: https://ngrok.com/download
    echo Va dat vao: C:\Users\PC\Downloads\
    pause
    exit /b 1
)

echo ✅ Ngrok found: %NGROK_PATH%
echo.
echo Dang khoi dong ngrok...
echo.
echo URL se hien thi ben duoi:
echo ==========================================
echo.

"%NGROK_PATH%" http 80

echo.
echo ==========================================
echo Neu ngrok khong chay, hay kiem tra:
echo 1. Ngrok da duoc cai dat trong PATH
echo 2. Authtoken da duoc cau hinh
echo 3. Port 80 dang duoc Apache su dung
echo ==========================================
pause
