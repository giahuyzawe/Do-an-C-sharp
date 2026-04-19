# Test Automation Script for Food Street Guide
# Run: .\test-automation.ps1

Write-Host "🧪 FOOD STREET GUIDE - AUTOMATED TEST" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check XAMPP
Write-Host "`n[1] Checking XAMPP..." -ForegroundColor Yellow
if (Test-Path "C:\xampp\apache\logs\access.log") {
    Write-Host "   ✅ XAMPP found" -ForegroundColor Green
} else {
    Write-Host "   ❌ XAMPP not found! Start XAMPP first." -ForegroundColor Red
    exit 1
}

# Check Web Admin API
Write-Host "`n[2] Testing Web Admin API..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost/foodtour-admin/api/get-pois.php" -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
        $data = $response.Content | ConvertFrom-Json
        Write-Host "   ✅ API Online - $($data.count) POIs" -ForegroundColor Green
    }
} catch {
    Write-Host "   ❌ API Error: $_" -ForegroundColor Red
}

# Check ADB
Write-Host "`n[3] Checking ADB..." -ForegroundColor Yellow
$adb = "C:\Users\$env:USERNAME\AppData\Local\Android\Sdk\platform-tools\adb.exe"
if (Test-Path $adb) {
    Write-Host "   ✅ ADB found" -ForegroundColor Green
    
    # Check device
    $devices = & $adb devices
    if ($devices -match "emulator") {
        Write-Host "   ✅ Emulator connected" -ForegroundColor Green
        
        # Set location to POI 1
        Write-Host "`n[4] Setting location to POI 1..." -ForegroundColor Yellow
        & $adb emu geo fix 105.550000 21.350000
        Write-Host "   📍 Location: 21.350000, 105.550000 (Phở Gà Vĩnh Phúc)" -ForegroundColor Cyan
        
        # Check logcat
        Write-Host "`n[5] Monitoring logs (5 seconds)..." -ForegroundColor Yellow
        $job = Start-Job { 
            param($adbPath)
            & $adbPath logcat -d | Select-String -Pattern "POI.*sync|Geofence|Entered" | Select-Object -Last 10
        } -ArgumentList $adb
        
        Start-Sleep -Seconds 5
        $logs = Receive-Job $job
        Remove-Job $job
        
        if ($logs) {
            Write-Host "   ✅ App activity detected:" -ForegroundColor Green
            $logs | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
        } else {
            Write-Host "   ⚠️ No app logs found" -ForegroundColor Yellow
        }
        
    } else {
        Write-Host "   ❌ No emulator connected" -ForegroundColor Red
        Write-Host "      Start Android Emulator first!" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ❌ ADB not found" -ForegroundColor Red
}

# Test complete
Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Cyan
Write-Host "`nNext steps:" -ForegroundColor White
Write-Host "1. Open Food Street Guide app on emulator"
Write-Host "2. Check tab 'Bản đồ' - should show 20 POI pins"
Write-Host "3. Click marker near (21.35, 105.55)"
Write-Host "4. Should show 'Phở Gà Vĩnh Phúc'"
Write-Host "5. Switch to 'Khám phá' tab"
Write-Host "6. Try filters: 'Gần nhất', 'Đánh giá cao', 'Đang mở'"
Write-Host "7. Add a review in POI detail page"
