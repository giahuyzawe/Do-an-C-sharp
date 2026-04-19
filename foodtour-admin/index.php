<?php
// Prevent browser caching
header('Cache-Control: no-cache, no-store, must-revalidate');
header('Pragma: no-cache');
header('Expires: 0');

require_once 'config.php';
require_auth();

// Add version for cache busting
$version = time(); // Changes every second

$user = current_user();
$role = $user['role'];

// Load data based on role
$pois = load_json($POIS_FILE);
$analytics = load_json($ANALYTICS_FILE);

if ($role === 'restaurant_owner') {
    // Owner only sees their restaurants - filter by ownerId
    $pois = array_filter($pois, fn($p) => ($p['ownerId'] ?? '') === $user['id']);
}

// Calculate stats
$totalPOIs = count($pois);
$approvedPOIs = count(array_filter($pois, fn($p) => ($p['status'] ?? 'pending') === 'approved'));
$pendingPOIs = count(array_filter($pois, fn($p) => ($p['status'] ?? 'pending') === 'pending'));

// Today's analytics
$today = date('Y-m-d');
$todayAppVisits = array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'app_visit');
$todayStats = [
    'dau' => count(array_unique(array_column($todayAppVisits, 'deviceId'))), // Unique devices
    'views' => count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'poi_view')),
    'checkins' => count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'check_in'))
];

// Total views from analytics (consistent with statistics)
$totalViews = count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'poi_view'));

$pageTitle = $role === 'admin' ? 'Dashboard Quản trị' : 'Dashboard Nhà hàng';
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= $pageTitle ?> - Food Tour</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
    <style>
        .sidebar {
            min-height: 100vh;
            background: #1e293b;
            color: white;
        }
        .sidebar .nav-link {
            color: #94a3b8;
            padding: 12px 20px;
        }
        .sidebar .nav-link:hover, .sidebar .nav-link.active {
            color: white;
            background: rgba(255,255,255,0.1);
        }
        .stat-card {
            border-radius: 12px;
            padding: 20px;
            color: white;
        }
        .stat-card.blue { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .stat-card.green { background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%); }
        .stat-card.orange { background: linear-gradient(135deg, #fa709a 0%, #fee140 100%); }
        .stat-card.red { background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); }
        .stat-number { font-size: 2rem; font-weight: bold; }
        .role-badge {
            background: <?= $role === 'admin' ? '#667eea' : '#43e97b' ?>;
            color: white;
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 0.85rem;
        }
    </style>
</head>
<body>
    <div class="container-fluid">
        <div class="row">
            <!-- Sidebar -->
            <div class="col-md-2 sidebar p-0">
                <div class="p-3 border-bottom border-secondary">
                    <h5 class="m-0">🍜 Food Tour</h5>
                    <small class="text-muted">Vĩnh Khánh</small>
                </div>
                <nav class="nav flex-column">
                    <a class="nav-link active" href="index.php"><i class="bi bi-speedometer2"></i> Dashboard</a>
                    
                    <?php if ($role === 'admin'): ?>
                        <!-- Admin: Focus on approval and management -->
                        <a class="nav-link text-warning" href="restaurant-approval.php"><i class="bi bi-check-circle-fill"></i> <strong>Duyệt nhà hàng</strong></a>
                        <a class="nav-link" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng</a>
                        <a class="nav-link" href="users.php"><i class="bi bi-people"></i> Người dùng</a>
                    <?php else: ?>
                        <!-- Owner: Manage their restaurants -->
                        <a class="nav-link" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng của tôi</a>
                        <a class="nav-link" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
                        <a class="nav-link" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
                    <?php endif; ?>
                    
                    <a class="nav-link" href="reviews.php"><i class="bi bi-star"></i> Đánh giá</a>
                    <a class="nav-link" href="statistics.php"><i class="bi bi-graph-up"></i> Thống kê</a>
                    
                    <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <!-- Main Content -->
            <div class="col-md-10 p-4">
                <div class="d-flex justify-content-between align-items-center mb-4">
                    <div>
                        <h4><?= $pageTitle ?></h4>
                        <span class="role-badge">
                            <?= $role === 'admin' ? '👤 Quản trị viên' : '🏪 Chủ nhà hàng' ?>
                        </span>
                    </div>
                    <div class="text-end">
                        <div class="fw-bold"><?= htmlspecialchars($user['name']) ?></div>
                        <small class="text-muted"><?= date('d/m/Y H:i') ?></small>
                    </div>
                </div>

                <!-- Stats Cards -->
                <div class="row g-3 mb-4">
                    <?php if ($role === 'admin'): ?>
                        <div class="col-md-3">
                            <div class="stat-card blue">
                                <div class="stat-number"><?= $totalPOIs ?></div>
                                <div>Tổng nhà hàng</div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="stat-card green">
                                <div class="stat-number"><?= $approvedPOIs ?></div>
                                <div>Đã duyệt</div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="stat-card orange">
                                <div class="stat-number"><?= $pendingPOIs ?></div>
                                <div>Chờ duyệt</div>
                            </div>
                        </div>
                        <div class="col-md-3">
                            <div class="stat-card red">
                                <div class="stat-number"><?= $todayStats['dau'] ?></div>
                                <div>DAU Hôm nay</div>
                            </div>
                        </div>
                    <?php else: ?>
                        <div class="col-md-4">
                            <div class="stat-card blue">
                                <div class="stat-number"><?= $totalPOIs ?></div>
                                <div>Nhà hàng</div>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="stat-card green">
                                <div class="stat-number"><?= number_format($totalViews) ?></div>
                                <div>Tổng lượt xem</div>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="stat-card orange">
                                <div class="stat-number"><?= $todayStats['checkins'] ?></div>
                                <div>Check-in hôm nay</div>
                            </div>
                        </div>
                    <?php endif; ?>
                </div>

                <!-- Quick Actions -->
                <div class="card mb-4">
                    <div class="card-header fw-bold">⚡ Thao tác nhanh</div>
                    <div class="card-body">
                        <div class="row g-2">
                            <?php if ($role === 'admin'): ?>
                                <!-- Admin: Approval focus -->
                                <div class="col-md-4">
                                    <a href="restaurant-approval.php" class="btn btn-warning w-100">
                                        <i class="bi bi-check-circle-fill"></i> Duyệt nhà hàng
                                        <?= $pendingPOIs > 0 ? "<span class='badge bg-danger ms-1'>$pendingPOIs chờ</span>" : '' ?>
                                    </a>
                                </div>
                                <div class="col-md-4">
                                    <a href="users.php" class="btn btn-outline-primary w-100">
                                        <i class="bi bi-people"></i> Quản lý chủ quán
                                    </a>
                                </div>
                                <div class="col-md-4">
                                    <a href="pois.php" class="btn btn-outline-secondary w-100">
                                        <i class="bi bi-shop"></i> Xem tất cả nhà hàng
                                    </a>
                                </div>
                            <?php else: ?>
                                <!-- Owner: Restaurant management -->
                                <div class="col-md-3">
                                    <a href="poi-form.php" class="btn btn-primary w-100">
                                        <i class="bi bi-plus-lg"></i> Thêm nhà hàng
                                    </a>
                                </div>
                                <div class="col-md-3">
                                    <a href="audio-management.php" class="btn btn-outline-info w-100">
                                        <i class="bi bi-mic"></i> Audio thuyết minh
                                    </a>
                                </div>
                                <div class="col-md-3">
                                    <a href="qr-generator.php" class="btn btn-outline-success w-100">
                                        <i class="bi bi-qr-code"></i> Tạo QR Code
                                    </a>
                                </div>
                                <div class="col-md-3">
                                    <a href="analytics.php" class="btn btn-outline-secondary w-100">
                                        <i class="bi bi-graph-up"></i> Thống kê
                                    </a>
                                </div>
                            <?php endif; ?>
                        </div>
                    </div>
                </div>

                <!-- Restaurant List -->
                <div class="card">
                    <div class="card-header fw-bold d-flex justify-content-between align-items-center">
                        <span><?= $role === 'admin' ? '🏪 Danh sách nhà hàng' : '🏪 Nhà hàng của tôi' ?></span>
                        <a href="pois.php" class="btn btn-sm btn-outline-primary">Xem tất cả</a>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-hover mb-0">
                            <thead>
                                <tr>
                                    <th>Tên</th>
                                    <th>Địa chỉ</th>
                                    <th>Trạng thái</th>
                                    <th>Lượt xem</th>
                                    <th>Thao tác</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach (array_slice($pois, 0, 5) as $poi): ?>
                                <tr>
                                    <td><strong><?= htmlspecialchars($poi['nameVi'] ?? $poi['name'] ?? 'N/A') ?></strong></td>
                                    <td><?= htmlspecialchars($poi['address'] ?? '') ?></td>
                                    <td>
                                        <?php $status = $poi['status'] ?? 'pending'; ?>
                                        <span class="badge bg-<?= $status === 'approved' ? 'success' : ($status === 'rejected' ? 'danger' : 'warning') ?>">
                                            <?= $status === 'approved' ? 'Đã duyệt' : ($status === 'rejected' ? 'Từ chối' : 'Chờ duyệt') ?>
                                        </span>
                                    </td>
                                    <td><?= number_format($poi['visitCount'] ?? 0) ?></td>
                                    <td>
                                        <a href="poi-form.php?id=<?= $poi['id'] ?>" class="btn btn-sm btn-outline-primary">
                                            <i class="bi bi-pencil"></i> Sửa
                                        </a>
                                    </td>
                                </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>
