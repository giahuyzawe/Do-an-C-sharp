<?php
session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

// Load POIs
$storage_file = 'pois.json';
$pois = [];
if (file_exists($storage_file)) {
    $pois = json_decode(file_get_contents($storage_file), true) ?: [];
}

// Search & Filter
$search = $_GET['search'] ?? '';
$category = $_GET['category'] ?? '';
$sort = $_GET['sort'] ?? 'newest';

if ($search) {
    $pois = array_filter($pois, fn($p) => 
        stripos($p['nameVi'], $search) !== false || 
        stripos($p['nameEn'] ?? '', $search) !== false
    );
}

if ($category) {
    $pois = array_filter($pois, fn($p) => ($p['category'] ?? 'landmark') == $category);
}

// Sort
usort($pois, function($a, $b) use ($sort) {
    return match($sort) {
        'newest' => strtotime($b['createdAt']) <=> strtotime($a['createdAt']),
        'oldest' => strtotime($a['createdAt']) <=> strtotime($b['createdAt']),
        'popular' => ($b['visitCount'] ?? 0) <=> ($a['visitCount'] ?? 0),
        default => 0
    };
});

// Delete POI
if (isset($_GET['delete'])) {
    $id = $_GET['delete'];
    $pois = array_filter($pois, fn($p) => $p['id'] != $id);
    file_put_contents($storage_file, json_encode(array_values($pois), JSON_PRETTY_PRINT));
    header('Location: pois.php');
    exit;
}

// Check for duplicates
$duplicate_count = 0;
$processed = [];
foreach ($pois as $i => $p1) {
    if (in_array($p1['id'], $processed)) continue;
    foreach ($pois as $j => $p2) {
        if ($i === $j) continue;
        $addr1 = trim($p1['address'] ?? '');
        $addr2 = trim($p2['address'] ?? '');
        $same_addr = ($addr1 && $addr2 && strcasecmp($addr1, $addr2) === 0);
        $close_coords = (abs($p1['latitude'] - $p2['latitude']) < 0.0001 && abs($p1['longitude'] - $p2['longitude']) < 0.0001);
        if ($same_addr || $close_coords) {
            $duplicate_count++;
            $processed[] = $p1['id'];
            break;
        }
    }
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Quản lý POI - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
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
        .table th {
            font-weight: 600;
            color: #64748B;
            text-transform: uppercase;
            font-size: 0.75rem;
            letter-spacing: 0.5px;
        }
        .badge-priority {
            padding: 0.5em 0.75em;
            border-radius: 0.5rem;
            font-size: 0.75rem;
            font-weight: 600;
        }
        .priority-high { background: #FEE2E2; color: #DC2626; }
        .priority-medium { background: #FEF3C7; color: #D97706; }
        .priority-low { background: #D1FAE5; color: #059669; }
        .btn-action {
            padding: 0.375rem 0.75rem;
            border-radius: 0.5rem;
            font-size: 0.875rem;
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
            <a class="nav-link" href="pois-duplicates.php"><i class="bi bi-copy text-danger"></i> POI Trùng lặp</a>
            <a class="nav-link" href="reviews.php"><i class="bi bi-star-fill"></i> Reviews</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="permissions.php"><i class="bi bi-shield-lock"></i> Phân quyền</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <?php if ($duplicate_count > 0): ?>
        <div class="alert alert-warning alert-dismissible fade show mb-4" role="alert">
            <div class="d-flex align-items-center">
                <i class="bi bi-exclamation-triangle-fill fs-4 me-3"></i>
                <div>
                    <strong>Phát hiện <?php echo $duplicate_count; ?> POI trùng lặp!</strong>
                    <p class="mb-0">Có <?php echo $duplicate_count; ?> quán có địa chỉ hoặc tọa độ trùng nhau. <a href="pois-duplicates.php" class="alert-link">Xem chi tiết và xử lý →</a></p>
                </div>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
        <?php endif; ?>

        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2>Quản lý POI</h2>
            <a href="poi-form.php" class="btn btn-primary">
                <i class="bi bi-plus-lg me-2"></i>Thêm POI mới
            </a>
        </div>

        <!-- Search & Filter -->
        <div class="card mb-4">
            <div class="card-body">
                <form method="GET" class="row g-3">
                    <div class="col-md-6">
                        <div class="input-group">
                            <span class="input-group-text bg-white border-end-0">
                                <i class="bi bi-search text-muted"></i>
                            </span>
                            <input type="text" class="form-control border-start-0" name="search" 
                                   placeholder="Tìm kiếm POI..." value="<?php echo htmlspecialchars($search); ?>">
                        </div>
                    </div>
                    <div class="col-md-3">
                        <select class="form-select" name="category">
                            <option value="">Tất cả Category</option>
                            <option value="landmark" <?php echo $category == 'landmark' ? 'selected' : ''; ?>>Landmark</option>
                            <option value="restaurant" <?php echo $category == 'restaurant' ? 'selected' : ''; ?>>Restaurant</option>
                            <option value="cafe" <?php echo $category == 'cafe' ? 'selected' : ''; ?>>Cafe</option>
                            <option value="bar" <?php echo $category == 'bar' ? 'selected' : ''; ?>>Bar</option>
                            <option value="market" <?php echo $category == 'market' ? 'selected' : ''; ?>>Market</option>
                            <option value="night_market" <?php echo $category == 'night_market' ? 'selected' : ''; ?>>Night Market</option>
                            <option value="street_food" <?php echo $category == 'street_food' ? 'selected' : ''; ?>>Street Food</option>
                            <option value="temple" <?php echo $category == 'temple' ? 'selected' : ''; ?>>Temple</option>
                            <option value="park" <?php echo $category == 'park' ? 'selected' : ''; ?>>Park</option>
                            <option value="museum" <?php echo $category == 'museum' ? 'selected' : ''; ?>>Museum</option>
                        </select>
                    </div>
                    <div class="col-md-3">
                        <select class="form-select" name="sort">
                            <option value="newest" <?php echo $sort == 'newest' ? 'selected' : ''; ?>>Mới nhất</option>
                            <option value="oldest" <?php echo $sort == 'oldest' ? 'selected' : ''; ?>>Cũ nhất</option>
                            <option value="popular" <?php echo $sort == 'popular' ? 'selected' : ''; ?>>Phổ biến nhất</option>
                        </select>
                    </div>
                    <div class="col-md-2">
                        <button type="submit" class="btn btn-outline-primary w-100">Lọc</button>
                    </div>
                </form>
            </div>
        </div>

        <!-- POI Map -->
        <div class="card mb-4">
            <div class="card-header bg-white border-0 pt-4 px-4">
                <h5 class="mb-0"><i class="bi bi-map me-2 text-primary"></i>Bản đồ POI</h5>
                <small class="text-muted">Hiển thị <?php echo count($pois); ?> địa điểm trên bản đồ</small>
            </div>
            <div class="card-body px-4 pb-4">
                <div id="poiMap" style="height: 400px; border-radius: 0.75rem; border: 1px solid #E2E8F0;"></div>
                <div class="mt-3 d-flex flex-wrap gap-2">
                    <span class="badge" style="background: #4F46E5;">Landmark</span>
                    <span class="badge" style="background: #EF4444;">Restaurant</span>
                    <span class="badge" style="background: #F59E0B;">Cafe</span>
                    <span class="badge" style="background: #8B5CF6;">Bar</span>
                    <span class="badge" style="background: #14B8A6;">Market</span>
                    <span class="badge" style="background: #EC4899;">Night Market</span>
                    <span class="badge" style="background: #F97316;">Street Food</span>
                    <span class="badge" style="background: #10B981;">Temple</span>
                    <span class="badge" style="background: #22C55E;">Park</span>
                    <span class="badge" style="background: #06B6D4;">Museum</span>
                    <span class="badge" style="background: #6366F1;">Shopping</span>
                    <span class="badge" style="background: #3B82F6;">Bridge</span>
                </div>
            </div>
        </div>

        <!-- POI Table -->
        <div class="card">
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-hover mb-0">
                        <thead class="table-light">
                            <tr>
                                <th>POI</th>
                                <th>Category</th>
                                <th>Tọa độ</th>
                                <th>Bán kính</th>
                                <th>Ưu tiên</th>
                                <th>Lượt xem</th>
                                <th>Ngày tạo</th>
                                <th>Thao tác</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($pois as $poi): ?>
                            <tr>
                                <td>
                                    <div class="d-flex align-items-center">
                                        <div class="bg-primary bg-opacity-10 text-primary rounded-circle d-flex align-items-center justify-content-center me-3" style="width: 40px; height: 40px;">
                                            <i class="bi bi-geo-alt-fill"></i>
                                        </div>
                                        <div>
                                            <div class="fw-semibold">
                                                <?php echo htmlspecialchars($poi['nameVi']); ?>
                                            </div>
                                            <small class="text-muted"><?php echo htmlspecialchars($poi['nameEn'] ?? '-'); ?></small>
                                        </div>
                                    </div>
                                </td>
                                <td>
                                    <span class="badge bg-light text-dark border"><?php echo htmlspecialchars($poi['category'] ?? 'landmark'); ?></span>
                                </td>
                                <td>
                                    <small class="text-muted">
                                        <?php echo number_format($poi['latitude'], 6); ?>, <br>
                                        <?php echo number_format($poi['longitude'], 6); ?>
                                    </small>
                                </td>
                                <td><?php echo $poi['radius']; ?> m</td>
                                <td>
                                    <span class="badge-priority <?php 
                                        echo $poi['priority'] >= 3 ? 'priority-high' : 
                                            ($poi['priority'] == 2 ? 'priority-medium' : 'priority-low'); ?>">
                                        <?php echo $poi['priority']; ?>
                                    </span>
                                </td>
                                <td>
                                    <span class="fw-semibold"><?php echo $poi['visitCount'] ?? 0; ?></span>
                                </td>
                                <td>
                                    <small class="text-muted"><?php echo date('d/m/Y', strtotime($poi['createdAt'])); ?></small>
                                </td>
                                <td>
                                    <a href="poi-form.php?id=<?php echo $poi['id']; ?>" class="btn btn-light btn-action me-1">
                                        <i class="bi bi-pencil"></i>
                                    </a>
                                    <a href="?delete=<?php echo $poi['id']; ?>" class="btn btn-light btn-action text-danger" onclick="return confirm('Xóa POI này?')">
                                        <i class="bi bi-trash"></i>
                                    </a>
                                </td>
                            </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <div class="mt-3 text-muted">
            <small>Tổng số: <?php echo count($pois); ?> POI</small>
        </div>
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

        // Create custom colored marker icon
        function createCustomIcon(color) {
            return L.divIcon({
                className: 'custom-marker',
                html: `<div style="
                    background-color: ${color};
                    width: 28px;
                    height: 28px;
                    border-radius: 50% 50% 50% 0;
                    border: 3px solid white;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.4);
                    transform: rotate(-45deg);
                "></div>`,
                iconSize: [28, 28],
                iconAnchor: [14, 28],
                popupAnchor: [0, -28]
            });
        }

        // Initialize map
        const map = L.map('poiMap').setView([10.762622, 106.660172], 12);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);

        // POI data from PHP
        const pois = <?php echo json_encode($pois); ?>;

        // Add markers for each POI
        const markers = [];
        pois.forEach(poi => {
            const color = categoryColors[poi.category] || '#4F46E5';
            const marker = L.marker([poi.latitude, poi.longitude], {
                icon: createCustomIcon(color)
            }).addTo(map);

            // Popup content
            marker.bindPopup(`
                <div style="min-width: 220px;">
                    <h6 class="mb-1 fw-semibold">${poi.nameVi}</h6>
                    <span class="badge" style="background-color: ${color}; font-size: 0.7rem;">${poi.category || 'landmark'}</span>
                    <div class="mt-2 text-muted small">
                        <div><i class="bi bi-geo-alt me-1"></i>${parseFloat(poi.latitude).toFixed(6)}, ${parseFloat(poi.longitude).toFixed(6)}</div>
                        <div><i class="bi bi-arrows-angle-expand me-1"></i>Bán kính: ${poi.radius}m</div>
                        ${poi.priority ? `<div><i class="bi bi-flag me-1"></i>Ưu tiên: ${poi.priority}</div>` : ''}
                    </div>
                    ${poi.narrationTextVi ? `
                    <div class="mt-2 p-2 bg-light rounded border-start border-info border-3">
                        <small class="text-muted d-block mb-1"><i class="bi bi-mic-fill me-1"></i>Thuyết minh:</small>
                        <p class="small mb-1" style="max-height: 60px; overflow-y: auto;">${poi.narrationTextVi.substring(0, 80)}${poi.narrationTextVi.length > 80 ? '...' : ''}</p>
                    </div>
                    ` : ''}
                    <div class="mt-2">
                        <a href="poi-form.php?id=${poi.id}" class="btn btn-sm btn-primary">Sửa</a>
                    </div>
                </div>
            `);

            markers.push(marker);
        });

        // Fit map to show all markers if there are POIs
        if (markers.length > 0) {
            const group = new L.featureGroup(markers);
            map.fitBounds(group.getBounds().pad(0.1));
        }
    </script>
</body>
</html>
