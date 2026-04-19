<?php
/**
 * Reviews / Ratings Management
 */
require_once 'config.php';
require_auth();

$user = current_user();
$role = $user['role'];
$isOwner = $role === 'restaurant_owner';

$pois = load_json($POIS_FILE);
$reviews = load_json($REVIEWS_FILE);

// Filter for owner
if ($isOwner) {
    $filteredReviews = [];
    foreach ($reviews as $r) {
        $poiFound = null;
        foreach ($pois as $p) {
            if ($p['id'] == ($r['poiId'] ?? 0)) {
                $poiFound = $p;
                break;
            }
        }
        if ($poiFound && ($poiFound['ownerId'] ?? '') === $user['id']) {
            $filteredReviews[] = $r;
        }
    }
    $reviews = $filteredReviews;
}

// Handle delete
if (isset($_GET['delete'])) {
    $deleteId = $_GET['delete'];
    $reviews = array_filter($reviews, function($r) use ($deleteId) { return $r['id'] !== $deleteId; });
    save_json($REVIEWS_FILE, array_values($reviews));
    header('Location: reviews.php');
    exit;
}

// Handle status change
if (isset($_GET['approve'])) {
    $reviewId = $_GET['approve'];
    foreach ($reviews as &$r) {
        if ($r['id'] === $reviewId) {
            $r['status'] = 'approved';
            break;
        }
    }
    save_json($REVIEWS_FILE, array_values($reviews));
    header('Location: reviews.php');
    exit;
}

// Calculate stats
$totalReviews = count($reviews);
$avgRating = $totalReviews > 0 ? round(array_sum(array_column($reviews, 'rating')) / $totalReviews, 1) : 0;
$pendingReviews = 0;
foreach ($reviews as $r) {
    if (($r['status'] ?? 'pending') === 'pending') $pendingReviews++;
}

$pageTitle = $isOwner ? 'Đánh giá nhà hàng' : 'Quản lý đánh giá';
$filter = $_GET['status'] ?? 'all';

if ($filter !== 'all') {
    $filtered = [];
    foreach ($reviews as $r) {
        if (($r['status'] ?? 'pending') === $filter) $filtered[] = $r;
    }
    $reviews = $filtered;
}

// Sort by newest
usort($reviews, function($a, $b) {
    return strtotime($b['createdAt'] ?? '0') - strtotime($a['createdAt'] ?? '0');
});
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
        .stat-card { border-radius: 12px; padding: 20px; color: white; text-align: center; }
        .stat-card.purple { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .stat-card.orange { background: linear-gradient(135deg, #fa709a 0%, #fee140 100%); }
        .stat-card.blue { background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%); }
        .stat-number { font-size: 2rem; font-weight: bold; }
        .review-card { border-left: 4px solid #667eea; }
        .review-card.pending { border-left-color: #ffc107; }
        .review-card.approved { border-left-color: #28a745; }
        .review-card.rejected { border-left-color: #dc3545; }
        .stars { color: #ffc107; }
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
                    
                    <a class="nav-link active" href="reviews.php"><i class="bi bi-star"></i> Đánh giá</a>
                    <a class="nav-link" href="statistics.php"><i class="bi bi-graph-up"></i> Thống kê</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <h4 class="mb-4">⭐ <?= $pageTitle ?></h4>
                
                <!-- Stats -->
                <div class="row g-3 mb-4">
                    <div class="col-md-4">
                        <div class="stat-card purple">
                            <div class="stat-number"><?= $totalReviews ?></div>
                            <div>Tổng đánh giá</div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="stat-card orange">
                            <div class="stat-number"><?= $avgRating ?>⭐</div>
                            <div>Điểm trung bình</div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="stat-card blue">
                            <div class="stat-number"><?= $pendingReviews ?></div>
                            <div>Chờ duyệt</div>
                        </div>
                    </div>
                </div>
                
                <!-- Filter -->
                <ul class="nav nav-pills mb-4">
                    <li class="nav-item">
                        <a class="nav-link <?= $filter=='all'?'active':'' ?>" href="?status=all">Tất cả</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link <?= $filter=='pending'?'active':'' ?>" href="?status=pending">
                            Chờ duyệt <?= $pendingReviews > 0 ? "<span class='badge bg-warning ms-1'>$pendingReviews</span>" : '' ?>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link <?= $filter=='approved'?'active':'' ?>" href="?status=approved">Đã duyệt</a>
                    </li>
                </ul>
                
                <!-- Reviews List -->
                <div class="card">
                    <div class="card-header fw-bold">
                        Danh sách đánh giá
                    </div>
                    <div class="card-body p-0">
                        <?php if (empty($reviews)): ?>
                            <div class="text-center py-5 text-muted">
                                <i class="bi bi-star fs-1"></i>
                                <p class="mt-2">Chưa có đánh giá nào</p>
                            </div>
                        <?php else: ?>
                            <?php foreach ($reviews as $review): 
                                $poi = array_filter($pois, fn($p) => $p['id'] == ($review['poiId'] ?? 0));
                                $poi = $poi ? array_values($poi)[0] : null;
                                $status = $review['status'] ?? 'pending';
                            ?>
                            <div class="p-3 border-bottom review-card <?= $status ?>">
                                <div class="d-flex justify-content-between align-items-start">
                                    <div>
                                        <h6 class="mb-1">
                                            <?= htmlspecialchars($review['userName'] ?? 'Người dùng ẩn danh') ?>
                                            <span class="text-muted">- <?= htmlspecialchars($poi['nameVi'] ?? 'N/A') ?></span>
                                        </h6>
                                        <div class="stars mb-2">
                                            <?php for($i=1; $i<=5; $i++): ?>
                                                <i class="bi bi-star<?= $i <= ($review['rating'] ?? 0) ? '-fill' : '' ?>"></i>
                                            <?php endfor; ?>
                                            <span class="ms-2 badge bg-<?= $status=='approved'?'success':($status=='rejected'?'danger':'warning') ?>">
                                                <?= $status=='approved'?'Đã duyệt':($status=='rejected'?'Từ chối':'Chờ duyệt') ?>
                                            </span>
                                        </div>
                                        <p class="mb-1"><?= htmlspecialchars($review['comment'] ?? '') ?></p>
                                        <small class="text-muted"><?= $review['createdAt'] ?? '' ?></small>
                                    </div>
                                    <div class="dropdown">
                                        <button class="btn btn-sm btn-outline-secondary" data-bs-toggle="dropdown">
                                            <i class="bi bi-three-dots-vertical"></i>
                                        </button>
                                        <ul class="dropdown-menu">
                                            <?php if ($status === 'pending'): ?>
                                            <li><a class="dropdown-item text-success" href="?approve=<?= $review['id'] ?>"><i class="bi bi-check"></i> Duyệt</a></li>
                                            <?php endif; ?>
                                            <li><a class="dropdown-item text-danger" href="?delete=<?= $review['id'] ?>" onclick="return confirm('Xóa đánh giá này?')"><i class="bi bi-trash"></i> Xóa</a></li>
                                        </ul>
                                    </div>
                                </div>
                            </div>
                            <?php endforeach; ?>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
