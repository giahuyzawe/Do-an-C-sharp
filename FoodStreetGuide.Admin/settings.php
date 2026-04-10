<?php
session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

$settings_file = 'settings.json';
$settings = [];
if (file_exists($settings_file)) {
    $settings = json_decode(file_get_contents($settings_file), true) ?: [];
}

$defaults = [
    'geofence_radius' => 100,
    'cooldown' => 300,
    'gps_interval' => 5000,
    'narration_language' => 'vi',
    'narration_enabled' => true
];

$settings = array_merge($defaults, $settings);

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $settings = [
        'geofence_radius' => intval($_POST['geofence_radius'] ?? 100),
        'cooldown' => intval($_POST['cooldown'] ?? 300),
        'gps_interval' => intval($_POST['gps_interval'] ?? 5000),
        'narration_language' => $_POST['narration_language'] ?? 'vi',
        'narration_enabled' => isset($_POST['narration_enabled'])
    ];
    file_put_contents($settings_file, json_encode($settings, JSON_PRETTY_PRINT));
    $saved = true;
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Cài đặt - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --primary: #4F46E5;
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
        .form-label {
            font-weight: 500;
            color: #374151;
        }
        .input-group-text {
            background: white;
        }
    </style>
</head>
<body>
    <div class="sidebar">
        <div class="sidebar-brand">
            <i class="bi bi-geo-alt-fill"></i>
            FoodStreetGuide
        </div>
        <nav class="nav flex-column">
            <a class="nav-link" href="index.php"><i class="bi bi-grid"></i> Dashboard</a>
            <a class="nav-link" href="pois.php"><i class="bi bi-geo"></i> Quản lý POI</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link active" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <h2 class="mb-4">Cài đặt hệ thống</h2>

        <?php if (isset($saved)): ?>
        <div class="alert alert-success">
            <i class="bi bi-check-circle me-2"></i>Đã lưu cài đặt thành công!
        </div>
        <?php endif; ?>

        <form method="POST">
            <div class="row g-4">
                <div class="col-lg-6">
                    <div class="card">
                        <div class="card-header">
                            <h5 class="mb-0"><i class="bi bi-geo me-2"></i>Geofence</h5>
                        </div>
                        <div class="card-body">
                            <div class="mb-3">
                                <label class="form-label">Bán kính mặc định (mét)</label>
                                <div class="input-group">
                                    <input type="number" class="form-control" name="geofence_radius" 
                                           value="<?php echo $settings['geofence_radius']; ?>" min="10" max="1000">
                                    <span class="input-group-text">m</span>
                                </div>
                                <small class="text-muted">Bán kính kích hoạt geofence</small>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Thời gian cooldown (giây)</label>
                                <div class="input-group">
                                    <input type="number" class="form-control" name="cooldown" 
                                           value="<?php echo $settings['cooldown']; ?>" min="0" max="3600">
                                    <span class="input-group-text">giây</span>
                                </div>
                                <small class="text-muted">Thời gian chờ giữa các lần kích hoạt</small>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-lg-6">
                    <div class="card">
                        <div class="card-header">
                            <h5 class="mb-0"><i class="bi bi-broadcast me-2"></i>GPS & Narration</h5>
                        </div>
                        <div class="card-body">
                            <div class="mb-3">
                                <label class="form-label">GPS Update Interval (ms)</label>
                                <div class="input-group">
                                    <input type="number" class="form-control" name="gps_interval" 
                                           value="<?php echo $settings['gps_interval']; ?>" min="1000" max="60000" step="1000">
                                    <span class="input-group-text">ms</span>
                                </div>
                                <small class="text-muted">Khoảng thời gian cập nhật vị trí</small>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Ngôn ngữ narration mặc định</label>
                                <select class="form-select" name="narration_language">
                                    <option value="vi" <?php echo $settings['narration_language'] == 'vi' ? 'selected' : ''; ?>>Tiếng Việt</option>
                                    <option value="en" <?php echo $settings['narration_language'] == 'en' ? 'selected' : ''; ?>>English</option>
                                </select>
                            </div>
                            <div class="mb-3">
                                <div class="form-check form-switch">
                                    <input class="form-check-input" type="checkbox" id="narration_enabled" name="narration_enabled" 
                                           <?php echo $settings['narration_enabled'] ? 'checked' : ''; ?>>
                                    <label class="form-check-label" for="narration_enabled">Bật narration tự động</label>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="mt-4 d-flex gap-2">
                <button type="submit" class="btn btn-primary">
                    <i class="bi bi-check-lg me-2"></i>Lưu cài đặt
                </button>
                <a href="settings.php?reset=1" class="btn btn-outline-secondary" onclick="return confirm('Reset về mặc định?')">
                    <i class="bi bi-arrow-counterclockwise me-2"></i>Reset
                </a>
            </div>
        </form>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
