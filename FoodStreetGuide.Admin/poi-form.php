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
    $latitude = floatval($_POST['latitude'] ?? 0);
    $longitude = floatval($_POST['longitude'] ?? 0);
    $address = trim($_POST['address'] ?? '');
    $nameVi = trim($_POST['nameVi'] ?? '');
    
    // Check for duplicate address (within 10 meters radius)
    $duplicate_found = false;
    $duplicate_name = '';
    foreach ($pois as $p) {
        // Skip current POI when editing
        if ($id && $p['id'] == $id) continue;
        
        // Check if same address or very close coordinates (within 10m)
        $existing_addr = trim($p['address'] ?? '');
        $lat_diff = abs($p['latitude'] - $latitude);
        $lng_diff = abs($p['longitude'] - $longitude);
        
        // Approximate 10m in degrees (rough estimate)
        if (($existing_addr && strcasecmp($existing_addr, $address) === 0) || 
            ($lat_diff < 0.0001 && $lng_diff < 0.0001)) {
            $duplicate_found = true;
            $duplicate_name = $p['nameVi'];
            break;
        }
    }
    
    if ($duplicate_found) {
        $error_message = "Không thể lưu! Địa điểm này trùng với quán: '$duplicate_name'. Vui lòng chọn địa chỉ hoặc tọa độ khác.";
    } else {
        // Handle image uploads
        $images = [];
        $upload_dir = 'uploads/';
        if (!is_dir($upload_dir)) {
            mkdir($upload_dir, 0755, true);
        }
        
        // Process uploaded files
        if (!empty($_FILES['images']['name'][0])) {
            foreach ($_FILES['images']['name'] as $key => $name) {
                if ($_FILES['images']['error'][$key] === UPLOAD_ERR_OK) {
                    $tmp_name = $_FILES['images']['tmp_name'][$key];
                    $ext = strtolower(pathinfo($name, PATHINFO_EXTENSION));
                    $allowed = ['jpg', 'jpeg', 'png', 'gif', 'webp'];
                    
                    if (in_array($ext, $allowed)) {
                        $new_filename = uniqid() . '.' . $ext;
                        $destination = $upload_dir . $new_filename;
                        
                        if (move_uploaded_file($tmp_name, $destination)) {
                            $images[] = $destination;
                        }
                    }
                }
            }
        }
        
        // Keep existing images if editing
        if ($id && $poi && !empty($poi['images'])) {
            $existing_images = $poi['images'];
            // Merge new images with existing
            $images = array_merge($existing_images, $images);
        }
        
        $new_poi = [
            'id' => $id ?: time(),
            'nameVi' => $nameVi,
            'nameEn' => $_POST['nameEn'] ?? '',
            'descriptionVi' => $_POST['descriptionVi'] ?? '',
            'descriptionEn' => $_POST['descriptionEn'] ?? '',
            'latitude' => $latitude,
            'longitude' => $longitude,
            'address' => $address,
            'radius' => intval($_POST['radius'] ?? 100),
            'priority' => intval($_POST['priority'] ?? 1),
            'createdAt' => $poi['createdAt'] ?? date('Y-m-d H:i:s'),
            'openingHours' => $_POST['openingHours'] ?? ($poi['openingHours'] ?? ''),
            'category' => $_POST['category'] ?? ($poi['category'] ?? 'landmark'),
            'tags' => $_POST['tags'] ?? ($poi['tags'] ?? ''),
            'status' => $poi['status'] ?? 'active',
            'visitCount' => $poi['visitCount'] ?? 0,
            'images' => $images
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
            <a class="nav-link" href="reviews.php"><i class="bi bi-star-fill"></i> Reviews</a>
            <a class="nav-link" href="permissions.php"><i class="bi bi-shield-lock"></i> Phân quyền</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
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

        <?php if (isset($error_message)): ?>
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            <i class="bi bi-exclamation-triangle-fill me-2"></i><?php echo $error_message; ?>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
        <?php endif; ?>

        <form method="POST" enctype="multipart/form-data">
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
                                <label class="form-label">Địa chỉ *</label>
                                <input type="text" class="form-control" name="address" required
                                       value="<?php echo htmlspecialchars($poi['address'] ?? ''); ?>">
                                <small class="text-muted">Địa chỉ dùng để kiểm tra trùng lặp với các quán khác</small>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Giờ mở cửa</label>
                                <input type="text" class="form-control" name="openingHours" 
                                       value="<?php echo htmlspecialchars($poi['openingHours'] ?? ''); ?>"
                                       placeholder="Ví dụ: 07:00 - 22:00">
                                <small class="text-muted">Định dạng: HH:MM - HH:MM hoặc mô tả tự do</small>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Mô tả (Tiếng Việt)</label>
                                <textarea class="form-control" name="descriptionVi" rows="3"><?php echo htmlspecialchars($poi['descriptionVi'] ?? ''); ?></textarea>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Mô tả (Tiếng Anh)</label>
                                <textarea class="form-control" name="descriptionEn" rows="3"><?php echo htmlspecialchars($poi['descriptionEn'] ?? ''); ?></textarea>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Category</label>
                                <select class="form-select" name="category">
                                    <option value="landmark" <?php echo ($poi['category'] ?? 'landmark') == 'landmark' ? 'selected' : ''; ?>>Landmark</option>
                                    <option value="restaurant" <?php echo ($poi['category'] ?? '') == 'restaurant' ? 'selected' : ''; ?>>Restaurant</option>
                                    <option value="cafe" <?php echo ($poi['category'] ?? '') == 'cafe' ? 'selected' : ''; ?>>Cafe</option>
                                    <option value="bar" <?php echo ($poi['category'] ?? '') == 'bar' ? 'selected' : ''; ?>>Bar</option>
                                    <option value="market" <?php echo ($poi['category'] ?? '') == 'market' ? 'selected' : ''; ?>>Market</option>
                                    <option value="night_market" <?php echo ($poi['category'] ?? '') == 'night_market' ? 'selected' : ''; ?>>Night Market</option>
                                    <option value="street_food" <?php echo ($poi['category'] ?? '') == 'street_food' ? 'selected' : ''; ?>>Street Food</option>
                                    <option value="temple" <?php echo ($poi['category'] ?? '') == 'temple' ? 'selected' : ''; ?>>Temple</option>
                                    <option value="park" <?php echo ($poi['category'] ?? '') == 'park' ? 'selected' : ''; ?>>Park</option>
                                    <option value="museum" <?php echo ($poi['category'] ?? '') == 'museum' ? 'selected' : ''; ?>>Museum</option>
                                    <option value="shopping" <?php echo ($poi['category'] ?? '') == 'shopping' ? 'selected' : ''; ?>>Shopping</option>
                                    <option value="bridge" <?php echo ($poi['category'] ?? '') == 'bridge' ? 'selected' : ''; ?>>Bridge</option>
                                </select>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">Tags</label>
                                <input type="text" class="form-control" name="tags" 
                                       value="<?php echo htmlspecialchars($poi['tags'] ?? ''); ?>"
                                       placeholder="Ví dụ: bánh mì, đường phố, ăn vặt">
                                <small class="text-muted">Các tag phân cách bằng dấu phẩy</small>
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

                    <div class="card mt-4">
                        <div class="card-header">
                            <h5 class="mb-0"><i class="bi bi-images me-2 text-success"></i>Hình ảnh</h5>
                        </div>
                        <div class="card-body">
                            <?php if ($id && !empty($poi['images'])): ?>
                            <div class="mb-3">
                                <label class="form-label">Hình ảnh hiện tại</label>
                                <div class="row g-2">
                                    <?php foreach ($poi['images'] as $img): ?>
                                    <div class="col-4">
                                        <img src="<?php echo htmlspecialchars($img); ?>" class="img-thumbnail" style="height: 100px; object-fit: cover; width: 100%;">
                                    </div>
                                    <?php endforeach; ?>
                                </div>
                            </div>
                            <?php endif; ?>
                            <div class="mb-3">
                                <label class="form-label">Thêm hình ảnh mới</label>
                                <input type="file" class="form-control" name="images[]" accept="image/*" multiple>
                                <small class="text-muted">Có thể chọn nhiều ảnh (JPG, PNG, GIF, WebP). Ảnh sẽ được lưu tự động khi bấm Lưu.</small>
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
        // Category colors configuration
        const categoryColors = {
            'landmark': '#4F46E5',      // Indigo
            'restaurant': '#EF4444',    // Red
            'cafe': '#F59E0B',         // Amber
            'bar': '#8B5CF6',          // Purple
            'market': '#14B8A6',        // Teal
            'night_market': '#EC4899', // Pink
            'street_food': '#F97316',  // Orange
            'temple': '#10B981',       // Emerald
            'park': '#22C55E',         // Green
            'museum': '#06B6D4',       // Cyan
            'shopping': '#6366F1',     // Indigo
            'bridge': '#3B82F6'        // Blue
        };

        const categoryNames = {
            'landmark': 'Landmark',
            'restaurant': 'Restaurant',
            'cafe': 'Cafe',
            'bar': 'Bar',
            'market': 'Market',
            'night_market': 'Night Market',
            'street_food': 'Street Food',
            'temple': 'Temple',
            'park': 'Park',
            'museum': 'Museum',
            'shopping': 'Shopping',
            'bridge': 'Bridge'
        };

        // Create custom colored marker icon
        function createCustomIcon(color) {
            return L.divIcon({
                className: 'custom-marker',
                html: `<div style="
                    background-color: ${color};
                    width: 30px;
                    height: 30px;
                    border-radius: 50% 50% 50% 0;
                    border: 3px solid white;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.4);
                    transform: rotate(-45deg);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                "></div>`,
                iconSize: [30, 30],
                iconAnchor: [15, 30],
                popupAnchor: [0, -30]
            });
        }

        const lat = parseFloat(document.getElementById('latitude').value) || 10.762622;
        const lng = parseFloat(document.getElementById('longitude').value) || 106.660172;
        
        const map = L.map('map').setView([lat, lng], 15);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);

        // Get current category
        const categorySelect = document.querySelector('select[name="category"]');
        const currentCategory = categorySelect ? categorySelect.value : 'landmark';
        const markerColor = categoryColors[currentCategory] || '#4F46E5';
        
        // Create marker with custom icon
        let marker = L.marker([lat, lng], {
            draggable: true,
            icon: createCustomIcon(markerColor)
        }).addTo(map);

        // Add popup showing category
        function updatePopup() {
            const cat = categorySelect ? categorySelect.value : 'landmark';
            const catName = categoryNames[cat] || 'POI';
            const catColor = categoryColors[cat] || '#4F46E5';
            marker.bindPopup(`
                <div style="text-align: center;">
                    <span style="
                        background-color: ${catColor};
                        color: white;
                        padding: 4px 12px;
                        border-radius: 12px;
                        font-size: 12px;
                        font-weight: 600;
                    ">${catName}</span>
                    <div style="margin-top: 8px; font-size: 11px; color: #666;">
                        ${lat.toFixed(6)}, ${lng.toFixed(6)}
                    </div>
                </div>
            `).openPopup();
        }
        updatePopup();
        
        // Update marker color when category changes
        if (categorySelect) {
            categorySelect.addEventListener('change', function() {
                const newCategory = this.value;
                const newColor = categoryColors[newCategory] || '#4F46E5';
                marker.setIcon(createCustomIcon(newColor));
                updatePopup();
            });
        }
        
        marker.on('dragend', function(e) {
            const pos = e.target.getLatLng();
            document.getElementById('latitude').value = pos.lat.toFixed(6);
            document.getElementById('longitude').value = pos.lng.toFixed(6);
            updatePopup();
        });
        
        map.on('click', function(e) {
            marker.setLatLng(e.latlng);
            document.getElementById('latitude').value = e.latlng.lat.toFixed(6);
            document.getElementById('longitude').value = e.latlng.lng.toFixed(6);
            updatePopup();
        });

        // Show popup on marker click
        marker.on('click', function() {
            updatePopup();
            marker.openPopup();
        });

    </script>
</body>
</html>
