echo off
echo ==========================================
echo FoodStreetGuide API Setup for XAMPP
echo ==========================================
echo.

set SOURCE_DIR=%~dp0FoodStreetGuide.Admin\api
set DEST_DIR=C:\xampp\htdocs\FoodStreetGuide.Admin\api

echo Source: %SOURCE_DIR%
echo Destination: %DEST_DIR%
echo.

if not exist "C:\xampp\htdocs\FoodStreetGuide.Admin" (
    echo Creating FoodStreetGuide.Admin directory...
    mkdir "C:\xampp\htdocs\FoodStreetGuide.Admin"
)

if not exist "%DEST_DIR%" (
    echo Creating api directory...
    mkdir "%DEST_DIR%"
)

echo Copying API files...
copy "%SOURCE_DIR%\pois.php" "%DEST_DIR%\"
copy "%SOURCE_DIR%\.htaccess" "%DEST_DIR%\"

echo.
echo ==========================================
echo API files copied successfully!
echo ==========================================
echo.
echo Please make sure XAMPP Apache is running
echo Test URL: http://localhost/FoodStreetGuide.Admin/api/pois.php?api_key=foodstreet_mobile_2024
echo.
pause
