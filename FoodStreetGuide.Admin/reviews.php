<?php
session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

// Sample review data structure (in production this comes from database)
$reviewsFile = 'reviews.json';
$reviews = [];
if (file_exists($reviewsFile)) {
    $reviews = json_decode(file_get_contents($reviewsFile), true) ?: [];
}

// Generate sample data if empty
if (empty($reviews)) {
    $reviews = [
        [
            'id' => 1,
            'poi_id' => 1,
            'poi_name' => 'Chợ Bến Thành',
            'user_id' => 'user_001',
            'user_name' => 'Nguyễn Văn A',
            'user_avatar' => '',
            'rating' => 5,
            'comment' => 'Địa điểm rất đẹp, nhiều món ăn ngon! Chợ sầm uất và đa dạng hàng hóa. Bánh mì ở đây tuyệt vời!',
            'images' => [],
            'status' => 'pending',
            'is_spam' => false,
            'spam_reports' => 0,
            'helpful_count' => 12,
            'created_at' => '2025-01-15 10:30:00',
            'reported_by' => []
        ],
        [
            'id' => 2,
            'poi_id' => 2,
            'poi_name' => 'Phố đi bộ Nguyễn Huệ',
            'user_id' => 'user_002',
            'user_name' => 'Trần Thị B',
            'user_avatar' => '',
            'rating' => 4,
            'comment' => 'Không gian thoáng đãng, thích hợp đi dạo buổi tối. Có nhiều quán cà đẹp.',
            'images' => [],
            'status' => 'approved',
            'is_spam' => false,
            'spam_reports' => 0,
            'helpful_count' => 8,
            'created_at' => '2025-01-14 15:20:00',
            'reported_by' => []
        ],
        [
            'id' => 3,
            'poi_id' => 3,
            'poi_name' => 'Bitexco Financial Tower',
            'user_id' => 'user_003',
            'user_name' => 'Spam Bot',
            'user_avatar' => '',
            'rating' => 1,
            'comment' => 'CLICK HERE!!! WIN IPHONE 15 PRO MAX FREE!!! www.scam-site.com',
            'images' => [],
            'status' => 'pending',
            'is_spam' => true,
            'spam_reports' => 5,
            'helpful_count' => 0,
            'created_at' => '2025-01-16 09:00:00',
            'reported_by' => ['admin', 'user_001', 'user_002']
        ],
        [
            'id' => 4,
            'poi_id' => 1,
            'poi_name' => 'Chợ Bến Thành',
            'user_id' => 'user_004',
            'user_name' => 'Lê Văn C',
            'user_avatar' => '',
            'rating' => 3,
            'comment' => 'Giá hơi đắt so với chợ khác, nhưng đồ ăn ngon. Nên thử bánh mì ở đây.',
            'images' => [],
            'status' => 'approved',
            'is_spam' => false,
            'spam_reports' => 1,
            'helpful_count' => 5,
            'created_at' => '2025-01-13 18:45:00',
            'reported_by' => ['user_005']
        ],
        [
            'id' => 5,
            'poi_id' => 4,
            'poi_name' => 'Cà phê cung điện',
            'user_id' => 'user_005',
            'user_name' => 'Phạm Thị D',
            'user_avatar' => '',
            'rating' => 5,
            'comment' => 'View đẹp, cà phê ngon. Điểm sống ảo tuyệt vời!',
            'images' => [],
            'status' => 'approved',
            'is_spam' => false,
            'spam_reports' => 0,
            'helpful_count' => 25,
            'created_at' => '2025-01-12 14:00:00',
            'reported_by' => []
        ]
    ];
    file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT));
}

// Handle actions
$success = '';
$error = '';

// Approve review
if (isset($_GET['approve'])) {
    $id = intval($_GET['approve']);
    foreach ($reviews as &$review) {
        if ($review['id'] == $id) {
            $review['status'] = 'approved';
            break;
        }
    }
    file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT));
    $success = 'Đã duyệt đánh giá #' . $id;
}

// Reject review
if (isset($_GET['reject'])) {
    $id = intval($_GET['reject']);
    foreach ($reviews as &$review) {
        if ($review['id'] == $id) {
            $review['status'] = 'rejected';
            break;
        }
    }
    file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT));
    $success = 'Đã từ chối đánh giá #' . $id;
}

// Report as spam
if (isset($_GET['spam'])) {
    $id = intval($_GET['spam']);
    foreach ($reviews as &$review) {
        if ($review['id'] == $id) {
            $review['is_spam'] = true;
            $review['spam_reports']++;
            $review['reported_by'][] = $_SESSION['admin'] ?? 'admin';
            $review['status'] = 'rejected';
            break;
        }
    }
    file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT));
    $success = 'Đã đánh dấu spam đánh giá #' . $id;
}

// Mark as not spam
if (isset($_GET['not_spam'])) {
    $id = intval($_GET['not_spam']);
    foreach ($reviews as &$review) {
        if ($review['id'] == $id) {
            $review['is_spam'] = false;
            break;
        }
    }
    file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT));
    $success = 'Đã gỡ đánh dấu spam đánh giá #' . $id;
}

// Delete review
if (isset($_GET['delete'])) {
    $id = intval($_GET['delete']);
    $reviews = array_filter($reviews, fn($r) => $r['id'] != $id);
    file_put_contents($reviewsFile, json_encode(array_values($reviews), JSON_PRETTY_PRINT));
    $success = 'Đã xóa đánh giá #' . $id;
}

// Bulk actions
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['bulk_action'])) {
    $action = $_POST['bulk_action'];
    $selected = $_POST['selected'] ?? [];
    
    foreach ($reviews as &$review) {
        if (in_array($review['id'], $selected)) {
            switch ($action) {
                case 'approve':
                    $review['status'] = 'approved';
                    break;
                case 'reject':
                    $review['status'] = 'rejected';
                    break;
                case 'spam':
                    $review['is_spam'] = true;
                    $review['spam_reports']++;
                    $review['status'] = 'rejected';
                    break;
                case 'delete':
                    // Will filter later
                    break;
            }
        }
    }
    
    if ($action === 'delete') {
        $reviews = array_filter($reviews, fn($r) => !in_array($r['id'], $selected));
        $reviews = array_values($reviews);
    }
    
    file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT));
    $success = 'Đã thực hiện thao tác trên ' . count($selected) . ' đánh giá';
}

// Filter & Search
$filter_status = $_GET['status'] ?? 'all';
$filter_rating = $_GET['rating'] ?? 'all';
$filter_spam = $_GET['spam'] ?? 'all';
$search = $_GET['search'] ?? '';

$filteredReviews = $reviews;

if ($filter_status !== 'all') {
    $filteredReviews = array_filter($filteredReviews, fn($r) => $r['status'] === $filter_status);
}

if ($filter_rating !== 'all') {
    $filteredReviews = array_filter($filteredReviews, fn($r) => $r['rating'] == $filter_rating);
}

if ($filter_spam !== 'all') {
    $isSpam = $filter_spam === 'yes';
    $filteredReviews = array_filter($filteredReviews, fn($r) => $r['is_spam'] === $isSpam);
}

if ($search) {
    $filteredReviews = array_filter($filteredReviews, fn($r) => 
        stripos($r['comment'], $search) !== false || 
        stripos($r['user_name'], $search) !== false ||
        stripos($r['poi_name'], $search) !== false
    );
}

// Sort by created_at desc
usort($filteredReviews, fn($a, $b) => strtotime($b['created_at']) <=> strtotime($a['created_at']));

// Stats
$stats = [
    'total' => count($reviews),
    'pending' => count(array_filter($reviews, fn($r) => $r['status'] === 'pending')),
    'approved' => count(array_filter($reviews, fn($r) => $r['status'] === 'approved')),
    'spam' => count(array_filter($reviews, fn($r) => $r['is_spam'])),
    'avg_rating' => count($reviews) > 0 ? round(array_sum(array_column($reviews, 'rating')) / count($reviews), 1) : 0
];
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>User Reviews - Food Street Guide Admin</title>
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
        .stats-card {
            background: white;
            border-radius: 1rem;
            padding: 1.5rem;
            text-align: center;
        }
        .stats-number {
            font-size: 2rem;
            font-weight: 700;
            color: var(--primary);
        }
        .review-card {
            background: white;
            border-radius: 1rem;
            padding: 1.5rem;
            margin-bottom: 1rem;
            border-left: 4px solid #E2E8F0;
            transition: all 0.2s;
        }
        .review-card:hover {
            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        }
        .review-card.pending { border-left-color: #F59E0B; }
        .review-card.approved { border-left-color: #10B981; }
        .review-card.rejected { border-left-color: #EF4444; }
        .review-card.spam { border-left-color: #DC2626; background: #FEF2F2; }
        .avatar-circle {
            width: 48px;
            height: 48px;
            border-radius: 50%;
            background: linear-gradient(135deg, var(--primary), var(--primary-dark));
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: 600;
            font-size: 1.2rem;
        }
        .star-rating {
            color: #FBBF24;
        }
        .spam-badge {
            background: #DC2626;
            color: white;
            font-size: 0.75rem;
            padding: 0.25rem 0.5rem;
            border-radius: 0.375rem;
        }
        .status-badge {
            font-size: 0.75rem;
            padding: 0.25rem 0.75rem;
            border-radius: 9999px;
        }
        .status-pending { background: #FEF3C7; color: #D97706; }
        .status-approved { background: #D1FAE5; color: #059669; }
        .status-rejected { background: #FEE2E2; color: #DC2626; }
        .action-btn {
            padding: 0.5rem 1rem;
            border-radius: 0.5rem;
            font-size: 0.875rem;
            border: none;
            cursor: pointer;
            transition: all 0.2s;
        }
        .btn-approve { background: #D1FAE5; color: #059669; }
        .btn-approve:hover { background: #059669; color: white; }
        .btn-reject { background: #FEE2E2; color: #DC2626; }
        .btn-reject:hover { background: #DC2626; color: white; }
        .btn-spam { background: #FEF2F2; color: #991B1B; border: 1px solid #FECACA; }
        .btn-spam:hover { background: #DC2626; color: white; }
        .btn-delete { background: #F3F4F6; color: #6B7280; }
        .btn-delete:hover { background: #EF4444; color: white; }
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
            <a class="nav-link active" href="reviews.php"><i class="bi bi-star-fill"></i> Reviews</a>
            <a class="nav-link" href="permissions.php"><i class="bi bi-shield-lock"></i> Phân quyền</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div>
                <h1 class="h3 mb-1">User Reviews</h1>
                <p class="text-muted mb-0">Quản lý đánh giá và bình luận từ người dùng</p>
            </div>
        </div>

        <?php if ($success): ?>
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            <i class="bi bi-check-circle me-2"></i><?php echo $success; ?>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
        <?php endif; ?>

        <?php if ($error): ?>
        <div class="alert alert-danger alert-dismissible fade show" role="alert">
            <i class="bi bi-exclamation-circle me-2"></i><?php echo $error; ?>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
        <?php endif; ?>

        <!-- Stats -->
        <div class="row g-4 mb-4">
            <div class="col-md-2">
                <div class="stats-card">
                    <div class="stats-number"><?php echo $stats['total']; ?></div>
                    <small class="text-muted">Tổng đánh giá</small>
                </div>
            </div>
            <div class="col-md-2">
                <div class="stats-card">
                    <div class="stats-number text-warning"><?php echo $stats['pending']; ?></div>
                    <small class="text-muted">Chờ duyệt</small>
                </div>
            </div>
            <div class="col-md-2">
                <div class="stats-card">
                    <div class="stats-number text-success"><?php echo $stats['approved']; ?></div>
                    <small class="text-muted">Đã duyệt</small>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stats-card">
                    <div class="stats-number text-primary"><?php echo $stats['avg_rating']; ?> <small>/5</small></div>
                    <div class="star-rating">
                        <?php for ($i = 1; $i <= 5; $i++): ?>
                        <i class="bi bi-star<?php echo $i <= round($stats['avg_rating']) ? '-fill' : ''; ?>" style="font-size: 0.9rem;"></i>
                        <?php endfor; ?>
                    </div>
                    <small class="text-muted">Đánh giá trung bình</small>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stats-card">
                    <div class="stats-number text-danger"><?php echo $stats['spam']; ?></div>
                    <small class="text-muted">Spam detected</small>
                </div>
            </div>
        </div>

        <!-- Filters -->
        <div class="card mb-4">
            <div class="card-body">
                <form method="GET" class="row g-3">
                    <div class="col-md-4">
                        <div class="input-group">
                            <span class="input-group-text bg-white border-end-0">
                                <i class="bi bi-search text-muted"></i>
                            </span>
                            <input type="text" class="form-control border-start-0" name="search" 
                                   placeholder="Tìm kiếm review, user, POI..." value="<?php echo htmlspecialchars($search); ?>">
                        </div>
                    </div>
                    <div class="col-md-2">
                        <select class="form-select" name="status">
                            <option value="all">Tất cả trạng thái</option>
                            <option value="pending" <?php echo $filter_status === 'pending' ? 'selected' : ''; ?>>Chờ duyệt</option>
                            <option value="approved" <?php echo $filter_status === 'approved' ? 'selected' : ''; ?>>Đã duyệt</option>
                            <option value="rejected" <?php echo $filter_status === 'rejected' ? 'selected' : ''; ?>>Từ chối</option>
                        </select>
                    </div>
                    <div class="col-md-2">
                        <select class="form-select" name="rating">
                            <option value="all">Tất cả rating</option>
                            <option value="5" <?php echo $filter_rating === '5' ? 'selected' : ''; ?>>5 sao</option>
                            <option value="4" <?php echo $filter_rating === '4' ? 'selected' : ''; ?>>4 sao</option>
                            <option value="3" <?php echo $filter_rating === '3' ? 'selected' : ''; ?>>3 sao</option>
                            <option value="2" <?php echo $filter_rating === '2' ? 'selected' : ''; ?>>2 sao</option>
                            <option value="1" <?php echo $filter_rating === '1' ? 'selected' : ''; ?>>1 sao</option>
                        </select>
                    </div>
                    <div class="col-md-2">
                        <select class="form-select" name="spam">
                            <option value="all">Tất cả</option>
                            <option value="yes" <?php echo $filter_spam === 'yes' ? 'selected' : ''; ?>>Spam</option>
                            <option value="no" <?php echo $filter_spam === 'no' ? 'selected' : ''; ?>>Không spam</option>
                        </select>
                    </div>
                    <div class="col-md-2">
                        <button type="submit" class="btn btn-outline-primary w-100">Lọc</button>
                    </div>
                </form>
            </div>
        </div>

        <!-- Bulk Actions -->
        <form method="POST" id="bulkForm">
            <div class="card mb-4">
                <div class="card-body d-flex justify-content-between align-items-center">
                    <div class="d-flex align-items-center gap-3">
                        <select name="bulk_action" class="form-select" style="width: auto;" required>
                            <option value="">-- Chọn thao tác --</option>
                            <option value="approve">Duyệt đã chọn</option>
                            <option value="reject">Từ chối đã chọn</option>
                            <option value="spam">Đánh dấu spam</option>
                            <option value="delete">Xóa đã chọn</option>
                        </select>
                        <button type="submit" class="btn btn-primary" onclick="return confirm('Bạn chắc chắn muốn thực hiện thao tác này?')">
                            <i class="bi bi-check-lg me-1"></i> Thực hiện
                        </button>
                    </div>
                    <div class="text-muted">
                        <small>Tổng: <?php echo count($filteredReviews); ?> đánh giá</small>
                    </div>
                </div>
            </div>

            <!-- Reviews List -->
            <div class="reviews-container">
                <?php if (empty($filteredReviews)): ?>
                <div class="card">
                    <div class="card-body text-center py-5">
                        <i class="bi bi-inbox fs-1 text-muted"></i>
                        <p class="text-muted mt-3">Không tìm thấy đánh giá nào</p>
                    </div>
                </div>
                <?php else: ?>
                <?php foreach ($filteredReviews as $review): ?>
                <div class="review-card <?php echo $review['status']; ?> <?php echo $review['is_spam'] ? 'spam' : ''; ?>">
                    <div class="d-flex justify-content-between align-items-start mb-3">
                        <div class="d-flex gap-3">
                            <input type="checkbox" name="selected[]" value="<?php echo $review['id']; ?>" class="form-check-input mt-2">
                            <div class="avatar-circle">
                                <?php echo strtoupper(substr($review['user_name'], 0, 1)); ?>
                            </div>
                            <div>
                                <div class="d-flex align-items-center gap-2">
                                    <span class="fw-semibold"><?php echo htmlspecialchars($review['user_name']); ?></span>
                                    <?php if ($review['is_spam']): ?>
                                    <span class="spam-badge"><i class="bi bi-exclamation-triangle me-1"></i>SPAM</span>
                                    <?php endif; ?>
                                    <span class="status-badge status-<?php echo $review['status']; ?>">
                                        <?php echo $review['status'] === 'pending' ? 'Chờ duyệt' : ($review['status'] === 'approved' ? 'Đã duyệt' : 'Từ chối'); ?>
                                    </span>
                                </div>
                                <div class="text-muted small">
                                    <i class="bi bi-geo-alt me-1"></i><?php echo htmlspecialchars($review['poi_name']); ?> • 
                                    <i class="bi bi-clock me-1"></i><?php echo date('d/m/Y H:i', strtotime($review['created_at'])); ?>
                                </div>
                            </div>
                        </div>
                        <div class="star-rating">
                            <?php for ($i = 1; $i <= 5; $i++): ?>
                            <i class="bi bi-star<?php echo $i <= $review['rating'] ? '-fill' : ''; ?>"></i>
                            <?php endfor; ?>
                            <span class="ms-1 text-muted">(<?php echo $review['rating']; ?>/5)</span>
                        </div>
                    </div>

                    <div class="mb-3">
                        <p class="mb-0 <?php echo $review['is_spam'] ? 'text-danger' : ''; ?>">
                            <?php echo nl2br(htmlspecialchars($review['comment'])); ?>
                        </p>
                    </div>

                    <div class="d-flex justify-content-between align-items-center mt-3 pt-3 border-top">
                        <div class="d-flex gap-3 text-muted small">
                            <span><i class="bi bi-hand-thumbs-up me-1"></i><?php echo $review['helpful_count']; ?> helpful</span>
                            <?php if ($review['spam_reports'] > 0): ?>
                            <span class="text-danger"><i class="bi bi-flag me-1"></i><?php echo $review['spam_reports']; ?> spam reports</span>
                            <?php endif; ?>
                        </div>
                        <div class="d-flex gap-2">
                            <?php if ($review['status'] === 'pending'): ?>
                            <a href="?approve=<?php echo $review['id']; ?>" class="action-btn btn-approve" onclick="return confirm('Duyệt review này?')">
                                <i class="bi bi-check-lg me-1"></i>Duyệt
                            </a>
                            <a href="?reject=<?php echo $review['id']; ?>" class="action-btn btn-reject" onclick="return confirm('Từ chối review này?')">
                                <i class="bi bi-x-lg me-1"></i>Từ chối
                            </a>
                            <?php endif; ?>
                            
                            <?php if (!$review['is_spam']): ?>
                            <a href="?spam=<?php echo $review['id']; ?>" class="action-btn btn-spam" onclick="return confirm('Đánh dấu spam và từ chối review này?')">
                                <i class="bi bi-exclamation-triangle me-1"></i>Spam
                            </a>
                            <?php else: ?>
                            <a href="?not_spam=<?php echo $review['id']; ?>" class="action-btn btn-approve">
                                <i class="bi bi-check-circle me-1"></i>Không spam
                            </a>
                            <?php endif; ?>
                            
                            <a href="?delete=<?php echo $review['id']; ?>" class="action-btn btn-delete" onclick="return confirm('Xóa vĩnh viễn review này?')">
                                <i class="bi bi-trash"></i>
                            </a>
                        </div>
                    </div>
                </div>
                <?php endforeach; ?>
                <?php endif; ?>
            </div>
        </form>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        // Select all checkbox
        document.querySelectorAll('input[name="selected[]"]').forEach(cb => {
            cb.addEventListener('change', function() {
                const anyChecked = document.querySelectorAll('input[name="selected[]"]:checked').length > 0;
                document.querySelector('select[name="bulk_action"]').disabled = !anyChecked;
            });
        });
    </script>
</body>
</html>
