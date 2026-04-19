@echo off
echo ==========================================
echo    FIX NGROK AUTHTOKEN
echo ==========================================
echo.
echo Hay lay authtoken tu: https://dashboard.ngrok.com/get-started/your-authtoken
echo.
echo Sau do chay lenh:
echo   ngrok config add-authtoken YOUR_TOKEN
echo.
echo Hoac mo file:
echo   C:\Users\PC\AppData\Local\ngrok\ngrok.yml
echo.
echo Va sua lai:
echo   version: "2"
echo   authtoken: "YOUR_ACTUAL_TOKEN_HERE"
echo.
pause
