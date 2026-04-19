<?php
/**
 * 🧪 Analytics Test Simulator
 * Dùng để test và demo thống kê cho thầy
 */

require_once 'config.php';

// Generate random device ID
function generateDeviceId() {
    return 'dev_' . substr(md5(uniqid()), 0, 12);
}

// Simulate app visits
function simulateAppVisits($count = 10) {
    global $ANALYTICS_FILE;
    
    $analytics = load_json($ANALYTICS_FILE);
    $today = date('Y-m-d');
    $usedDevices = [];
    
    // Generate unique devices
    for ($i = 0; $i < $count; $i++) {
        $usedDevices[] = generateDeviceId();
    }
    
    // Simulate each device visiting multiple times
    foreach ($usedDevices as $deviceId) {
        // Each device opens app 1-3 times
        $visits = rand(1, 3);
        for ($j = 0; $j < $visits; $j++) {
            $analytics[] = [
                'id' => uniqid(),
                'type' => 'app_visit',
                'deviceId' => $deviceId,
                'date' => $today,
                'timestamp' => date('Y-m-d H:i:s', strtotime("-$j hours")),
                'sessionId' => 'sess_' . substr(md5(uniqid()), 0, 8)
            ];
        }
    }
    
    save_json($ANALYTICS_FILE, $analytics);
    return ['uniqueUsers' => $count, 'totalVisits' => count($analytics)];
}

// Simulate POI views
function simulatePOIViews($poiId, $deviceCount = 5) {
    global $ANALYTICS_FILE, $POIS_FILE;
    
    $analytics = load_json($ANALYTICS_FILE);
    $pois = load_json($POIS_FILE);
    $today = date('Y-m-d');
    
    for ($i = 0; $i < $deviceCount; $i++) {
        $deviceId = generateDeviceId();
        // Each device views POI 1-2 times
        $views = rand(1, 2);
        for ($j = 0; $j < $views; $j++) {
            $analytics[] = [
                'id' => uniqid(),
                'type' => 'poi_view',
                'poiId' => $poiId,
                'deviceId' => $deviceId,
                'date' => $today,
                'timestamp' => date('Y-m-d H:i:s', strtotime("-$j hours")),
                'sessionId' => 'sess_' . substr(md5(uniqid()), 0, 8)
            ];
        }
    }
    
    // Update POI visit count
    foreach ($pois as &$poi) {
        if ($poi['id'] == $poiId) {
            $poi['visitCount'] = ($poi['visitCount'] ?? 0) + $deviceCount;
        }
    }
    
    save_json($ANALYTICS_FILE, $analytics);
    save_json($POIS_FILE, $pois);
    
    return ['poiId' => $poiId, 'views' => $deviceCount];
}

// Simulate QR check-ins
function simulateCheckIns($poiId, $count = 3) {
    global $ANALYTICS_FILE, $POIS_FILE;
    
    $analytics = load_json($ANALYTICS_FILE);
    $pois = load_json($POIS_FILE);
    $today = date('Y-m-d');
    
    for ($i = 0; $i < $count; $i++) {
        $analytics[] = [
            'id' => uniqid(),
            'type' => 'check_in',
            'poiId' => $poiId,
            'deviceId' => generateDeviceId(),
            'date' => $today,
            'timestamp' => date('Y-m-d H:i:s'),
            'sessionId' => 'sess_' . substr(md5(uniqid()), 0, 8),
            'qrToken' => 'qr_' . substr(md5(uniqid()), 0, 8)
        ];
    }
    
    // Update POI check-in count
    foreach ($pois as &$poi) {
        if ($poi['id'] == $poiId) {
            $poi['checkInCount'] = ($poi['checkInCount'] ?? 0) + $count;
        }
    }
    
    save_json($ANALYTICS_FILE, $analytics);
    save_json($POIS_FILE, $pois);
    
    return ['poiId' => $poiId, 'checkins' => $count];
}

// Clear all analytics
function clearAnalytics() {
    global $ANALYTICS_FILE;
    save_json($ANALYTICS_FILE, []);
    return true;
}

// Handle AJAX requests
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    header('Content-Type: application/json');
    
    $action = $_POST['action'] ?? '';
    
    switch ($action) {
        case 'app_visits':
            $count = intval($_POST['count'] ?? 10);
            $result = simulateAppVisits($count);
            echo json_encode(['success' => true, 'data' => $result]);
            break;
            
        case 'poi_views':
            $poiId = intval($_POST['poiId'] ?? 1);
            $count = intval($_POST['count'] ?? 5);
            $result = simulatePOIViews($poiId, $count);
            echo json_encode(['success' => true, 'data' => $result]);
            break;
            
        case 'check_ins':
            $poiId = intval($_POST['poiId'] ?? 1);
            $count = intval($_POST['count'] ?? 3);
            $result = simulateCheckIns($poiId, $count);
            echo json_encode(['success' => true, 'data' => $result]);
            break;
            
        case 'clear':
            clearAnalytics();
            echo json_encode(['success' => true, 'message' => 'Đã xóa toàn bộ dữ liệu analytics']);
            break;
            
        default:
            echo json_encode(['success' => false, 'error' => 'Unknown action']);
    }
    exit;
}

// Get current stats for display
$analytics = load_json($ANALYTICS_FILE);
$pois = load_json($POIS_FILE);
$today = date('Y-m-d');

$todayAppVisits = array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'app_visit');
$uniqueUsers = count(array_unique(array_column($todayAppVisits, 'deviceId')));
$totalAppVisits = count($todayAppVisits);
$todayPOIViews = count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'poi_view'));
$todayCheckins = count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'check_in'));
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>🧪 Analytics Test Simulator - Food Tour</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
    <style>
        body { background: #f8f9fa; }
        .simulator-card {
            background: white;
            border-radius: 16px;
            padding: 24px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.08);
            margin-bottom: 20px;
        }
        .stat-box {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border-radius: 12px;
            padding: 20px;
            text-align: center;
        }
        .stat-box.green {
            background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%);
        }
        .stat-box.orange {
            background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);
        }
        .stat-number {
            font-size: 2.5rem;
            font-weight: bold;
            margin-bottom: 8px;
        }
        .btn-simulate {
            width: 100%;
            padding: 15px;
            font-size: 1.1rem;
            border-radius: 10px;
            margin-bottom: 10px;
        }
        .result-toast {
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 1000;
        }
    </style>
</head>
<body>
    <div class="container py-4">
        <h1 class="mb-4">🧪 Analytics Test Simulator</h1>
        <p class="text-muted mb-4">Dùng công cụ này để tạo dữ liệu test cho thầy xem. DAU sẽ tính đúng = unique users!</p>
        
        <!-- Current Stats -->
        <div class="row g-3 mb-4">
            <div class="col-md-3">
                <div class="stat-box">
                    <div class="stat-number"><?= $uniqueUsers ?></div>
                    <div><i class="bi bi-people"></i> DAU (Unique Users)</div>
                    <small class="opacity-75">Total visits: <?= $totalAppVisits ?></small>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-box green">
                    <div class="stat-number"><?= $todayPOIViews ?></div>
                    <div><i class="bi bi-eye"></i> POI Views</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-box orange">
                    <div class="stat-number"><?= $todayCheckins ?></div>
                    <div><i class="bi bi-qr-code-scan"></i> Check-ins</div>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-box" style="background: linear-gradient(135deg, #fa709a 0%, #fee140 100%);">
                    <div class="stat-number"><?= count($analytics) ?></div>
                    <div><i class="bi bi-database"></i> Total Records</div>
                </div>
            </div>
        </div>
        
        <div class="row">
            <!-- Simulator Controls -->
            <div class="col-md-8">
                <div class="simulator-card">
                    <h4 class="mb-3">🎮 Simulate User Activity</h4>
                    
                    <!-- App Visits -->
                    <div class="mb-4">
                        <label class="form-label"><strong>1. Simulate App Visits</strong></label>
                        <div class="row g-2">
                            <div class="col-md-6">
                                <input type="number" id="appVisitsCount" class="form-control" value="10" min="1" max="100">
                                <small class="text-muted">Số user unique (mỗi user mở app 1-3 lần)</small>
                            </div>
                            <div class="col-md-6">
                                <button class="btn btn-primary btn-simulate" onclick="simulate('app_visits')">
                                    <i class="bi bi-phone"></i> Simulate App Visits
                                </button>
                            </div>
                        </div>
                    </div>
                    
                    <!-- POI Views -->
                    <div class="mb-4">
                        <label class="form-label"><strong>2. Simulate POI Views</strong></label>
                        <div class="row g-2">
                            <div class="col-md-4">
                                <select id="poiSelect" class="form-select">
                                    <?php foreach ($pois as $poi): ?>
                                        <option value="<?= $poi['id'] ?>"><?= $poi['nameVI'] ?? $poi['nameVi'] ?? 'POI #' . $poi['id'] ?></option>
                                    <?php endforeach; ?>
                                </select>
                            </div>
                            <div class="col-md-4">
                                <input type="number" id="poiViewsCount" class="form-control" value="5" min="1" max="50">
                                <small class="text-muted">Số user xem</small>
                            </div>
                            <div class="col-md-4">
                                <button class="btn btn-success btn-simulate" onclick="simulate('poi_views')">
                                    <i class="bi bi-eye"></i> Simulate Views
                                </button>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Check-ins -->
                    <div class="mb-4">
                        <label class="form-label"><strong>3. Simulate QR Check-ins</strong></label>
                        <div class="row g-2">
                            <div class="col-md-4">
                                <select id="checkinPoiSelect" class="form-select">
                                    <?php foreach ($pois as $poi): ?>
                                        <option value="<?= $poi['id'] ?>"><?= $poi['nameVI'] ?? $poi['nameVi'] ?? 'POI #' . $poi['id'] ?></option>
                                    <?php endforeach; ?>
                                </select>
                            </div>
                            <div class="col-md-4">
                                <input type="number" id="checkinCount" class="form-control" value="3" min="1" max="20">
                                <small class="text-muted">Số check-in</small>
                            </div>
                            <div class="col-md-4">
                                <button class="btn btn-warning btn-simulate" onclick="simulate('check_ins')">
                                    <i class="bi bi-qr-code-scan"></i> Simulate Check-ins
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- Danger Zone -->
                <div class="simulator-card border-danger">
                    <h5 class="text-danger"><i class="bi bi-exclamation-triangle"></i> Danger Zone</h5>
                    <button class="btn btn-outline-danger" onclick="clearAll()">
                        <i class="bi bi-trash"></i> Xóa toàn bộ analytics
                    </button>
                </div>
            </div>
            
            <!-- Quick Links -->
            <div class="col-md-4">
                <div class="simulator-card">
                    <h5><i class="bi bi-link-45deg"></i> Quick Links</h5>
                    <div class="d-grid gap-2">
                        <a href="statistics.php" class="btn btn-outline-primary" target="_blank">
                            <i class="bi bi-graph-up"></i> Xem Statistics
                        </a>
                        <a href="index.php" class="btn btn-outline-secondary" target="_blank">
                            <i class="bi bi-speedometer2"></i> Dashboard
                        </a>
                        <a href="pois.php" class="btn btn-outline-info" target="_blank">
                            <i class="bi bi-shop"></i> Quản lý POI
                        </a>
                    </div>
                </div>
                
                <div class="simulator-card">
                    <h5><i class="bi bi-info-circle"></i> How DAU Works</h5>
                    <ul class="small text-muted">
                        <li><strong>DAU</strong> = số <strong>deviceId unique</strong> mở app</li>
                        <li>1 user mở app 5 lần = DAU = 1 ✓</li>
                        <li>5 user mỗi người 1 lần = DAU = 5 ✓</li>
                        <li>Cập nhật <strong>real-time</strong></li>
                    </ul>
                </div>
            </div>
        </div>
    </div>
    
    <!-- Toast Container -->
    <div class="result-toast" id="toastContainer"></div>
    
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        function showToast(message, type = 'success') {
            const toast = document.createElement('div');
            toast.className = `alert alert-${type} alert-dismissible fade show`;
            toast.innerHTML = `
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;
            document.getElementById('toastContainer').appendChild(toast);
            setTimeout(() => toast.remove(), 5000);
        }
        
        async function simulate(action) {
            const formData = new FormData();
            formData.append('action', action);
            
            if (action === 'app_visits') {
                formData.append('count', document.getElementById('appVisitsCount').value);
            } else if (action === 'poi_views') {
                formData.append('poiId', document.getElementById('poiSelect').value);
                formData.append('count', document.getElementById('poiViewsCount').value);
            } else if (action === 'check_ins') {
                formData.append('poiId', document.getElementById('checkinPoiSelect').value);
                formData.append('count', document.getElementById('checkinCount').value);
            }
            
            try {
                const response = await fetch('test-simulator.php', {
                    method: 'POST',
                    body: formData
                });
                
                const result = await response.json();
                
                if (result.success) {
                    showToast('✅ ' + JSON.stringify(result.data), 'success');
                    setTimeout(() => location.reload(), 1000);
                } else {
                    showToast('❌ ' + result.error, 'danger');
                }
            } catch (e) {
                showToast('❌ Lỗi: ' + e.message, 'danger');
            }
        }
        
        async function clearAll() {
            if (!confirm('Bạn có chắc muốn xóa toàn bộ dữ liệu analytics?')) return;
            
            const formData = new FormData();
            formData.append('action', 'clear');
            
            try {
                const response = await fetch('test-simulator.php', {
                    method: 'POST',
                    body: formData
                });
                
                const result = await response.json();
                
                if (result.success) {
                    showToast('🗑️ ' + result.message, 'warning');
                    setTimeout(() => location.reload(), 1000);
                }
            } catch (e) {
                showToast('❌ Lỗi: ' + e.message, 'danger');
            }
        }
    </script>
</body>
</html>
