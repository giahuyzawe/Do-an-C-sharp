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
$sort = $_GET['sort'] ?? 'newest';

if ($search) {
    $pois = array_filter($pois, fn($p) => 
        stripos($p['nameVi'], $search) !== false || 
        stripos($p['nameEn'] ?? '', $search) !== false
    );
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
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Quản lý POI - Food Street Guide Admin</title>
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
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
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
                    <div class="col-md-4">
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

        <!-- POI Table -->
        <div class="card">
            <div class="card-body p-0">
                <div class="table-responsive">
                    <table class="table table-hover mb-0">
                        <thead class="table-light">
                            <tr>
                                <th>POI</th>
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
                                            <div class="fw-semibold"><?php echo htmlspecialchars($poi['nameVi']); ?></div>
                                            <small class="text-muted"><?php echo htmlspecialchars($poi['nameEn'] ?? '-'); ?></small>
                                        </div>
                                    </div>
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
</body>
</html>
