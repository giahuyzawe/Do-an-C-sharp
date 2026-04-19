@echo off
echo ==========================================
echo    BUILD FOOD TOUR APK
echo ==========================================
echo.
echo Dang build APK...
echo.

cd /d "%~dp0"

dotnet publish -f net8.0-android -c Release -p:AndroidPackageFormat=apk -p:AndroidUseAapt2=false

if errorlevel 1 (
    echo.
    echo [LOI] Build that bai!
    echo Kiem tra:
    echo - Android SDK da cai dat chua?
    echo - JDK da cai dat chua?
    echo - Visual Studio co cai Android workload khong?
    pause
    exit /b 1
)

echo.
echo [THANH CONG] APK da duoc build!
echo.

REM Copy APK to htdocs for download
for /f "delims=" %%a in ('dir /s /b "bin\Release\net8.0-android\publish\*.apk" 2^>nul') do (
    echo Copying: %%a
    copy "%%a" "c:\xampp\htdocs\foodtour-admin\uploads\FoodTour.apk" /Y
    echo.
    echo Da copy APK vao thu muc downloads!
    echo URL: https://xxx.ngrok-free.dev/foodtour-admin/uploads/FoodTour.apk
    goto :done
)

echo [CANH BAO] Khong tim thay file APK!
echo Kiem tra thu muc: bin\Release\net8.0-android\publish\

:done
pause
