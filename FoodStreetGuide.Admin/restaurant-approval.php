<?php
/**
 * Trang duyệt nhà hàng (Restaurant Approval)
 * Nhà hàng phải đủ điều kiện mới được duyệt hiển thị trên app
 */

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

// Handle approval actions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $poi_id = $_POST['poi_id'] ?? null;
    $action = $_POST['action'] ?? null;
    $rejection_reason = $_POST['rejection_reason'] ?? '';
    
    if ($poi_id && $action) {
        foreach ($pois as &$poi) {
            if ($poi['id'] == $poi_id) {
                $poi['approvalStatus'] = ($action === 'approve') ? 'approved' : 'rejected';
                $poi['approvalDate'] = date('Y-m-d H:i:s');
                $poi['approvedBy'] = $_SESSION['admin']['username'] ?? 'admin';
                if ($action === 'reject') {
                    $poi['rejectionReason'] = $rejection_reason;
                }
                break;
            }
        }
        file_put_contents($storage_file, json_encode($pois, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
        
        // Log activity
        $activity = [
            'id' => uniqid(),
            'action' => $action === 'approve' ? 'approve_restaurant' : 'reject_restaurant',
            'target' => $poi_id,
            'details' => $action === 'approve' ? 'Đã duyệt nhà hàng' : 'Từ chối: ' . $rejection_reason,
            'timestamp' => date('Y-m-d H:i:s'),
            'user' => $_SESSION['admin']['username'] ?? 'admin'
        ];
        
        $activities_file = 'activities.json';
        $activities = file_exists($activities_file) ? json_decode(file_get_contents($activities_file), true) ?: [] : [];
        array_unshift($activities, $activity);
        file_put_contents($activities_file, json_encode(array_slice($activities, 0, 100), JSON_PRETTY_PRINT));
        
        header('Location: restaurant-approval.php?success=1');
        exit;
    }
}

// Filter by status
$filter_status = $_GET['status'] ?? 'pending';
$filtered_pois = array_filter($pois, function($poi) use ($filter_status) {
    $status = $poi['approvalStatus'] ?? 'pending';
    return $status === $filter_status;
});

// Count by status
$counts = [
    'pending' => count(array_filter($pois, fn($p) => ($p['approvalStatus'] ?? 'pending') === 'pending')),
    'approved' => count(array_filter($pois, fn($p) => ($p['approvalStatus'] ?? 'pending') === 'approved')),
    'rejected' => count(array_filter($pois, fn($p) => ($p['approvalStatus'] ?? 'pending') === 'rejected'))
];

?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Duyệt Nhà Hàng - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --primary: #FF6B35;
            --primary-dark: #E55A2B;
            --secondary: #2EC4B6;
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
        .stat-card {
            background: white;
            border-radius: 1rem;
            padding: 1.5rem;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
        }
        .stat-card.pending { border-left: 4px solid #F59E0B; }
        .stat-card.approved { border-left: 4px solid #10B981; }
        .stat-card.rejected { border-left: 4px solid #EF4444; }
        .restaurant-card {
            background: white;
            border-radius: 1rem;
            overflow: hidden;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            margin-bottom: 1.5rem;
        }
        .restaurant-image {
            width: 100%;
            height: 200px;
            object-fit: cover;
        }
        .status-badge {
            padding: 0.5rem 1rem;
            border-radius: 50px;
            font-size: 0.875rem;
            font-weight: 500;
        }
        .status-pending { background: #FEF3C7; color: #92400E; }
        .status-approved { background: #D1FAE5; color: #065F46; }
        .status-rejected { background: #FEE2E2; color: #991B1B; }
        .approval-criteria {
            background: #F0FDF4;
            border: 1px solid #BBF7D0;
            border-radius: 0.75rem;
            padding: 1rem;
            margin-top: 1rem;
        }
        .criteria-item {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            margin-bottom: 0.5rem;
            font-size: 0.875rem;
        }
        .criteria-item i { color: #10B981; }
    </style>
</head>
<body>
    <!-- Sidebar -->
    <div class="sidebar">
        <div class="sidebar-brand">
            <i class="bi bi-geo-alt-fill"></i>
            FoodStreetGuide
        </div>
        <nav class="nav flex-column">
            <a class="nav-link" href="index.php"><i class="bi bi-grid"></i> Dashboard</a>
            <a class="nav-link" href="pois.php"><i class="bi bi-geo"></i> Quản lý POI</a>
            <a class="nav-link active" href="restaurant-approval.php"><i class="bi bi-check-circle"></i> Duyệt Nhà Hàng</a>
            <a class="nav-link" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
            <a class="nav-link" href="reviews.php"><i class="bi bi-star-fill"></i> Reviews</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <!-- Main Content -->
    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div>
                <h2 class="mb-1"><i class="bi bi-check-circle me-2 text-primary"></i>Duyệt Nhà Hàng</h2>
                <p class="text-muted mb-0">Nhà hàng phải đủ điều kiện mới được hiển thị trên app</p>
            </div>
        </div>

        <!-- Stats Cards -->
        <div class="row g-4 mb-4">
            <div class="col-md-4">
                <a href="?status=pending" class="text-decoration-none">
                    <div class="stat-card pending">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <div class="text-muted small">Chờ duyệt</div>
                                <div class="h3 mb-0"><?php echo $counts['pending']; ?></div>
                            </div>
                            <i class="bi bi-hourglass-split text-warning fs-1"></i>
                        </div>
                    </div>
                </a>
            </div>
            <div class="col-md-4">
                <a href="?status=approved" class="text-decoration-none">
                    <div class="stat-card approved">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <div class="text-muted small">Đã duyệt</div>
                                <div class="h3 mb-0 text-success"><?php echo $counts['approved']; ?></div>
                            </div>
                            <i class="bi bi-check-circle-fill text-success fs-1"></i>
                        </div>
                    </div>
                </a>
            </div>
            <div class="col-md-4">
                <a href="?status=rejected" class="text-decoration-none">
                    <div class="stat-card rejected">
                        <div class="d-flex justify-content-between align-items-center">
                            <div>
                                <div class="text-muted small">Từ chối</div>
                                <div class="h3 mb-0 text-danger"><?php echo $counts['rejected']; ?></div>
                            </div>
                            <i class="bi bi-x-circle-fill text-danger fs-1"></i>
                        </div>
                    </div>
                </a>
            </div>
        </div>

        <!-- Filter Tabs -->
        <div class="card mb-4">
            <div class="card-body">
                <div class="d-flex gap-2">
                    <a href="?status=pending" class="btn <?php echo $filter_status === 'pending' ? 'btn-warning' : 'btn-outline-warning'; ?>">
                        <i class="bi bi-hourglass me-1"></i>Chờ duyệt (<?php echo $counts['pending']; ?>)
                    </a>
                    <a href="?status=approved" class="btn <?php echo $filter_status === 'approved' ? 'btn-success' : 'btn-outline-success'; ?>">
                        <i class="bi bi-check-circle me-1"></i>Đã duyệt (<?php echo $counts['approved']; ?>)
                    </a>
                    <a href="?status=rejected" class="btn <?php echo $filter_status === 'rejected' ? 'btn-danger' : 'btn-outline-danger'; ?>">
                        <i class="bi bi-x-circle me-1"></i>Từ chối (<?php echo $counts['rejected']; ?>)
                    </a>
                </div>
            </div>
        </div>

        <!-- Restaurant List -->
        <div class="row">
            <?php if (empty($filtered_pois)): ?>
            <div class="col-12">
                <div class="text-center py-5">
                    <i class="bi bi-inbox text-muted" style="font-size: 4rem;"></i>
                    <p class="text-muted mt-3">Không có nhà hàng nào <?php echo $filter_status === 'pending' ? 'chờ duyệt' : ($filter_status === 'approved' ? 'đã duyệt' : 'bị từ chối'); ?></p>
                </div>
            </div>
            <?php else: ?>
            <?php foreach ($filtered_pois as $poi): ?>
            <div class="col-md-6 col-lg-4">
                <div class="restaurant-card">
                    <div style="height: 200px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); position: relative;">
                        <?php if (!empty($poi['imageUrl'])): ?>
                        <img src="<?php echo htmlspecialchars($poi['imageUrl']); ?>" class="restaurant-image" alt="">
                        <?php else: ?>
                        <div class="d-flex align-items-center justify-content-center h-100">
                            <i class="bi bi-shop text-white" style="font-size: 4rem;"></i>
                        </div>
                        <?php endif; ?>
                        <div class="position-absolute top-0 end-0 m-3">
                            <span class="status-badge status-<?php echo $poi['approvalStatus'] ?? 'pending'; ?>">
                                <?php 
                                echo match($poi['approvalStatus'] ?? 'pending') {
                                    'approved' => '✓ Đã duyệt',
                                    'rejected' => '✗ Từ chối',
                                    default => '⏳ Chờ duyệt'
                                };
                                ?>
                            </span>
                        </div>
                    </div>
                    <div class="p-4">
                        <h5 class="mb-2"><?php echo htmlspecialchars($poi['nameVi'] ?? 'Không tên'); ?></h5>
                        <p class="text-muted small mb-2">
                            <i class="bi bi-geo-alt me-1"></i>
                            <?php echo htmlspecialchars($poi['address'] ?? 'Không có địa chỉ'); ?>
                        </p>
                        <p class="text-muted small mb-3">
                            <i class="bi bi-calendar me-1"></i>
                            Tạo: <?php echo date('d/m/Y', strtotime($poi['createdAt'] ?? 'now')); ?>
                        </p>
                        
                        <?php if ($filter_status === 'pending'): ?>
                        <!-- Tiêu chí đánh giá -->
                        <div class="approval-criteria">
                            <h6 class="mb-3"><i class="bi bi-clipboard-check me-1"></i>Tiêu chí đánh giá:</h6>
                            <div class="criteria-item">
                                <i class="bi bi-check-circle-fill"></i>
                                <span>Có tên nhà hàng rõ ràng</span>
                            </div>
                            <div class="criteria-item">
                                <i class="bi bi-check-circle-fill"></i>
                                <span>Có địa chỉ cụ thể</span>
                            </div>
                            <div class="criteria-item">
                                <i class="bi bi-check-circle-fill"></i>
                                <span>Có mô tả/món ăn đặc trưng</span>
                            </div>
                            <div class="criteria-item">
                                <i class="bi bi-check-circle-fill"></i>
                                <span>Có tọa độ GPS chính xác</span>
                            </div>
                        </div>

                        <!-- Action Buttons -->
                        <div class="d-flex gap-2 mt-3">
                            <form method="POST" class="d-inline">
                                <input type="hidden" name="poi_id" value="<?php echo $poi['id']; ?>">
                                <input type="hidden" name="action" value="approve">
                                <button type="submit" class="btn btn-success w-100">
                                    <i class="bi bi-check-lg me-1"></i>Duyệt
                                </button>
                            </form>
                            <button type="button" class="btn btn-outline-danger" data-bs-toggle="modal" data-bs-target="#rejectModal<?php echo $poi['id']; ?>">
                                <i class="bi bi-x-lg"></i>
                            </button>
                        </div>

                        <!-- Reject Modal -->
                        <div class="modal fade" id="rejectModal<?php echo $poi['id']; ?>" tabindex="-1">
                            <div class="modal-dialog">
                                <div class="modal-content">
                                    <div class="modal-header">
                                        <h5 class="modal-title">Từ chối nhà hàng</h5>
                                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                                    </div>
                                    <form method="POST">
                                        <div class="modal-body">
                                            <input type="hidden" name="poi_id" value="<?php echo $poi['id']; ?>">
                                            <input type="hidden" name="action" value="reject">
                                            <div class="mb-3">
                                                <label class="form-label">Lý do từ chối:</label>
                                                <textarea name="rejection_reason" class="form-control" rows="3" placeholder="Nhập lý do từ chối..." required></textarea>
                                            </div>
                                        </div>
                                        <div class="modal-footer">
                                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                                            <button type="submit" class="btn btn-danger">Xác nhận từ chối</button>
                                        </div>
                                    </form>
                                </div>
                            </div>
                        </div>
                        <?php elseif ($filter_status === 'rejected'): ?>
                        <div class="alert alert-danger mt-3">
                            <strong><i class="bi bi-x-circle me-1"></i>Lý do từ chối:</strong><br>
                            <?php echo nl2br(htmlspecialchars($poi['rejectionReason'] ?? 'Không có lý do')); ?>
                        </div>
                        <form method="POST" class="mt-2">
                            <input type="hidden" name="poi_id" value="<?php echo $poi['id']; ?>">
                            <input type="hidden" name="action" value="approve">
                            <button type="submit" class="btn btn-success w-100">
                                <i class="bi bi-arrow-counterclockwise me-1"></i>Duyệt lại
                            </button>
                        </form>
                        <?php else: ?>
                        <div class="alert alert-success mt-3">
                            <i class="bi bi-check-circle me-1"></i>
                            <strong>Đã duyệt bởi:</strong> <?php echo htmlspecialchars($poi['approvedBy'] ?? 'admin'); ?><br>
                            <small><?php echo date('d/m/Y H:i', strtotime($poi['approvalDate'] ?? 'now')); ?></small>
                        </div>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
            <?php endforeach; ?>
            <?php endif; ?>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
