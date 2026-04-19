@echo off
chcp 65001 >nul
echo ==========================================
echo   KIEM TRA XAMPP & WEB ADMIN
echo ==========================================
echo.

echo [1] Kiem tra Apache...
tasklist | findstr httpd >nul
if %errorlevel% equ 0 (
    echo [OK] Apache DANG CHAY
) else (
    echo [LOI] Apache CHUA CHAY!
    echo      -> Mo XAMPP Control Panel -> Start Apache
)

echo.
echo [2] Kiem tra file index.php...
if exist "C:\xampp\htdocs\foodtour-admin\index.php" (
    echo [OK] File index.php TON TAI
) else (
    echo [LOI] Khong tim thay index.php!
    echo      -> Kiem tra lai duong dan: C:\xampp\htdocs\foodtour-admin\
)

echo.
echo [3] Kiem tra port 80...
netstat -ano | findstr :80 | findstr LISTENING >nul
if %errorlevel% equ 0 (
    echo [OK] Port 80 dang hoat dong
) else (
    echo [CANH BAO] Khong tim thay port 80 listening
)

echo.
echo [4] URL de truy cap:
echo      http://localhost/foodtour-admin/
echo.
echo [5] Neu van loi, thu:
echo      - Ctrl + Shift + R (hard refresh)
echo      - Xoa cache browser
echo      - Kiem tra error.log trong C:\xampp\apache\logs\
echo.
pause
