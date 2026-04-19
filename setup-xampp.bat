@echo off
chcp 65001 >nul
echo ==========================================
echo   FoodStreetGuide - XAMPP Setup Script
echo ==========================================
echo.

:: Check for admin rights
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Vui lòng chạy script với quyền Administrator!
    echo.
    echo Cách chạy:
    echo 1. Nhấn chuột phải vào setup-xampp.bat
    echo 2. Chon "Run as administrator"
    pause
    exit /b 1
)

:: Set paths
set PROJECT_ADMIN=C:\Users\PC\FoodStreetGuide\FoodStreetGuide.Admin
set HTDOCS_PATH=C:\xampp\htdocs
set TARGET_PATH=%HTDOCS_PATH%\foodstreetguide

echo [1/4] Kiem tra XAMPP...
if not exist "%HTDOCS_PATH%" (
    echo [ERROR] Khong tim thay XAMPP tai %HTDOCS_PATH%
    echo Vui long cai dat XAMPP truoc!
    pause
    exit /b 1
)
echo [OK] XAMPP found!

echo.
echo [2/4] Xoa thu muc cu (neu co)...
if exist "%TARGET_PATH%" (
    rmdir /s /q "%TARGET_PATH%"
    echo [OK] Da xoa thu muc cu
)

echo.
echo [3/4] Tao symbolic link...
mklink /d "%TARGET_PATH%" "%PROJECT_ADMIN%"
if %errorlevel% neq 0 (
    echo [WARNING] Khong tao duoc symbolic link, chuyen sang copy files...
    xcopy /s /e /i /y "%PROJECT_ADMIN%\*" "%TARGET_PATH%\"
)
echo [OK] Da tao lien ket!

echo.
echo [4/4] Tao file config...
echo <?php > "%TARGET_PATH%\config.php"
echo // Auto-generated config for XAMPP >> "%TARGET_PATH%\config.php"
echo $BASE_URL = 'http://localhost/foodstreetguide'; >> "%TARGET_PATH%\config.php"
echo $PROJECT_ROOT = '%PROJECT_ADMIN%'; >> "%TARGET_PATH%\config.php"
echo ? >> "%TARGET_PATH%\config.php"
echo [OK] Config created!

echo.
echo ==========================================
echo   Setup HOAN TAT!
echo ==========================================
echo.
echo Web Admin URL: http://localhost/foodstreetguide
echo.
echo Cac buoc tiep theo:
echo 1. Mo XAMPP Control Panel
echo 2. Start Apache
echo 3. Truy cap http://localhost/foodstreetguide
echo.
pause
