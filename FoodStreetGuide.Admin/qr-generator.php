<?php
/**
 * Trang tạo QR Code trong Web Admin
 * Cho phép Admin tạo QR để quét vào app hoặc đến từng POI cụ thể
 */

session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

// Load POIs for selection
$storage_file = 'pois.json';
$pois = [];
if (file_exists($storage_file)) {
    $pois = json_decode(file_get_contents($storage_file), true) ?: [];
}

// Base URL for deep links
$baseUrl = 'http://' . $_SERVER['HTTP_HOST'] . dirname($_SERVER['PHP_SELF']);
$appDownloadUrl = $baseUrl . '/download.php';
$deepLinkScheme = 'foodstreetguide://poi/';

// Handle QR generation
$selectedPoiId = $_GET['poi_id'] ?? null;
$qrType = $_GET['type'] ?? 'app'; // 'app' or 'poi'
$qrContent = '';
$qrTitle = '';

if ($qrType === 'poi' && $selectedPoiId) {
    // Find POI
    $selectedPoi = null;
    foreach ($pois as $poi) {
        if ($poi['id'] == $selectedPoiId) {
            $selectedPoi = $poi;
            break;
        }
    }
    if ($selectedPoi) {
        $qrContent = $deepLinkScheme . $selectedPoi['id'];
        $qrTitle = 'QR cho: ' . htmlspecialchars($selectedPoi['nameVi']);
    }
} else {
    // App download QR
    $qrContent = $appDownloadUrl;
    $qrTitle = 'QR Tải App';
}

// Generate QR Code URL using QRServer API
$qrImageUrl = '';
if ($qrContent) {
    $qrImageUrl = 'https://api.qrserver.com/v1/create-qr-code/?size=400x400&margin=10&data=' . urlencode($qrContent);
}

?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Tạo QR Code - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --primary: #FF6B35;
            --primary-dark: #E55A2B;
            --secondary: #2EC4B6;
            --sidebar-bg: #1E293B;
            --sidebar-width: 260px;
        }
        body {
            font-family: 'Inter', sans-serif;
            background: #F8FAFC;
        }
        .sidebar {
            position: fixed;
            top: 0;
            left: 0;
            width: var(--sidebar-width);
            height: 100vh;
            background: var(--sidebar-bg);
            color: white;
            padding: 1.5rem;
            z-index: 1000;
        }
        .sidebar-brand {
            font-size: 1.25rem;
            font-weight: 700;
            margin-bottom: 2rem;
            display: flex;
            align-items: center;
            gap: 0.75rem;
        }
        .nav-link {
            color: #94A3B8;
            padding: 0.75rem 1rem;
            border-radius: 0.5rem;
            margin-bottom: 0.25rem;
            display: flex;
            align-items: center;
            gap: 0.75rem;
            transition: all 0.2s;
            text-decoration: none;
        }
        .nav-link:hover, .nav-link.active {
            color: white;
            background: rgba(255,255,255,0.1);
        }
        .main-content {
            margin-left: var(--sidebar-width);
            padding: 2rem;
        }
        .card {
            border: none;
            border-radius: 1rem;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
        }
        .card-header {
            background: white;
            border-bottom: 2px solid #F1F5F9;
            padding: 1.5rem;
            border-radius: 1rem 1rem 0 0 !important;
        }
        .qr-preview {
            background: white;
            border-radius: 1rem;
            padding: 30px;
            text-align: center;
            border: 2px dashed #E2E8F0;
        }
        .qr-code-container {
            background: white;
            padding: 20px;
            border-radius: 16px;
            display: inline-block;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
            margin-bottom: 20px;
        }
        .qr-code-container img {
            width: 300px;
            height: 300px;
            border-radius: 8px;
        }
        .qr-info {
            background: #F8FAFC;
            border-radius: 12px;
            padding: 15px;
            margin-top: 15px;
        }
        .btn-generate {
            background: linear-gradient(135deg, var(--primary) 0%, var(--primary-dark) 100%);
            border: none;
            padding: 12px 30px;
            font-weight: 600;
            border-radius: 50px;
        }
        .poi-item {
            cursor: pointer;
            padding: 12px;
            border-radius: 10px;
            transition: all 0.2s;
            border: 2px solid transparent;
        }
        .poi-item:hover {
            background: #F1F5F9;
            border-color: var(--primary);
        }
        .poi-item.selected {
            background: rgba(255, 107, 53, 0.1);
            border-color: var(--primary);
        }
        .qr-type-btn {
            padding: 15px 25px;
            border-radius: 12px;
            border: 2px solid #E2E8F0;
            background: white;
            cursor: pointer;
            transition: all 0.3s;
            text-align: center;
        }
        .qr-type-btn:hover {
            border-color: var(--primary);
            transform: translateY(-2px);
        }
        .qr-type-btn.active {
            border-color: var(--primary);
            background: rgba(255, 107, 53, 0.1);
        }
        .qr-type-btn i {
            font-size: 2rem;
            margin-bottom: 10px;
            color: var(--primary);
        }
        .download-section {
            background: linear-gradient(135deg, #F8FAFC 0%, #E0F2FE 100%);
            border-radius: 16px;
            padding: 25px;
            margin-top: 20px;
        }
        .print-qr {
            display: none;
        }
        @media print {
            .sidebar, .btn-group, .card-header, .nav-link {
                display: none !important;
            }
            .main-content {
                margin-left: 0;
            }
            .print-qr {
                display: block;
            }
            .qr-code-container {
                box-shadow: none;
                border: 1px solid #ddd;
            }
        }
    </style>
</head>
<body>
    <!-- Sidebar -->
    <div class="sidebar">
        <div class="sidebar-brand">
            <i class="bi bi-qr-code-scan"></i>
            QR Generator
        </div>
        <nav class="nav flex-column">
            <a class="nav-link" href="index.php"><i class="bi bi-grid"></i> Dashboard</a>
            <a class="nav-link" href="pois.php"><i class="bi bi-geo"></i> Quản lý POI</a>
            <a class="nav-link" href="reviews.php"><i class="bi bi-star-fill"></i> Reviews</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link active" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
            <a class="nav-link" href="permissions.php"><i class="bi bi-shield-lock"></i> Phân quyền</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <!-- Main Content -->
    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div>
                <h2 class="mb-1"><i class="bi bi-qr-code me-2 text-primary"></i>Tạo QR Code</h2>
                <p class="text-muted mb-0">Tạo mã QR để người dùng quét vào app hoặc đến POI cụ thể</p>
            </div>
        </div>

        <div class="row g-4">
            <!-- Left Column - Options -->
            <div class="col-lg-5">
                <div class="card">
                    <div class="card-header">
                        <h5 class="mb-0"><i class="bi bi-sliders me-2"></i>Chọn loại QR</h5>
                    </div>
                    <div class="card-body p-4">
                        <!-- QR Type Selection -->
                        <div class="row g-3 mb-4">
                            <div class="col-6">
                                <a href="?type=app" class="text-decoration-none">
                                    <div class="qr-type-btn <?php echo $qrType === 'app' ? 'active' : ''; ?>">
                                        <i class="bi bi-phone"></i>
                                        <div class="fw-bold">Tải App</div>
                                        <small class="text-muted">Quét để tải APK</small>
                                    </div>
                                </a>
                            </div>
                            <div class="col-6">
                                <a href="?type=poi" class="text-decoration-none">
                                    <div class="qr-type-btn <?php echo $qrType === 'poi' ? 'active' : ''; ?>">
                                        <i class="bi bi-geo-alt"></i>
                                        <div class="fw-bold">Đến POI</div>
                                        <small class="text-muted">Quét đến quán cụ thể</small>
                                    </div>
                                </a>
                            </div>
                        </div>

                        <?php if ($qrType === 'poi'): ?>
                        <!-- POI Selection -->
                        <h6 class="mb-3"><i class="bi bi-shop me-2"></i>Chọn quán ăn</h6>
                        <div class="mb-3" style="max-height: 400px; overflow-y: auto;">
                            <?php foreach ($pois as $poi): ?>
                            <a href="?type=poi&poi_id=<?php echo $poi['id']; ?>" class="text-decoration-none text-dark">
                                <div class="poi-item mb-2 <?php echo $selectedPoiId == $poi['id'] ? 'selected' : ''; ?>">
                                    <div class="d-flex align-items-center">
                                        <div class="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center me-3" style="width: 45px; height: 45px; flex-shrink: 0;">
                                            <i class="bi bi-geo-alt-fill"></i>
                                        </div>
                                        <div class="flex-grow-1">
                                            <div class="fw-semibold"><?php echo htmlspecialchars($poi['nameVi']); ?></div>
                                            <small class="text-muted"><?php echo htmlspecialchars($poi['address'] ?? 'Không có địa chỉ'); ?></small>
                                        </div>
                                        <?php if ($selectedPoiId == $poi['id']): ?>
                                        <i class="bi bi-check-circle-fill text-primary"></i>
                                        <?php endif; ?>
                                    </div>
                                </div>
                            </a>
                            <?php endforeach; ?>
                        </div>
                        <?php else: ?>
                        <!-- App Download Info -->
                        <div class="alert alert-info">
                            <i class="bi bi-info-circle me-2"></i>
                            QR này sẽ dẫn đến trang tải app. Người dùng quét → Vào trang download → Tải APK.
                        </div>
                        <div class="qr-info">
                            <small class="text-muted d-block mb-1">URL:</small>
                            <code class="bg-light p-2 rounded d-block" style="word-break: break-all; font-size: 11px;">
                                <?php echo htmlspecialchars($appDownloadUrl); ?>
                            </code>
                        </div>
                        <?php endif; ?>
                    </div>
                </div>
            </div>

            <!-- Right Column - QR Preview -->
            <div class="col-lg-7">
                <div class="card">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5 class="mb-0"><i class="bi bi-eye me-2"></i>Xem trước QR Code</h5>
                        <?php if ($qrImageUrl): ?>
                        <div class="btn-group">
                            <a href="<?php echo $qrImageUrl; ?>" download="qr-code.png" class="btn btn-outline-primary btn-sm">
                                <i class="bi bi-download me-1"></i>Tải PNG
                            </a>
                            <button onclick="window.print()" class="btn btn-outline-secondary btn-sm">
                                <i class="bi bi-printer me-1"></i>In
                            </button>
                        </div>
                        <?php endif; ?>
                    </div>
                    <div class="card-body p-4">
                        <?php if ($qrImageUrl): ?>
                        <div class="qr-preview">
                            <div class="qr-code-container">
                                <img src="<?php echo $qrImageUrl; ?>" alt="QR Code">
                            </div>
                            <h5 class="mb-2"><?php echo $qrTitle; ?></h5>
                            <p class="text-muted mb-0">
                                <i class="bi bi-phone me-1"></i>
                                Quét bằng camera điện thoại để <?php echo $qrType === 'app' ? 'tải app' : 'mở POI này'; ?>
                            </p>

                            <!-- QR Content Info -->
                            <div class="qr-info mt-3">
                                <small class="text-muted d-block mb-1">Nội dung QR:</small>
                                <code class="d-block" style="word-break: break-all; font-size: 12px;">
                                    <?php echo htmlspecialchars($qrContent); ?>
                                </code>
                            </div>
                        </div>

                        <!-- Print Section (hidden until print) -->
                        <div class="print-qr mt-4 text-center">
                            <h3><?php echo $qrTitle; ?></h3>
                            <div class="qr-code-container" style="transform: scale(1.5);">
                                <img src="<?php echo $qrImageUrl; ?>" alt="QR Code">
                            </div>
                            <p style="margin-top: 30px; font-size: 14pt;">
                                Quét mã QR để <?php echo $qrType === 'app' ? 'tải FoodStreetGuide App' : 'xem thông tin ' . $qrTitle; ?>
                            </p>
                            <p style="margin-top: 50px; font-size: 10pt; color: #666;">
                                © 2025 FoodStreetGuide - Nguyễn Gia Huy | Vũ Gia Huy
                            </p>
                        </div>

                        <!-- Tips -->
                        <div class="download-section">
                            <h6 class="mb-3"><i class="bi bi-lightbulb me-2 text-warning"></i>Gợi ý sử dụng</h6>
                            <div class="row g-3">
                                <div class="col-md-6">
                                    <div class="d-flex align-items-start">
                                        <i class="bi bi-1-circle-fill text-primary me-2 mt-1"></i>
                                        <div>
                                            <strong>Tải về</strong>
                                            <p class="small text-muted mb-0">Nhấn "Tải PNG" để lưu ảnh QR</p>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="d-flex align-items-start">
                                        <i class="bi bi-2-circle-fill text-primary me-2 mt-1"></i>
                                        <div>
                                            <strong>In ấn</strong>
                                            <p class="small text-muted mb-0">Nhấn "In" để in QR dán tại quán</p>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="d-flex align-items-start">
                                        <i class="bi bi-3-circle-fill text-primary me-2 mt-1"></i>
                                        <div>
                                            <strong>Kích thước</strong>
                                            <p class="small text-muted mb-0">Khuyến nghị in 10x10cm để dễ quét</p>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="d-flex align-items-start">
                                        <i class="bi bi-4-circle-fill text-primary me-2 mt-1"></i>
                                        <div>
                                            <strong>Vị trí</strong>
                                            <p class="small text-muted mb-0">Dán ở cửa ra vào hoặc bàn ăn</p>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                        <?php else: ?>
                        <div class="text-center py-5">
                            <i class="bi bi-qr-code" style="font-size: 5rem; color: #ddd;"></i>
                            <p class="text-muted mt-3">Vui lòng chọn loại QR và POI (nếu cần) để tạo mã</p>
                        </div>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
