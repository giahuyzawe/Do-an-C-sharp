<?php
/**
 * Trang tải xuống APK cho FoodStreetGuide App
 * User quét QR → Vào trang này → Tải APK
 */

// Cấu hình
$appName = "FoodStreetGuide";
$appVersion = "1.0.0";
$apkFile = "FoodStreetGuide.apk";  // File APK cần đặt trong cùng thư mục
$qrCodeUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" . urlencode("https://" . $_SERVER['HTTP_HOST'] . $_SERVER['REQUEST_URI']);

// Kiểm tra file APK có tồn tại không
$apkExists = file_exists($apkFile);
$apkSize = $apkExists ? round(filesize($apkFile) / (1024 * 1024), 2) : 0;

?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="description" content="Tải xuống FoodStreetGuide App - Khám phá ẩm thực đường phố">
    <title>Tải FoodStreetGuide App</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <style>
        :root {
            --primary: #FF6B35;
            --primary-dark: #E55A2B;
            --secondary: #2EC4B6;
            --dark: #1a1a2e;
            --light: #f8f9fa;
        }
        
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, var(--dark) 0%, #16213e 100%);
            min-height: 100vh;
            color: white;
        }
        
        .download-container {
            max-width: 600px;
            margin: 0 auto;
            padding: 40px 20px;
        }
        
        .app-icon {
            width: 120px;
            height: 120px;
            background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
            border-radius: 24px;
            display: flex;
            align-items: center;
            justify-content: center;
            margin: 0 auto 30px;
            font-size: 60px;
            box-shadow: 0 10px 40px rgba(255, 107, 53, 0.3);
        }
        
        .app-name {
            font-size: 2rem;
            font-weight: 700;
            text-align: center;
            margin-bottom: 10px;
        }
        
        .app-tagline {
            text-align: center;
            color: rgba(255,255,255,0.7);
            font-size: 1.1rem;
            margin-bottom: 40px;
        }
        
        .download-btn {
            background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
            border: none;
            padding: 18px 40px;
            font-size: 1.2rem;
            font-weight: 600;
            border-radius: 50px;
            color: white;
            width: 100%;
            margin-bottom: 20px;
            transition: all 0.3s;
            box-shadow: 0 5px 20px rgba(255, 107, 53, 0.4);
        }
        
        .download-btn:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 30px rgba(255, 107, 53, 0.5);
        }
        
        .download-btn:disabled {
            background: #666;
            cursor: not-allowed;
            transform: none;
        }
        
        .file-info {
            background: rgba(255,255,255,0.1);
            border-radius: 15px;
            padding: 20px;
            margin-bottom: 30px;
            backdrop-filter: blur(10px);
        }
        
        .qr-section {
            background: rgba(255,255,255,0.05);
            border-radius: 20px;
            padding: 30px;
            text-align: center;
            margin-top: 30px;
        }
        
        .qr-code {
            background: white;
            padding: 15px;
            border-radius: 15px;
            display: inline-block;
            margin-bottom: 15px;
        }
        
        .qr-code img {
            width: 200px;
            height: 200px;
        }
        
        .features {
            margin: 40px 0;
        }
        
        .feature-item {
            display: flex;
            align-items: center;
            margin-bottom: 20px;
            padding: 15px;
            background: rgba(255,255,255,0.05);
            border-radius: 12px;
        }
        
        .feature-icon {
            width: 50px;
            height: 50px;
            background: rgba(255,255,255,0.1);
            border-radius: 12px;
            display: flex;
            align-items: center;
            justify-content: center;
            margin-right: 15px;
            font-size: 24px;
        }
        
        .steps {
            background: rgba(255,255,255,0.05);
            border-radius: 20px;
            padding: 25px;
            margin-top: 30px;
        }
        
        .step {
            display: flex;
            margin-bottom: 20px;
        }
        
        .step-number {
            width: 35px;
            height: 35px;
            background: var(--primary);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: 700;
            margin-right: 15px;
            flex-shrink: 0;
        }
        
        .warning-box {
            background: rgba(255, 193, 7, 0.2);
            border: 1px solid #ffc107;
            border-radius: 12px;
            padding: 20px;
            margin: 20px 0;
        }
        
        .android-icon {
            font-size: 1.5rem;
            margin-right: 10px;
        }
        
        @media (max-width: 576px) {
            .app-name {
                font-size: 1.5rem;
            }
            .download-btn {
                font-size: 1rem;
                padding: 15px 30px;
            }
        }
    </style>
</head>
<body>
    <div class="download-container">
        <!-- App Icon & Info -->
        <div class="app-icon">🍜</div>
        <h1 class="app-name">FoodStreetGuide</h1>
        <p class="app-tagline">Khám phá ẩm thực đường phố</p>
        
        <!-- APK Info -->
        <div class="file-info">
            <div class="d-flex justify-content-between align-items-center mb-2">
                <span><i class="bi bi-android2 android-icon text-success"></i>Android App</span>
                <span class="badge bg-light text-dark">v<?php echo $appVersion; ?></span>
            </div>
            <?php if ($apkExists): ?>
                <div class="d-flex justify-content-between text-white-50">
                    <span>File: <?php echo $apkFile; ?></span>
                    <span><?php echo $apkSize; ?> MB</span>
                </div>
            <?php else: ?>
                <div class="text-warning">
                    <i class="bi bi-exclamation-triangle"></i> File APK chưa được upload
                </div>
            <?php endif; ?>
        </div>
        
        <!-- Download Button -->
        <?php if ($apkExists): ?>
            <a href="<?php echo $apkFile; ?>" download class="btn download-btn text-decoration-none">
                <i class="bi bi-download me-2"></i>Tải xuống APK
            </a>
            <p class="text-center text-white-50 small">
                Nhấn và giữ để tải xuống
            </p>
        <?php else: ?>
            <button class="btn download-btn" disabled>
                <i class="bi bi-x-circle me-2"></i>APK chưa sẵn sàng
            </button>
            <div class="warning-box">
                <strong><i class="bi bi-info-circle me-2"></i>Hướng dẫn upload APK:</strong><br>
                1. Build app: <code>dotnet publish -f net8.0-android -c Release</code><br>
                2. Tìm file APK trong <code>bin/Release/net8.0-android/</code><br>
                3. Copy file APK vào thư mục admin cùng với file này
            </div>
        <?php endif; ?>
        
        <!-- Features -->
        <div class="features">
            <h5 class="mb-3"><i class="bi bi-stars me-2 text-warning"></i>Tính năng nổi bật</h5>
            
            <div class="feature-item">
                <div class="feature-icon">🗺️</div>
                <div>
                    <strong>Bản đồ tương tác</strong><br>
                    <small class="text-white-50">Tìm quán ăn gần bạn</small>
                </div>
            </div>
            
            <div class="feature-item">
                <div class="feature-icon">🎧</div>
                <div>
                    <strong>Thuyết minh tự động</strong><br>
                    <small class="text-white-50">Nghe lịch sử quán khi đến gần</small>
                </div>
            </div>
            
            <div class="feature-item">
                <div class="feature-icon">⭐</div>
                <div>
                    <strong>Đánh giá & Review</strong><br>
                    <small class="text-white-50">Chia sẻ trải nghiệm ẩm thực</small>
                </div>
            </div>
            
            <div class="feature-item">
                <div class="feature-icon">🌐</div>
                <div>
                    <strong>Đa ngôn ngữ</strong><br>
                    <small class="text-white-50">Tiếng Việt & English</small>
                </div>
            </div>
        </div>
        
        <!-- Install Steps -->
        <div class="steps">
            <h5 class="mb-3"><i class="bi bi-phone me-2"></i>Cách cài đặt</h5>
            
            <div class="step">
                <div class="step-number">1</div>
                <div>
                    <strong>Tải file APK</strong><br>
                    <small class="text-white-50">Nhấn nút tải xuống bên trên</small>
                </div>
            </div>
            
            <div class="step">
                <div class="step-number">2</div>
                <div>
                    <strong>Bật "Unknown Sources"</strong><br>
                    <small class="text-white-50">Settings → Security → Cho phép cài đặt từ nguồn không xác định</small>
                </div>
            </div>
            
            <div class="step">
                <div class="step-number">3</div>
                <div>
                    <strong>Cài đặt app</strong><br>
                    <small class="text-white-50">Mở file APK đã tải và nhấn "Install"</small>
                </div>
            </div>
            
            <div class="step">
                <div class="step-number">4</div>
                <div>
                    <strong>Thưởng thức!</strong><br>
                    <small class="text-white-50">Mở app và khám phá ẩm thực đường phố</small>
                </div>
            </div>
        </div>
        
        <!-- QR Code -->
        <div class="qr-section">
            <h5 class="mb-3"><i class="bi bi-qr-code me-2"></i>Quét mã để tải</h5>
            <div class="qr-code">
                <img src="<?php echo $qrCodeUrl; ?>" alt="QR Code Download">
            </div>
            <p class="text-white-50 mb-0">Quét bằng camera điện thoại để vào trang này</p>
        </div>
        
        <!-- Footer -->
        <div class="text-center mt-4 text-white-50 small">
            <p>© 2025 FoodStreetGuide - Đồ án sinh viên</p>
            <p>Nguyễn Gia Huy | Vũ Gia Huy</p>
        </div>
    </div>
    
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
    
    <script>
        // Track download
        document.querySelector('.download-btn').addEventListener('click', function() {
            console.log('APK download started');
            // Có thể thêm Google Analytics hoặc tracking ở đây
        });
        
        // Detect device
        const isAndroid = /Android/i.test(navigator.userAgent);
        const isIOS = /iPhone|iPad|iPod/i.test(navigator.userAgent);
        
        if (isIOS) {
            document.querySelector('.warning-box').style.display = 'block';
            document.querySelector('.warning-box').innerHTML = 
                '<strong><i class="bi bi-apple me-2"></i>Lưu ý:</strong> ' +
                'Bạn đang dùng iOS. File APK chỉ cài được trên Android. ' +
                'Vui lòng dùng điện thoại Android để cài đặt.';
        }
    </script>
</body>
</html>
