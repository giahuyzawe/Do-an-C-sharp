<?php
/**
 * Trang tạo QR Code ĐỘNG (Dynamic QR)
 * Mỗi lần tạo là 1 mã QR khác nhau (unique token)
 * Có thể set thời hạn và giới hạn số lần quét
 */

session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

// Load POIs
$pois_file = 'pois.json';
$pois = [];
if (file_exists($pois_file)) {
    $pois = json_decode(file_get_contents($pois_file), true) ?: [];
}

// Only approved restaurants
$approved_pois = array_filter($pois, fn($p) => ($p['approvalStatus'] ?? 'pending') === 'approved');

// Load QR codes
$qr_file = 'qrcodes.json';
$qr_codes = [];
if (file_exists($qr_file)) {
    $qr_codes = json_decode(file_get_contents($qr_file), true) ?: [];
}

// Handle QR generation
$message = '';
$generated_qr = null;

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['generate'])) {
    $poi_id = $_POST['poi_id'] ?? null;
    $qr_type = $_POST['qr_type'] ?? 'time_limited'; // single_use, time_limited, unlimited
    $expiry_hours = intval($_POST['expiry_hours'] ?? 24);
    $max_scans = $_POST['max_scans'] ?? null;
    $notes = $_POST['notes'] ?? '';
    
    if ($poi_id) {
        // Find POI
        $poi = null;
        foreach ($approved_pois as $p) {
            if ($p['id'] == $poi_id) {
                $poi = $p;
                break;
            }
        }
        
        if ($poi) {
            // Generate unique token
            $unique_token = 'vk-' . date('Ymd') . '-' . substr(md5(uniqid() . $poi_id), 0, 12);
            
            // Calculate expiry
            $expires_at = null;
            if ($qr_type === 'time_limited' && $expiry_hours > 0) {
                $expires_at = date('Y-m-d H:i:s', strtotime("+$expiry_hours hours"));
            }
            
            // Create QR record
            $qr_record = [
                'id' => uniqid(),
                'unique_token' => $unique_token,
                'poi_id' => $poi_id,
                'poi_name' => $poi['nameVi'],
                'qr_type' => $qr_type,
                'created_at' => date('Y-m-d H:i:s'),
                'expires_at' => $expires_at,
                'max_scans' => $max_scans ? intval($max_scans) : null,
                'scan_count' => 0,
                'is_used' => false,
                'created_by' => $_SESSION['admin']['username'] ?? 'admin',
                'notes' => $notes
            ];
            
            // Save to file
            $qr_codes[] = $qr_record;
            file_put_contents($qr_file, json_encode($qr_codes, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
            
            $generated_qr = $qr_record;
            $message = 'success';
            
            // Log activity
            $activity = [
                'id' => uniqid(),
                'action' => 'generate_qr',
                'target' => $poi_id,
                'details' => "Tạo QR động cho {$poi['nameVi']} (Token: {$unique_token})",
                'timestamp' => date('Y-m-d H:i:s'),
                'user' => $_SESSION['admin']['username'] ?? 'admin'
            ];
            $activities_file = 'activities.json';
            $activities = file_exists($activities_file) ? json_decode(file_get_contents($activities_file), true) ?: [] : [];
            array_unshift($activities, $activity);
            file_put_contents($activities_file, json_encode(array_slice($activities, 0, 100), JSON_PRETTY_PRINT));
        }
    }
}

// Handle QR deletion
if (isset($_GET['delete'])) {
    $qr_id = $_GET['delete'];
    $qr_codes = array_filter($qr_codes, fn($q) => $q['id'] !== $qr_id);
    file_put_contents($qr_file, json_encode(array_values($qr_codes), JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
    header('Location: qr-generator.php');
    exit;
}

// Get selected POI for display
$selected_poi_id = $_GET['poi_id'] ?? ($_POST['poi_id'] ?? null);
$selected_poi = null;
if ($selected_poi_id) {
    foreach ($approved_pois as $p) {
        if ($p['id'] == $selected_poi_id) {
            $selected_poi = $p;
            break;
        }
    }
}

// Stats
$total_qr = count($qr_codes);
$valid_qr = count(array_filter($qr_codes, fn($q) => {
    if ($q['is_used'] && $q['qr_type'] === 'single_use') return false;
    if ($q['expires_at'] && strtotime($q['expires_at']) < time()) return false;
    if ($q['max_scans'] && $q['scan_count'] >= $q['max_scans']) return false;
    return true;
}));

?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Tạo QR Code Động - Food Street Guide Admin</title>
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
        .qr-card {
            background: white;
            border-radius: 1rem;
            padding: 2rem;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            text-align: center;
        }
        .qr-container {
            background: white;
            padding: 20px;
            border-radius: 16px;
            display: inline-block;
            box-shadow: 0 4px 20px rgba(0,0,0,0.1);
            margin: 20px 0;
        }
        .qr-container img {
            width: 250px;
            height: 250px;
        }
        .token-badge {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 8px 16px;
            border-radius: 50px;
            font-family: 'Courier New', monospace;
            font-size: 0.9rem;
            display: inline-block;
            margin: 10px 0;
        }
        .poi-item {
            cursor: pointer;
            padding: 15px;
            border-radius: 12px;
            transition: all 0.2s;
            border: 2px solid transparent;
            margin-bottom: 10px;
            background: white;
        }
        .poi-item:hover {
            border-color: var(--primary);
            transform: translateX(5px);
        }
        .poi-item.selected {
            border-color: var(--primary);
            background: rgba(255, 107, 53, 0.05);
        }
        .qr-type-card {
            cursor: pointer;
            padding: 20px;
            border-radius: 12px;
            border: 2px solid #E2E8F0;
            transition: all 0.3s;
            text-align: center;
        }
        .qr-type-card:hover {
            border-color: var(--primary);
            transform: translateY(-2px);
        }
        .qr-type-card.selected {
            border-color: var(--primary);
            background: rgba(255, 107, 53, 0.05);
        }
        .qr-type-card i {
            font-size: 2rem;
            margin-bottom: 10px;
            color: var(--primary);
        }
        .qr-list-item {
            background: white;
            border-radius: 12px;
            padding: 15px;
            margin-bottom: 10px;
            border-left: 4px solid var(--primary);
        }
        .qr-list-item.valid { border-left-color: #10B981; }
        .qr-list-item.expired { border-left-color: #EF4444; opacity: 0.7; }
        .qr-list-item.used { border-left-color: #F59E0B; }
        .stats-card {
            background: white;
            border-radius: 1rem;
            padding: 1.5rem;
            text-align: center;
        }
        .info-box {
            background: #F0F9FF;
            border: 1px solid #BAE6FD;
            border-radius: 12px;
            padding: 15px;
            margin: 15px 0;
        }
    </style>
</head>
<body>
    <!-- Sidebar -->
    <div class="sidebar">
        <div class="sidebar-brand">
            <i class="bi bi-qr-code-scan"></i>
            QR Động
        </div>
        <nav class="nav flex-column">
            <a class="nav-link" href="index.php"><i class="bi bi-grid"></i> Dashboard</a>
            <a class="nav-link" href="pois.php"><i class="bi bi-geo"></i> Quản lý POI</a>
            <a class="nav-link" href="restaurant-approval.php"><i class="bi bi-check-circle"></i> Duyệt Nhà Hàng</a>
            <a class="nav-link" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
            <a class="nav-link active" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <!-- Main Content -->
    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div>
                <h2 class="mb-1"><i class="bi bi-qr-code me-2 text-primary"></i>Tạo QR Code Động</h2>
                <p class="text-muted mb-0">Mỗi lần tạo là 1 mã QR <strong>duy nhất</strong> (không bao giờ trùng)</p>
            </div>
        </div>

        <!-- Stats -->
        <div class="row g-4 mb-4">
            <div class="col-md-4">
                <div class="stats-card">
                    <div class="text-muted small">Tổng QR đã tạo</div>
                    <div class="h2 mb-0"><?php echo $total_qr; ?></div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="stats-card">
                    <div class="text-muted small">QR còn hiệu lực</div>
                    <div class="h2 mb-0 text-success"><?php echo $valid_qr; ?></div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="stats-card">
                    <div class="text-muted small">QR hết hạn/đã dùng</div>
                    <div class="h2 mb-0 text-danger"><?php echo $total_qr - $valid_qr; ?></div>
                </div>
            </div>
        </div>

        <?php if ($message === 'success' && $generated_qr): ?>
        <!-- Generated QR Display -->
        <div class="alert alert-success">
            <i class="bi bi-check-circle me-2"></i>Đã tạo QR Code thành công!
        </div>
        
        <div class="qr-card mb-4">
            <h4 class="mb-3"><?php echo htmlspecialchars($generated_qr['poi_name']); ?></h4>
            
            <div class="token-badge">
                <i class="bi bi-upc-scan me-2"></i><?php echo $generated_qr['unique_token']; ?>
            </div>
            
            <div class="qr-container">
                <?php
                $qrContent = "foodstreetguide://qr/" . $generated_qr['unique_token'];
                $qrImageUrl = 'https://api.qrserver.com/v1/create-qr-code/?size=300x300&margin=10&data=' . urlencode($qrContent);
                ?>
                <img src="<?php echo $qrImageUrl; ?>" alt="QR Code">
            </div>
            
            <div class="info-box">
                <p class="mb-1"><strong>Loại QR:</strong> 
                    <?php 
                    echo match($generated_qr['qr_type']) {
                        'single_use' => '🎫 Chỉ dùng 1 lần',
                        'time_limited' => '⏰ Có thời hạn',
                        'unlimited' => '♾️ Không giới hạn',
                        default => $generated_qr['qr_type']
                    };
                    ?>
                </p>
                <?php if ($generated_qr['expires_at']): ?>
                <p class="mb-1"><strong>Hết hạn:</strong> <?php echo date('d/m/Y H:i', strtotime($generated_qr['expires_at'])); ?></p>
                <?php endif; ?>
                <?php if ($generated_qr['max_scans']): ?>
                <p class="mb-1"><strong>Giới hạn quét:</strong> <?php echo $generated_qr['max_scans']; ?> lần</p>
                <?php endif; ?>
                <p class="mb-0"><strong>Nội dung QR:</strong> <code><?php echo $qrContent; ?></code></p>
            </div>
            
            <div class="d-flex gap-2 justify-content-center">
                <a href="<?php echo $qrImageUrl; ?>" download="qr-<?php echo $generated_qr['unique_token']; ?>.png" class="btn btn-primary">
                    <i class="bi bi-download me-1"></i>Tải QR
                </a>
                <button onclick="window.print()" class="btn btn-outline-secondary">
                    <i class="bi bi-printer me-1"></i>In
                </button>
                <a href="qr-generator.php" class="btn btn-success">
                    <i class="bi bi-plus-lg me-1"></i>Tạo QR khác
                </a>
            </div>
        </div>
        <?php else: ?>
        
        <!-- QR Generation Form -->
        <div class="row">
            <!-- Left: Select Restaurant -->
            <div class="col-lg-4">
                <div class="card">
                    <div class="card-header">
                        <h5 class="mb-0"><i class="bi bi-shop me-2"></i>1. Chọn nhà hàng</h5>
                    </div>
                    <div class="card-body" style="max-height: 500px; overflow-y: auto;">
                        <?php if (empty($approved_pois)): ?>
                        <div class="alert alert-warning">
                            <i class="bi bi-exclamation-triangle me-2"></i>Chưa có nhà hàng nào được duyệt.
                            <a href="restaurant-approval.php" class="alert-link">Đi duyệt</a>
                        </div>
                        <?php else: ?>
                        <form method="GET" id="poiForm">
                            <?php foreach ($approved_pois as $poi): ?>
                            <div class="poi-item <?php echo ($selected_poi_id == $poi['id']) ? 'selected' : ''; ?>" 
                                 onclick="selectPOI(<?php echo $poi['id']; ?>)">
                                <div class="d-flex align-items-center">
                                    <div class="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center me-3" style="width: 45px; height: 45px; flex-shrink: 0;">
                                        <i class="bi bi-geo-alt-fill"></i>
                                    </div>
                                    <div class="flex-grow-1">
                                        <div class="fw-semibold"><?php echo htmlspecialchars($poi['nameVi']); ?></div>
                                        <small class="text-muted"><?php echo htmlspecialchars($poi['address'] ?? 'Không có địa chỉ'); ?></small>
                                    </div>
                                    <?php if ($selected_poi_id == $poi['id']): ?>
                                    <i class="bi bi-check-circle-fill text-primary"></i>
                                    <?php endif; ?>
                                </div>
                            </div>
                            <?php endforeach; ?>
                            <input type="hidden" name="poi_id" id="selectedPoiId" value="<?php echo $selected_poi_id ?? ''; ?>">
                        </form>
                        <?php endif; ?>
                    </div>
                </div>
            </div>

            <!-- Right: QR Configuration -->
            <div class="col-lg-8">
                <?php if ($selected_poi): ?>
                <div class="card">
                    <div class="card-header">
                        <h5 class="mb-0"><i class="bi bi-gear me-2"></i>2. Cấu hình QR cho: <?php echo htmlspecialchars($selected_poi['nameVi']); ?></h5>
                    </div>
                    <div class="card-body">
                        <form method="POST">
                            <input type="hidden" name="poi_id" value="<?php echo $selected_poi['id']; ?>">
                            
                            <!-- QR Type Selection -->
                            <div class="mb-4">
                                <label class="form-label fw-semibold">Loại QR Code:</label>
                                <div class="row g-3">
                                    <div class="col-md-4">
                                        <div class="qr-type-card selected" onclick="selectQRType('time_limited')" id="card-time_limited">
                                            <i class="bi bi-clock-history"></i>
                                            <div class="fw-bold">Có thời hạn</div>
                                            <small class="text-muted">Hết hạn sau X giờ</small>
                                        </div>
                                    </div>
                                    <div class="col-md-4">
                                        <div class="qr-type-card" onclick="selectQRType('single_use')" id="card-single_use">
                                            <i class="bi bi-ticket-perforated"></i>
                                            <div class="fw-bold">Chỉ 1 lần</div>
                                            <small class="text-muted">Quét xong là vô hiệu</small>
                                        </div>
                                    </div>
                                    <div class="col-md-4">
                                        <div class="qr-type-card" onclick="selectQRType('unlimited')" id="card-unlimited">
                                            <i class="bi bi-infinity"></i>
                                            <div class="fw-bold">Không giới hạn</div>
                                            <small class="text-muted">Dùng mãi mãi</small>
                                        </div>
                                    </div>
                                </div>
                                <input type="hidden" name="qr_type" id="qrTypeInput" value="time_limited">
                            </div>

                            <!-- Time Limited Options -->
                            <div id="timeOptions" class="mb-4">
                                <label class="form-label">Thời hạn (giờ):</label>
                                <select name="expiry_hours" class="form-select">
                                    <option value="1">1 giờ</option>
                                    <option value="6">6 giờ</option>
                                    <option value="12">12 giờ</option>
                                    <option value="24" selected>24 giờ</option>
                                    <option value="48">48 giờ</option>
                                    <option value="72">72 giờ (3 ngày)</option>
                                    <option value="168">168 giờ (1 tuần)</option>
                                </select>
                            </div>

                            <!-- Max Scans Option -->
                            <div id="scanOptions" class="mb-4" style="display: none;">
                                <label class="form-label">Giới hạn số lần quét:</label>
                                <input type="number" name="max_scans" class="form-control" placeholder="Để trống = không giới hạn" min="1">
                                <div class="form-text">QR sẽ vô hiệu sau khi đạt số lần quét này</div>
                            </div>

                            <!-- Notes -->
                            <div class="mb-4">
                                <label class="form-label">Ghi chú (tùy chọn):</label>
                                <input type="text" name="notes" class="form-control" placeholder="VD: QR cho sự kiện 15/08">
                            </div>

                            <!-- Info Box -->
                            <div class="alert alert-info">
                                <i class="bi bi-info-circle me-2"></i>
                                <strong>Lưu ý:</strong> Mỗi lần bấm "Tạo QR" sẽ sinh ra mã <strong>duy nhất</strong> không bao giờ trùng lặp. 
                                Mã QR sẽ có dạng: <code>foodstreetguide://qr/vk-20240815-abc123</code>
                            </div>

                            <!-- Submit -->
                            <button type="submit" name="generate" class="btn btn-primary btn-lg w-100">
                                <i class="bi bi-plus-circle me-2"></i>Tạo QR Code
                            </button>
                        </form>
                    </div>
                </div>
                <?php else: ?>
                <div class="card">
                    <div class="card-body text-center py-5">
                        <i class="bi bi-shop text-muted" style="font-size: 4rem;"></i>
                        <h5 class="mt-3">Vui lòng chọn nhà hàng</h5>
                        <p class="text-muted">Chọn một nhà hàng từ danh sách bên trái để tạo QR</p>
                    </div>
                </div>
                <?php endif; ?>
            </div>
        </div>

        <!-- Existing QR Codes -->
        <div class="card mt-4">
            <div class="card-header">
                <h5 class="mb-0"><i class="bi bi-list me-2"></i>QR Codes đã tạo</h5>
            </div>
            <div class="card-body">
                <?php if (empty($qr_codes)): ?>
                <div class="text-center py-4">
                    <i class="bi bi-inbox text-muted fs-1"></i>
                    <p class="text-muted mt-2">Chưa có QR code nào được tạo</p>
                </div>
                <?php else: ?>
                <?php 
                // Sort by created date desc
                usort($qr_codes, fn($a, $b) => strtotime($b['created_at']) - strtotime($a['created_at']));
                foreach (array_slice($qr_codes, 0, 10) as $qr): 
                    // Determine status
                    $status = 'valid';
                    $status_text = 'Còn hiệu lực';
                    $status_class = 'valid';
                    
                    if ($qr['is_used'] && $qr['qr_type'] === 'single_use') {
                        $status = 'used';
                        $status_text = 'Đã sử dụng';
                        $status_class = 'used';
                    } elseif ($qr['expires_at'] && strtotime($qr['expires_at']) < time()) {
                        $status = 'expired';
                        $status_text = 'Đã hết hạn';
                        $status_class = 'expired';
                    } elseif ($qr['max_scans'] && $qr['scan_count'] >= $qr['max_scans']) {
                        $status = 'expired';
                        $status_text = 'Hết lượt quét';
                        $status_class = 'expired';
                    }
                ?>
                <div class="qr-list-item <?php echo $status_class; ?>">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <div class="d-flex align-items-center gap-2">
                                <span class="badge bg-<?php echo $status === 'valid' ? 'success' : ($status === 'used' ? 'warning' : 'danger'); ?>">
                                    <?php echo $status_text; ?>
                                </span>
                                <code><?php echo $qr['unique_token']; ?></code>
                            </div>
                            <div class="mt-1">
                                <strong><?php echo htmlspecialchars($qr['poi_name']); ?></strong>
                                <span class="text-muted">| 
                                    <?php 
                                    echo match($qr['qr_type']) {
                                        'single_use' => '🎫 1 lần',
                                        'time_limited' => '⏰ Thời hạn',
                                        'unlimited' => '♾️ Vĩnh viễn',
                                        default => $qr['qr_type']
                                    };
                                    ?>
                                </span>
                            </div>
                            <small class="text-muted">
                                Tạo: <?php echo date('d/m/Y H:i', strtotime($qr['created_at'])); ?> | 
                                Quét: <?php echo $qr['scan_count']; ?>
                                <?php if ($qr['max_scans']): ?>/ <?php echo $qr['max_scans']; endif; ?> lần
                                <?php if ($qr['expires_at']): ?>| Hết hạn: <?php echo date('d/m H:i', strtotime($qr['expires_at'])); endif; ?>
                            </small>
                        </div>
                        <div class="d-flex gap-2">
                            <?php
                            $qrContent = "foodstreetguide://qr/" . $qr['unique_token'];
                            $qrImageUrl = 'https://api.qrserver.com/v1/create-qr-code/?size=200x200&margin=10&data=' . urlencode($qrContent);
                            ?>
                            <img src="<?php echo $qrImageUrl; ?>" alt="QR" style="width: 60px; height: 60px;">
                            <a href="?delete=<?php echo $qr['id']; ?>" class="btn btn-sm btn-outline-danger" onclick="return confirm('Xóa QR này?')">
                                <i class="bi bi-trash"></i>
                            </a>
                        </div>
                    </div>
                </div>
                <?php endforeach; ?>
                <?php if (count($qr_codes) > 10): ?>
                <div class="text-center mt-3">
                    <span class="text-muted">... và <?php echo count($qr_codes) - 10; ?> QR codes khác</span>
                </div>
                <?php endif; ?>
                <?php endif; ?>
            </div>
        </div>
        <?php endif; ?>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        function selectPOI(poiId) {
            document.getElementById('selectedPoiId').value = poiId;
            document.getElementById('poiForm').submit();
        }

        function selectQRType(type) {
            // Update hidden input
            document.getElementById('qrTypeInput').value = type;
            
            // Update visual selection
            document.querySelectorAll('.qr-type-card').forEach(card => {
                card.classList.remove('selected');
            });
            document.getElementById('card-' + type).classList.add('selected');
            
            // Show/hide options
            const timeOptions = document.getElementById('timeOptions');
            const scanOptions = document.getElementById('scanOptions');
            
            if (type === 'time_limited') {
                timeOptions.style.display = 'block';
                scanOptions.style.display = 'none';
            } else if (type === 'single_use') {
                timeOptions.style.display = 'none';
                scanOptions.style.display = 'none';
            } else {
                timeOptions.style.display = 'none';
                scanOptions.style.display = 'block';
            }
        }
    </script>
</body>
</html>
