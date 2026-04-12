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

// Find duplicates
$duplicates = [];
$processed_ids = [];

foreach ($pois as $i => $poi1) {
    if (in_array($poi1['id'], $processed_ids)) continue;
    
    $duplicates_for_this_poi = [];
    
    foreach ($pois as $j => $poi2) {
        if ($i === $j) continue;
        if ($poi1['id'] == $poi2['id']) continue;
        
        // Check if same address
        $addr1 = trim($poi1['address'] ?? '');
        $addr2 = trim($poi2['address'] ?? '');
        $same_address = ($addr1 && $addr2 && strcasecmp($addr1, $addr2) === 0);
        
        // Check if close coordinates (within ~10m)
        $lat_diff = abs($poi1['latitude'] - $poi2['latitude']);
        $lng_diff = abs($poi1['longitude'] - $poi2['longitude']);
        $close_coords = ($lat_diff < 0.0001 && $lng_diff < 0.0001);
        
        if ($same_address || $close_coords) {
            $duplicates_for_this_poi[] = $poi2;
            $processed_ids[] = $poi2['id'];
        }
    }
    
    if (!empty($duplicates_for_this_poi)) {
        $duplicates[] = [
            'original' => $poi1,
            'duplicates' => $duplicates_for_this_poi
        ];
        $processed_ids[] = $poi1['id'];
    }
}

// Handle delete action
if (isset($_GET['delete']) && isset($_GET['group'])) {
    $delete_id = intval($_GET['delete']);
    $group_index = intval($_GET['group']);
    
    // Remove POI from array
    $pois = array_filter($pois, fn($p) => $p['id'] != $delete_id);
    file_put_contents($storage_file, json_encode(array_values($pois), JSON_PRETTY_PRINT));
    
    header('Location: pois-duplicates.php');
    exit;
}

// Handle merge action (keep first, delete others)
if (isset($_GET['merge']) && isset($_GET['ids'])) {
    $ids_to_delete = explode(',', $_GET['ids']);
    $keep_id = intval($ids_to_delete[0]); // Keep the first one
    
    $pois = array_filter($pois, fn($p) => $p['id'] == $keep_id || !in_array($p['id'], $ids_to_delete));
    file_put_contents($storage_file, json_encode(array_values($pois), JSON_PRETTY_PRINT));
    
    header('Location: pois-duplicates.php?merged=1');
    exit;
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Lọc POI Trùng - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
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
        .duplicate-group {
            border-left: 4px solid #EF4444;
            margin-bottom: 1.5rem;
        }
        .duplicate-item {
            background: #FEF2F2;
            border: 1px solid #FECACA;
        }
        .original-item {
            background: #F0FDF4;
            border: 1px solid #BBF7D0;
            border-left: 4px solid #10B981;
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
            <a class="nav-link active" href="pois-duplicates.php"><i class="bi bi-copy"></i> POI Trùng lặp</a>
            <a class="nav-link" href="reviews.php"><i class="bi bi-star-fill"></i> Reviews</a>
            <a class="nav-link" href="permissions.php"><i class="bi bi-shield-lock"></i> Phân quyền</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div>
                <h2><i class="bi bi-copy text-danger me-2"></i>Lọc POI Trùng lặp</h2>
                <p class="text-muted mb-0">Tìm và xử lý các POI có địa chỉ hoặc tọa độ trùng nhau</p>
            </div>
            <div>
                <a href="pois.php" class="btn btn-outline-secondary">
                    <i class="bi bi-arrow-left me-2"></i>Quay lại
                </a>
            </div>
        </div>

        <?php if (isset($_GET['merged'])): ?>
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            <i class="bi bi-check-circle-fill me-2"></i>Đã gộp và xóa các POI trùng lặp thành công!
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
        <?php endif; ?>

        <!-- Stats -->
        <div class="row mb-4">
            <div class="col-md-4">
                <div class="card">
                    <div class="card-body d-flex align-items-center">
                        <div class="flex-shrink-0">
                            <div class="bg-danger bg-opacity-10 rounded p-3">
                                <i class="bi bi-exclamation-triangle text-danger fs-4"></i>
                            </div>
                        </div>
                        <div class="flex-grow-1 ms-3">
                            <h6 class="mb-0">Nhóm trùng lặp</h6>
                            <h3 class="mb-0"><?php echo count($duplicates); ?></h3>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="card">
                    <div class="card-body d-flex align-items-center">
                        <div class="flex-shrink-0">
                            <div class="bg-warning bg-opacity-10 rounded p-3">
                                <i class="bi bi-layers text-warning fs-4"></i>
                            </div>
                        </div>
                        <div class="flex-grow-1 ms-3">
                            <h6 class="mb-0">Tổng POI trùng</h6>
                            <h3 class="mb-0">
                                <?php 
                                $total_dup = 0;
                                foreach ($duplicates as $d) $total_dup += count($d['duplicates']);
                                echo $total_dup;
                                ?>
                            </h3>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-4">
                <div class="card">
                    <div class="card-body d-flex align-items-center">
                        <div class="flex-shrink-0">
                            <div class="bg-info bg-opacity-10 rounded p-3">
                                <i class="bi bi-database text-info fs-4"></i>
                            </div>
                        </div>
                        <div class="flex-grow-1 ms-3">
                            <h6 class="mb-0">Tổng số POI</h6>
                            <h3 class="mb-0"><?php echo count($pois); ?></h3>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Duplicate Groups -->
        <?php if (empty($duplicates)): ?>
        <div class="card">
            <div class="card-body text-center py-5">
                <i class="bi bi-check-circle text-success fs-1 mb-3"></i>
                <h4>Không có POI trùng lặp</h4>
                <p class="text-muted">Tất cả POI đều có địa chỉ và tọa độ duy nhất.</p>
            </div>
        </div>
        <?php else: ?>
            <?php foreach ($duplicates as $index => $group): ?>
            <div class="card duplicate-group">
                <div class="card-header bg-white">
                    <div class="d-flex justify-content-between align-items-center">
                        <h5 class="mb-0">
                            <i class="bi bi-exclamation-triangle-fill text-danger me-2"></i>
                            Nhóm trùng #<?php echo $index + 1; ?>
                        </h5>
                        <div>
                            <?php
                            $all_ids = array_merge([$group['original']['id']], array_column($group['duplicates'], 'id'));
                            $ids_str = implode(',', $all_ids);
                            ?>
                            <a href="?merge=1&ids=<?php echo $ids_str; ?>" 
                               class="btn btn-success btn-sm"
                               onclick="return confirm('Bạn có chắc muốn GỘP các POI này? Chỉ giữ lại POI đầu tiên, xóa tất cả POI trùng?')">
                                <i class="bi bi-layers me-1"></i>Gộp nhóm
                            </a>
                        </div>
                    </div>
                </div>
                <div class="card-body">
                    <!-- Original -->
                    <div class="d-flex align-items-center p-3 mb-2 original-item rounded">
                        <div class="flex-shrink-0">
                            <span class="badge bg-success">GIỮ LẠI</span>
                        </div>
                        <div class="flex-grow-1 ms-3">
                            <h6 class="mb-1"><?php echo htmlspecialchars($group['original']['nameVi']); ?></h6>
                            <p class="mb-1 text-muted small">
                                <i class="bi bi-geo-alt me-1"></i>
                                <?php echo htmlspecialchars($group['original']['address'] ?? 'Chưa có địa chỉ'); ?>
                            </p>
                            <p class="mb-0 text-muted small">
                                <i class="bi bi-map me-1"></i>
                                <?php echo $group['original']['latitude']; ?>, <?php echo $group['original']['longitude']; ?>
                            </p>
                        </div>
                        <div class="flex-shrink-0">
                            <a href="poi-form.php?id=<?php echo $group['original']['id']; ?>" class="btn btn-outline-primary btn-sm me-1">
                                <i class="bi bi-pencil"></i>
                            </a>
                        </div>
                    </div>

                    <!-- Duplicates -->
                    <?php foreach ($group['duplicates'] as $dup): ?>
                    <div class="d-flex align-items-center p-3 mb-2 duplicate-item rounded">
                        <div class="flex-shrink-0">
                            <span class="badge bg-danger">TRÙNG</span>
                        </div>
                        <div class="flex-grow-1 ms-3">
                            <h6 class="mb-1"><?php echo htmlspecialchars($dup['nameVi']); ?></h6>
                            <p class="mb-1 text-muted small">
                                <i class="bi bi-geo-alt me-1"></i>
                                <?php echo htmlspecialchars($dup['address'] ?? 'Chưa có địa chỉ'); ?>
                            </p>
                            <p class="mb-0 text-muted small">
                                <i class="bi bi-map me-1"></i>
                                <?php echo $dup['latitude']; ?>, <?php echo $dup['longitude']; ?>
                            </p>
                        </div>
                        <div class="flex-shrink-0">
                            <a href="poi-form.php?id=<?php echo $dup['id']; ?>" class="btn btn-outline-primary btn-sm me-1">
                                <i class="bi bi-pencil"></i>
                            </a>
                            <a href="?delete=<?php echo $dup['id']; ?>&group=<?php echo $index; ?>" 
                               class="btn btn-outline-danger btn-sm"
                               onclick="return confirm('Bạn có chắc muốn XÓA POI này?')">
                                <i class="bi bi-trash"></i>
                            </a>
                        </div>
                    </div>
                    <?php endforeach; ?>
                </div>
            </div>
            <?php endforeach; ?>
        <?php endif; ?>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
