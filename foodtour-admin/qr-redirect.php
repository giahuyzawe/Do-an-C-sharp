<?php
/**
 * QR Redirect Page - Universal Link Fallback
 * Handles both cases: App installed vs No app
 */
require_once 'config.php';

$token = $_GET['token'] ?? '';
$error = '';
$poi = null;
$qrCode = null;

// Load and validate QR
if ($token) {
    $qrCodes = load_json($QR_CODES_FILE);
    $pois = load_json($POIS_FILE);
    
    foreach ($qrCodes as $qr) {
        if ($qr['token'] === $token) {
            $qrCode = $qr;
            break;
        }
    }
    
    if ($qrCode) {
        // Check expired
        if ($qrCode['expiresAt'] && strtotime($qrCode['expiresAt']) < time()) {
            $error = 'QR Code đã hết hạn!';
        }
        // Check max scans
        elseif ($qrCode['maxScans'] && $qrCode['scanCount'] >= $qrCode['maxScans']) {
            $error = 'QR Code đã hết lượt sử dụng!';
        }
        // Check status
        elseif ($qrCode['status'] !== 'active') {
            $error = 'QR Code không còn hoạt động!';
        }
        else {
            // Find POI
            foreach ($pois as $p) {
                if ($p['id'] == $qrCode['poiId']) {
                    $poi = $p;
                    break;
                }
            }
            
            if (!$poi) {
                $error = 'Không tìm thấy nhà hàng!';
            } elseif ($poi['status'] !== 'approved') {
                $error = 'Nhà hàng này chưa được duyệt!';
            }
        }
    } else {
        $error = 'QR Code không hợp lệ!';
    }
} else {
    $error = 'Thiếu mã QR!';
}

// Build deep link
$deepLink = $token ? "foodtour://qr/{$token}" : '';

// Check APK file exists
$apkExists = file_exists(__DIR__ . '/uploads/FoodTour.apk') && filesize(__DIR__ . '/uploads/FoodTour.apk') > 1000;

// Track this scan attempt
if ($qrCode && !$error) {
    record_analytics('qr_scan_attempt', [
        'token' => $token,
        'poiId' => $poi['id'] ?? null,
        'userAgent' => $_SERVER['HTTP_USER_AGENT'] ?? 'unknown',
        'source' => 'web_redirect'
    ]);
}

// Check if mobile
$isMobile = preg_match('/(android|iphone|ipad|mobile)/i', $_SERVER['HTTP_USER_AGENT'] ?? '');
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= $poi ? htmlspecialchars($poi['nameVi']) : 'QR Code' ?> - Food Tour</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
    <style>
        body { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; }
        .qr-container { max-width: 500px; margin: 0 auto; padding: 20px; }
        .restaurant-card {
            background: white;
            border-radius: 20px;
            padding: 30px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            text-align: center;
        }
        .app-icon {
            width: 80px;
            height: 80px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border-radius: 20px;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 20px;
            font-size: 40px;
            color: white;
        }
        .btn-open-app {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border: none;
            color: white;
            padding: 15px 40px;
            border-radius: 30px;
            font-size: 18px;
            font-weight: bold;
            width: 100%;
            margin-bottom: 15px;
        }
        .btn-download {
            background: #28a745;
            border: none;
            color: white;
            padding: 12px 30px;
            border-radius: 25px;
            width: 100%;
        }
        .divider {
            margin: 20px 0;
            text-align: center;
            position: relative;
        }
        .divider::before {
            content: '';
            position: absolute;
            top: 50%;
            left: 0;
            right: 0;
            height: 1px;
            background: #ddd;
        }
        .divider span {
            background: white;
            padding: 0 15px;
            position: relative;
            color: #666;
        }
        .restaurant-info {
            background: #f8f9fa;
            border-radius: 15px;
            padding: 20px;
            margin: 20px 0;
            text-align: left;
        }
        .restaurant-info h5 {
            color: #333;
            margin-bottom: 10px;
        }
        .restaurant-info p {
            color: #666;
            margin-bottom: 5px;
        }
        .badge-status {
            display: inline-block;
            padding: 5px 15px;
            border-radius: 20px;
            font-size: 12px;
            margin-top: 10px;
        }
        .trying-app {
            display: none;
            padding: 20px;
            background: #e3f2fd;
            border-radius: 15px;
            margin-bottom: 20px;
        }
        .spinner {
            width: 40px;
            height: 40px;
            border: 4px solid #667eea;
            border-top-color: transparent;
            border-radius: 50%;
            animation: spin 1s linear infinite;
            margin: 0 auto 15px;
        }
        @keyframes spin { to { transform: rotate(360deg); } }
    </style>
</head>
<body>
    <div class="qr-container">
        <div class="restaurant-card">
            <?php if ($error): ?>
                <!-- Error State -->
                <div class="app-icon bg-danger">
                    <i class="bi bi-exclamation-triangle"></i>
                </div>
                <h4 class="text-danger mb-3"><?= $error ?></h4>
                <p class="text-muted">Vui lòng kiểm tra lại QR Code hoặc liên hệ chủ nhà hàng.</p>
                <a href="index.php" class="btn btn-outline-primary mt-3">
                    <i class="bi bi-house"></i> Về trang chủ
                </a>
                
            <?php elseif ($poi): ?>
                <!-- Success - Restaurant Found -->
                <div class="app-icon">🍜</div>
                <h4><?= htmlspecialchars($poi['nameVi']) ?></h4>
                <p class="text-muted"><?= htmlspecialchars($poi['address']) ?></p>
                
                <?php if ($poi['status'] === 'approved'): ?>
                    <span class="badge-status bg-success text-white">✓ Đã duyệt</span>
                <?php endif; ?>
                
                <!-- Trying to open app indicator -->
                <div id="tryingApp" class="trying-app">
                    <div class="spinner"></div>
                    <p class="mb-0">Đang mở ứng dụng Food Tour...</p>
                </div>
                
                <!-- Primary: Open in App -->
                <button id="btnOpenApp" class="btn-open-app" onclick="openApp()">
                    <i class="bi bi-box-arrow-up-right"></i> Mở trong ứng dụng
                </button>
                
                <div class="divider"><span>hoặc</span></div>
                
                <!-- Download App for Android -->
                <div id="downloadSection" style="margin-top: 20px;">
                    <p class="text-muted mb-3"><strong>Chưa cài đặt ứng dụng?</strong></p>
                    
                    <?php if ($apkExists): ?>
                    <!-- APK Available -->
                    <a href="/foodtour-admin/download/FoodStreetGuide-v2.apk" class="btn btn-success w-100 mb-2" download>
                        <i class="bi bi-download"></i> <strong>Tải APK cho Android</strong>
                        <br><small style="font-size: 11px;">Cài đặt trực tiếp (không cần Play Store)</small>
                    </a>
                    
                    <div class="alert alert-warning py-2" style="font-size: 12px;">
                        <strong><i class="bi bi-info-circle"></i> Hướng dẫn cài APK:</strong>
                        <ol class="mb-0 mt-1" style="padding-left: 18px;">
                            <li>Bấm nút tải APK ở trên</li>
                            <li>Mở file đã tải (trong thông báo)</li>
                            <li>Cho phép "Cài đặt ứng dụng không rõ nguồn gốc"</li>
                            <li>Cài đặt và mở app</li>
                        </ol>
                    </div>
                    <?php else: ?>
                    <!-- APK Not Available -->
                    <div class="alert alert-info">
                        <h6><i class="bi bi-android2"></i> App Android đang được build</h6>
                        <p style="font-size: 13px; margin-bottom: 10px;">
                            File APK chưa sẵn sàng. Vui lòng:
                        </p>
                        <ol class="text-start mb-0" style="font-size: 12px; padding-left: 18px;">
                            <li>Chạy file <code>build-apk.bat</code> trong thư mục project</li>
                            <li>Hoặc liên hệ dev để nhận APK</li>
                            <li>Hoặc dùng QR code trên Android đã cài app test</li>
                        </ol>
                    </div>
                    <?php endif; ?>
                    
                    <!-- iOS Info -->
                    <div class="mt-3 pt-3 border-top">
                        <p class="text-muted mb-2" style="font-size: 13px;">
                            <i class="bi bi-apple"></i> <strong>iOS:</strong> Liên hệ dev để build app
                        </p>
                        <a href="https://apps.apple.com/app/food-tour/id123456789" class="btn btn-outline-secondary btn-sm w-100 disabled" onclick="alert('App iOS đang phát triển. Vui lòng dùng Android hoặc liên hệ dev.'); return false;">
                            <i class="bi bi-apple"></i> iOS (Coming Soon)
                        </a>
                    </div>
                </div>
                
                <!-- Hidden timer message -->
                <div id="fallbackMessage" style="display: none; margin-top: 15px; color: #666;">
                    <small>Nếu ứng dụng không mở, vui lòng <a href="#" onclick="showDownload()">tải ứng dụng</a></small>
                </div>
                
            <?php else: ?>
                <!-- Invalid QR -->
                <div class="app-icon bg-warning">
                    <i class="bi bi-qr-code-scan"></i>
                </div>
                <h4>QR Code không hợp lệ</h4>
                <p class="text-muted">Vui lòng quét lại hoặc liên hệ hỗ trợ.</p>
            <?php endif; ?>
        </div>
        
        <!-- Footer -->
        <div class="text-center text-white mt-4" style="opacity: 0.8;">
            <small>🍜 Food Tour - Ẩm thực đường phố Vĩnh Khánh</small>
        </div>
    </div>

    <script>
        const deepLink = '<?= $deepLink ?>';
        let appOpened = false;
        
        // Auto try to open app on mobile
        <?php if ($isMobile && $deepLink): ?>
        window.onload = function() {
            document.getElementById('tryingApp').style.display = 'block';
            document.getElementById('btnOpenApp').style.display = 'none';
            
            // Try to open app
            setTimeout(function() {
                window.location.href = deepLink;
                
                // Show fallback after 2.5 seconds if app didn't open
                setTimeout(function() {
                    if (!appOpened) {
                        document.getElementById('tryingApp').style.display = 'none';
                        document.getElementById('btnOpenApp').style.display = 'block';
                        document.getElementById('fallbackMessage').style.display = 'block';
                    }
                }, 2500);
            }, 500);
        };
        <?php endif; ?>
        
        function openApp() {
            appOpened = true;
            window.location.href = deepLink;
            
            // Show fallback option after delay
            setTimeout(function() {
                document.getElementById('fallbackMessage').style.display = 'block';
            }, 3000);
        }
        
        function showDownload() {
            document.getElementById('downloadSection').scrollIntoView({ behavior: 'smooth' });
        }
    </script>
</body>
</html>
