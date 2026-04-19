@echo off
chcp 65001
cls
echo ==========================================
echo 🚀 FOOD TOUR - FULL DEPLOYMENT
echo ==========================================
echo.

:: ==========================================
:: STEP 1: CHECK XAMPP
:: ==========================================
echo [1/5] 🔍 Kiểm tra XAMPP Apache...

set XAMPP_PATH=C:\xampp
if not exist "%XAMPP_PATH%\apache\bin\httpd.exe" (
    echo ❌ Khong tim thay XAMPP!
    echo Hay cai dat XAMPP tai: https://www.apachefriends.org/
    pause
    exit /b 1
)

:: Check if Apache is running
tasklist | findstr "httpd.exe" >nul
if errorlevel 1 (
    echo ⚠️  Apache chua chay!
    echo Dang khoi dong Apache...
    start /B "" "%XAMPP_PATH%\apache\bin\httpd.exe" >nul 2>&1
    timeout /t 3 /nobreak >nul
    echo ✅ Apache da khoi dong
) else (
    echo ✅ Apache dang chay
)
echo.

:: ==========================================
:: STEP 2: START NGROK
:: ==========================================
echo [2/5] 🔗 Khoi dong ngrok...

set NGROK_PATH=C:\Users\PC\Downloads\ngrok.exe
if not exist "%NGROK_PATH%" (
    echo ❌ Khong tim thay ngrok.exe
    echo Hay tai tu: https://ngrok.com/download
    pause
    exit /b 1
)

echo ✅ Ngrok found
echo.
echo 📝 Luu y: URL ngrok se hien thi o cua so moi
echo.

:: Start ngrok in new window
start "NGROK - Food Tour" cmd /c "echo ========================================== && echo NGROK URL - Copy cai nay vao browser: && echo ========================================== && echo. && \"%NGROK_PATH%\" http 80 && pause"

echo ⏳ Doi ngrok khoi dong (5 giay)...
timeout /t 5 /nobreak >nul
echo ✅ Ngrok dang chay trong cua so moi
echo.

:: ==========================================
:: STEP 3: BUILD APK
:: ==========================================
echo [3/5] 📦 Build APK...
echo.

cd /d C:\Users\PC\FoodStreetGuide

echo 🔨 Dang build project...
dotnet build -f net9.0-android -c Debug

if errorlevel 1 (
    echo ❌ Build failed!
    pause
    exit /b 1
)

echo ✅ Build successful!
echo.

:: ==========================================
:: STEP 4: CHECK EMULATOR
:: ==========================================
echo [4/5] 📱 Kiem tra Android Emulator...

set ADB_PATH=%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe
if not exist "%ADB_PATH%" (
    set ADB_PATH=C:\Users\PC\AppData\Local\Android\Sdk\platform-tools\adb.exe
)
if not exist "%ADB_PATH%" (
    set ADB_PATH=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
)

if not exist "%ADB_PATH%" (
    echo ❌ Khong tim thay ADB!
    echo Hay cai dat Android Studio va tao Emulator
    pause
    exit /b 1
)

echo ✅ ADB found: %ADB_PATH%

:: Check emulator
"%ADB_PATH%" devices | findstr "emulator" >nul
if errorlevel 1 (
    echo ⚠️  Khong co emulator nao dang chay!
    echo.
    echo 📝 Huong dan mo Emulator:
    echo 1. Mo Android Studio
    echo 2. Vao "Device Manager" (ben trai hoac Tools -^> Device Manager)
    echo 3. Chon "Create Device" hoac "Play" icon
    echo 4. Doi emulator khoi dong xong
    echo.
    echo ⏳ Nhan phim bat ky khi da mo emulator...
    pause
    
    :: Check again
    "%ADB_PATH%" devices | findstr "emulator" >nul
    if errorlevel 1 (
        echo ❌ Van khong tim thay emulator
        pause
        exit /b 1
    )
)

echo ✅ Emulator dang chay
echo.

:: ==========================================
:: STEP 5: INSTALL APK
:: ==========================================
echo [5/5] 📲 Cai dat APK len Emulator...
echo.

set APK_PATH=C:\Users\PC\FoodStreetGuide\bin\Debug\net9.0-android\com.companyname.foodstreetguide-Signed.apk

if not exist "%APK_PATH%" (
    echo ❌ Khong tim thay APK!
    echo Path: %APK_PATH%
    pause
    exit /b 1
)

echo 🗑️  Go app cu (neu co)...
"%ADB_PATH%" uninstall com.companyname.foodstreetguide >nul 2>&1

echo 📦 Cai dat APK...
"%ADB_PATH%" install "%APK_PATH%"

if errorlevel 1 (
    echo ❌ Cai dat that bai!
    pause
    exit /b 1
)

echo ✅ APK da cai dat!
echo.

:: Start app
echo 🚀 Mo app...
"%ADB_PATH%" shell am start -n com.companyname.foodstreetguide/crc64c6eb0eb18474a477.MainActivity
echo ✅ App da mo!
echo.

:: ==========================================
:: DONE
:: ==========================================
echo ==========================================
echo 🎉 DEPLOY HOAN TAT!
echo ==========================================
echo.
echo 📋 Cac buoc tiep theo:
echo.
echo 1. 📋 Xem ngrok URL trong cua so "NGROK"
echo    (Thuong la: https://xxx.ngrok-free.dev)
echo.
echo 2. 🌐 Mo browser, truy cap:
echo    https://xxx.ngrok-free.dev/foodtour-admin/
echo.
echo 3. 🔍 Mo App tren emulator, test cac chuc nang:
echo    - Mo app ^> Check Dashboard (DAU tang)
echo    - Xem nha hang ^> Check Statistics (POI View tang)
echo    - Quet QR ^> Check Check-in tang
echo.
echo 4. 📊 Xem test page:
echo    https://xxx.ngrok-free.dev/foodtour-admin/test-api.html
echo.
echo ==========================================
pause
