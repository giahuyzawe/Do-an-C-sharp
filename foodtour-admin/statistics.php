<?php
/**
 * Statistics Dashboard
 * Unified analytics and data visualization
 */
require_once 'config.php';
require_auth();

$user = current_user();
$role = $user['role'];
$isOwner = $role === 'restaurant_owner';

$pois = load_json($POIS_FILE);
$analytics = load_json($ANALYTICS_FILE);
$qrCodes = load_json($QR_CODES_FILE);

// Filter for owner
if ($isOwner) {
    $myIds = $user['restaurantIds'] ?? [];
    $pois = array_filter($pois, fn($p) => in_array($p['id'], $myIds));
}

// Calculate stats
$today = date('Y-m-d');
$thisWeek = date('Y-m-d', strtotime('-7 days'));
$thisMonth = date('Y-m-d', strtotime('-30 days'));

// Today's stats
$todayStats = [
    'dau' => count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'app_visit')),
    'views' => count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'poi_view')),
    'checkins' => count(array_filter($analytics, fn($a) => $a['date'] === $today && $a['type'] === 'check_in'))
];

// Week stats
$weekStats = [
    'dau' => count(array_filter($analytics, fn($a) => $a['date'] >= $thisWeek && $a['type'] === 'app_visit')),
    'views' => count(array_filter($analytics, fn($a) => $a['date'] >= $thisWeek && $a['type'] === 'poi_view')),
    'checkins' => count(array_filter($analytics, fn($a) => $a['date'] >= $thisWeek && $a['type'] === 'check_in'))
];

// Month stats
$monthStats = [
    'dau' => count(array_filter($analytics, fn($a) => $a['date'] >= $thisMonth && $a['type'] === 'app_visit')),
    'views' => count(array_filter($analytics, fn($a) => $a['date'] >= $thisMonth && $a['type'] === 'poi_view')),
    'checkins' => count(array_filter($analytics, fn($a) => $a['date'] >= $thisMonth && $a['type'] === 'check_in'))
];

// POI Stats
$totalPOIs = count($pois);
$approvedPOIs = count(array_filter($pois, fn($p) => ($p['status'] ?? '') === 'approved'));
$totalViews = array_sum(array_column($pois, 'visitCount'));
$totalCheckIns = array_sum(array_column($pois, 'checkInCount'));

// Top POIs
usort($pois, fn($a, $b) => ($b['visitCount'] ?? 0) <=> ($a['visitCount'] ?? 0));
$topPOIs = array_slice($pois, 0, 5);

$pageTitle = $isOwner ? 'Thống kê nhà hàng' : 'Thống kê tổng quan';
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title><?= $pageTitle ?> - Food Tour</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
    <style>
        .sidebar { min-height: 100vh; background: #1e293b; color: white; }
        .sidebar .nav-link { color: #94a3b8; padding: 12px 20px; }
        .sidebar .nav-link:hover, .sidebar .nav-link.active { color: white; background: rgba(255,255,255,0.1); }
        .stat-card { border-radius: 12px; padding: 20px; color: white; }
        .stat-card.blue { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .stat-card.green { background: linear-gradient(135deg, #43e97b 0%, #38f9d7 100%); }
        .stat-card.orange { background: linear-gradient(135deg, #fa709a 0%, #fee140 100%); }
        .stat-card.purple { background: linear-gradient(135deg, #a8edea 0%, #fed6e3 100%); color: #333; }
        .stat-number { font-size: 2rem; font-weight: bold; }
        .trend-chart { height: 200px; background: #f8f9fa; border-radius: 8px; display: flex; align-items: center; justify-content: center; }
    </style>
</head>
<body>
    <div class="container-fluid">
        <div class="row">
            <!-- Sidebar -->
            <div class="col-md-2 sidebar p-0">
                <div class="p-3 border-bottom border-secondary">
                    <h5 class="m-0">🍜 Food Tour</h5>
                </div>
                <nav class="nav flex-column">
                    <a class="nav-link" href="index.php"><i class="bi bi-speedometer2"></i> Dashboard</a>
                    
                    <?php if ($role === 'admin'): ?>
                        <a class="nav-link text-warning" href="restaurant-approval.php"><i class="bi bi-check-circle-fill"></i> <strong>Duyệt nhà hàng</strong></a>
                        <a class="nav-link" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng</a>
                        <a class="nav-link" href="users.php"><i class="bi bi-people"></i> Người dùng</a>
                    <?php else: ?>
                        <a class="nav-link" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng của tôi</a>
                        <a class="nav-link" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
                        <a class="nav-link" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
                    <?php endif; ?>
                    
                    <a class="nav-link" href="reviews.php"><i class="bi bi-star"></i> Đánh giá</a>
                    <a class="nav-link active" href="statistics.php"><i class="bi bi-graph-up"></i> Thống kê</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <h4 class="mb-4">📊 <?= $pageTitle ?></h4>
                
                <!-- Today Stats -->
                <h5 class="mb-3">Hôm nay</h5>
                <div class="row g-3 mb-4">
                    <div class="col-md-4">
                        <div class="stat-card blue">
                            <div class="stat-number"><?= $todayStats['dau'] ?></div>
                            <div><i class="bi bi-people"></i> DAU (Người dùng)</div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="stat-card green">
                            <div class="stat-number"><?= $todayStats['views'] ?></div>
                            <div><i class="bi bi-eye"></i> Lượt xem POI</div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="stat-card orange">
                            <div class="stat-number"><?= $todayStats['checkins'] ?></div>
                            <div><i class="bi bi-qr-code-scan"></i> Check-in QR</div>
                        </div>
                    </div>
                </div>
                
                <!-- 7 Days Stats -->
                <h5 class="mb-3">7 ngày qua</h5>
                <div class="row g-3 mb-4">
                    <div class="col-md-4">
                        <div class="card">
                            <div class="card-body text-center">
                                <div class="stat-number text-primary"><?= $weekStats['dau'] ?></div>
                                <div class="text-muted">DAU</div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card">
                            <div class="card-body text-center">
                                <div class="stat-number text-success"><?= $weekStats['views'] ?></div>
                                <div class="text-muted">Lượt xem</div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card">
                            <div class="card-body text-center">
                                <div class="stat-number text-warning"><?= $weekStats['checkins'] ?></div>
                                <div class="text-muted">Check-in</div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- POI Stats -->
                <h5 class="mb-3">Thống kê nhà hàng</h5>
                <div class="row g-3 mb-4">
                    <div class="col-md-3">
                        <div class="card">
                            <div class="card-body text-center">
                                <div class="stat-number text-info"><?= $totalPOIs ?></div>
                                <div class="text-muted">Tổng nhà hàng</div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card">
                            <div class="card-body text-center">
                                <div class="stat-number text-success"><?= $approvedPOIs ?></div>
                                <div class="text-muted">Đã duyệt</div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card">
                            <div class="card-body text-center">
                                <div class="stat-number text-primary"><?= number_format($totalViews) ?></div>
                                <div class="text-muted">Tổng lượt xem</div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-3">
                        <div class="card">
                            <div class="card-body text-center">
                                <div class="stat-number text-warning"><?= number_format($totalCheckIns) ?></div>
                                <div class="text-muted">Tổng check-in</div>
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- Top POIs -->
                <div class="card">
                    <div class="card-header fw-bold">
                        <i class="bi bi-trophy"></i> Top nhà hàng được xem nhiều
                    </div>
                    <div class="table-responsive">
                        <table class="table table-hover mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th>Rank</th>
                                    <th>Nhà hàng</th>
                                    <th>Lượt xem</th>
                                    <th>Check-in</th>
                                    <th>Trạng thái</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($topPOIs as $i => $poi): ?>
                                <tr>
                                    <td><span class="badge bg-<?= $i==0?'warning':($i==1?'secondary':'info') ?>">#<?= $i+1 ?></span></td>
                                    <td><?= htmlspecialchars($poi['nameVi'] ?? '') ?></td>
                                    <td><?= number_format($poi['visitCount'] ?? 0) ?></td>
                                    <td><?= number_format($poi['checkInCount'] ?? 0) ?></td>
                                    <td>
                                        <?php $status = $poi['status'] ?? 'pending'; ?>
                                        <span class="badge bg-<?= $status==='approved'?'success':($status==='rejected'?'danger':'warning') ?>">
                                            <?= $status==='approved'?'Đã duyệt':($status==='rejected'?'Từ chối':'Chờ duyệt') ?>
                                        </span>
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
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
