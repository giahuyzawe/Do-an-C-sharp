@echo off
chcp 65001
cls
echo ==========================================
echo 🚀 FOOD TOUR - DEPLOY TO ANDROID EMULATOR
echo ==========================================
echo.

REM Check if Android SDK is available
if not exist "%ANDROID_HOME%\platform-tools\adb.exe" (
    echo ❌ Android SDK not found!
    echo Please set ANDROID_HOME environment variable
    echo Or install Android Studio with SDK
    pause
    exit /b 1
)

set APK_PATH=C:\Users\PC\FoodStreetGuide\bin\Debug\net9.0-android\com.companyname.foodstreetguide-Signed.apk

REM Check if APK exists
if not exist "%APK_PATH%" (
    echo ❌ APK not found at:
    echo %APK_PATH%
    echo.
    echo Building APK first...
    echo.
    cd /d C:\Users\PC\FoodStreetGuide
    dotnet build -f net9.0-android
    if errorlevel 1 (
        echo ❌ Build failed!
        pause
        exit /b 1
    )
)

echo ✅ APK found: %APK_PATH%
echo.

REM Check if emulator is running
echo 📱 Checking Android Emulator...
"%ANDROID_HOME%\platform-tools\adb.exe" devices | findstr "emulator" >nul
if errorlevel 1 (
    echo ❌ No emulator running!
    echo.
    echo Please start Android Emulator:
    echo 1. Open Android Studio
    echo 2. Tools -^> Device Manager
    echo 3. Start an emulator (e.g., Pixel 7)
    echo.
    pause
    exit /b 1
)

echo ✅ Emulator is running
echo.

REM Uninstall old version if exists
echo 🗑️  Uninstalling old version...
"%ANDROID_HOME%\platform-tools\adb.exe" uninstall com.companyname.foodstreetguide >nul 2>&1
echo ✅ Old version removed (if any)
echo.

REM Install new APK
echo 📦 Installing new APK...
"%ANDROID_HOME%\platform-tools\adb.exe" install "%APK_PATH%"
if errorlevel 1 (
    echo ❌ Install failed!
    pause
    exit /b 1
)

echo ✅ APK installed successfully!
echo.

REM Start the app
echo 🚀 Starting Food Tour app...
"%ANDROID_HOME%\platform-tools\adb.exe" shell am start -n com.companyname.foodstreetguide/crc64c6eb0eb18474a477.MainActivity
echo ✅ App started!
echo.

echo ==========================================
echo 🎉 DEPLOY COMPLETE!
echo ==========================================
echo.
echo App is running on emulator.
echo.
echo Next steps:
echo 1. Check Web Admin at:
echo    https://false-awaken-uncooked.ngrok-free.dev/foodtour-admin/
echo.
echo 2. Test API with:
echo    https://false-awaken-uncooked.ngrok-free.dev/foodtour-admin/test-api.html
echo.
echo 3. The app will connect to Web Admin via API
echo.
pause
