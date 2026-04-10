<?php
session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

$storage_file = 'pois.json';
$pois = [];
if (file_exists($storage_file)) {
    $pois = json_decode(file_get_contents($storage_file), true) ?: [];
}

$id = $_GET['id'] ?? null;
$poi = null;

if ($id) {
    foreach ($pois as $p) {
        if ($p['id'] == $id) {
            $poi = $p;
            break;
        }
    }
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $new_poi = [
        'id' => $id ?: time(),
        'nameVi' => $_POST['nameVi'] ?? '',
        'nameEn' => $_POST['nameEn'] ?? '',
        'descriptionVi' => $_POST['descriptionVi'] ?? '',
        'descriptionEn' => $_POST['descriptionEn'] ?? '',
        'latitude' => floatval($_POST['latitude'] ?? 0),
        'longitude' => floatval($_POST['longitude'] ?? 0),
        'radius' => intval($_POST['radius'] ?? 100),
        'priority' => intval($_POST['priority'] ?? 1),
        'visitCount' => $poi['visitCount'] ?? 0,
        'createdAt' => $poi['createdAt'] ?? date('Y-m-d H:i:s')
    ];
    
    if ($id) {
        // Update existing
        foreach ($pois as &$p) {
            if ($p['id'] == $id) {
                $p = $new_poi;
                break;
            }
        }
    } else {
        // Add new
        $pois[] = $new_poi;
    }
    
    file_put_contents($storage_file, json_encode($pois, JSON_PRETTY_PRINT));
    header('Location: pois.php');
    exit;
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo $id ? 'Chỉnh sửa' : 'Thêm'; ?> POI - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
    <style>
        :root {
            --primary: #4F46E5;
            --primary-dark: #4338CA;
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
        #map {
            height: 400px;
            border-radius: 0.75rem;
        }
        .form-label {
            font-weight: 500;
            color: #374151;
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
            <a class="nav-link active" href="pois.php"><i class="bi bi-geo"></i> Quản lý POI</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2><?php echo $id ? 'Chỉnh sửa POI' : 'Thêm POI mới'; ?></h2>
            <a href="pois.php" class="btn btn-outline-secondary">
                <i class="bi bi-arrow-left me-2"></i>Quay lại
            </a>
        </div>

        <form method="POST">
            <div class="row g-4">
                <!-- Left Column - Form -->
                <div class="col-lg-6">
                    <div class="card">
                        <div class="card-header">
                            <h5 class="mb-0">Thông tin POI</h5>
                        </div>
                        <div class="card-body">
                            <div class="mb-3">
                                <label class="form-label">Tên (Tiếng Việt) *</label>
                                <input type="text" class="form-control" name="nameVi" required 
                                       value="<?php echo htmlspecialchars($poi['nameVi'] ?? ''); ?>">
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Tên (Tiếng Anh)</label>
                                <input type="text" class="form-control" name="nameEn"
                                       value="<?php echo htmlspecialchars($poi['nameEn'] ?? ''); ?>">
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Mô tả (Tiếng Việt)</label>
                                <textarea class="form-control" name="descriptionVi" rows="3"><?php echo htmlspecialchars($poi['descriptionVi'] ?? ''); ?></textarea>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Mô tả (Tiếng Anh)</label>
                                <textarea class="form-control" name="descriptionEn" rows="3"><?php echo htmlspecialchars($poi['descriptionEn'] ?? ''); ?></textarea>
                            </div>
                        </div>
                    </div>

                    <div class="card mt-4">
                        <div class="card-header">
                            <h5 class="mb-0">Cài đặt Geofence</h5>
                        </div>
                        <div class="card-body">
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Bán kính (mét)</label>
                                    <input type="number" class="form-control" name="radius" min="10" max="1000"
                                           value="<?php echo $poi['radius'] ?? 100; ?>">
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Mức ưu tiên</label>
                                    <select class="form-select" name="priority">
                                        <option value="1" <?php echo ($poi['priority'] ?? 1) == 1 ? 'selected' : ''; ?>>1 - Thấp</option>
                                        <option value="2" <?php echo ($poi['priority'] ?? 1) == 2 ? 'selected' : ''; ?>>2 - Trung bình</option>
                                        <option value="3" <?php echo ($poi['priority'] ?? 1) == 3 ? 'selected' : ''; ?>>3 - Cao</option>
                                    </select>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Right Column - Map -->
                <div class="col-lg-6">
                    <div class="card">
                        <div class="card-header">
                            <h5 class="mb-0">Chọn vị trí trên bản đồ</h5>
                        </div>
                        <div class="card-body">
                            <div class="row mb-3">
                                <div class="col-6">
                                    <label class="form-label">Latitude</label>
                                    <input type="number" step="any" class="form-control" name="latitude" id="latitude" required
                                           value="<?php echo $poi['latitude'] ?? '10.762622'; ?>">
                                </div>
                                <div class="col-6">
                                    <label class="form-label">Longitude</label>
                                    <input type="number" step="any" class="form-control" name="longitude" id="longitude" required
                                           value="<?php echo $poi['longitude'] ?? '106.660172'; ?>">
                                </div>
                            </div>
                            <div id="map"></div>
                            <small class="text-muted">Click vào bản đồ để chọn vị trí</small>
                        </div>
                    </div>
                </div>
            </div>

            <div class="mt-4 d-flex gap-2">
                <button type="submit" class="btn btn-primary">
                    <i class="bi bi-check-lg me-2"></i>Lưu
                </button>
                <a href="pois.php" class="btn btn-outline-secondary">Hủy</a>
            </div>
        </form>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <script>
        const lat = parseFloat(document.getElementById('latitude').value) || 10.762622;
        const lng = parseFloat(document.getElementById('longitude').value) || 106.660172;
        
        const map = L.map('map').setView([lat, lng], 15);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);
        
        let marker = L.marker([lat, lng], {draggable: true}).addTo(map);
        
        marker.on('dragend', function(e) {
            const pos = e.target.getLatLng();
            document.getElementById('latitude').value = pos.lat.toFixed(6);
            document.getElementById('longitude').value = pos.lng.toFixed(6);
        });
        
        map.on('click', function(e) {
            marker.setLatLng(e.latlng);
            document.getElementById('latitude').value = e.latlng.lat.toFixed(6);
            document.getElementById('longitude').value = e.latlng.lng.toFixed(6);
        });
    </script>
</body>
</html>
