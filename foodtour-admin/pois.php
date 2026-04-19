<?php
require_once 'config.php';
require_auth();

$user = current_user();
$role = $user['role'];
$isOwner = $role === 'restaurant_owner';

// Load POIs
$pois = load_json($POIS_FILE);

// Filter for restaurant owner
if ($isOwner) {
    $myIds = $user['restaurantIds'] ?? [];
    $pois = array_filter($pois, fn($p) => in_array($p['id'], $myIds));
}

// Handle delete
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['delete_id'])) {
    $deleteId = (int)$_POST['delete_id'];
    $canDelete = $isOwner ? in_array($deleteId, $user['restaurantIds'] ?? []) : true;
    
    if ($canDelete) {
        $pois = array_filter($pois, fn($p) => $p['id'] !== $deleteId);
        save_json($POIS_FILE, array_values($pois));
        $success = "Đã xóa nhà hàng!";
    }
}

// Search
$search = $_GET['search'] ?? '';
$filterStatus = $_GET['status'] ?? '';

if ($search) {
    $pois = array_filter($pois, fn($p) => 
        stripos($p['nameVi'] ?? '', $search) !== false ||
        stripos($p['name'] ?? '', $search) !== false ||
        stripos($p['address'] ?? '', $search) !== false
    );
}

if ($filterStatus) {
    $pois = array_filter($pois, fn($p) => ($p['status'] ?? 'pending') === $filterStatus);
}

$pageTitle = $isOwner ? 'Nhà hàng của tôi' : 'Quản lý nhà hàng';
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
                        <a class="nav-link active" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng</a>
                        <a class="nav-link" href="users.php"><i class="bi bi-people"></i> Người dùng</a>
                    <?php else: ?>
                        <a class="nav-link active" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng của tôi</a>
                        <a class="nav-link" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
                        <a class="nav-link" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
                    <?php endif; ?>
                    
                    <a class="nav-link" href="reviews.php"><i class="bi bi-star"></i> Đánh giá</a>
                    <a class="nav-link" href="statistics.php"><i class="bi bi-graph-up"></i> Thống kê</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <div class="d-flex justify-content-between align-items-center mb-4">
                    <h4><?= $pageTitle ?></h4>
                    <div class="d-flex gap-2">
                        <form class="d-flex gap-2" method="GET">
                            <input type="text" name="search" class="form-control" placeholder="Tìm kiếm..." value="<?= htmlspecialchars($search) ?>">
                            <?php if (!$isOwner): // Admin sees status filter ?>
                                <select name="status" class="form-select" style="width: 150px;" onchange="this.form.submit()">
                                    <option value="">Tất cả</option>
                                    <option value="approved" <?= $filterStatus=='approved'?'selected':'' ?>>Đã duyệt</option>
                                    <option value="pending" <?= $filterStatus=='pending'?'selected':'' ?>>Chờ duyệt</option>
                                    <option value="rejected" <?= $filterStatus=='rejected'?'selected':'' ?>>Từ chối</option>
                                </select>
                            <?php endif; ?>
                            <button class="btn btn-outline-primary"><i class="bi bi-search"></i></button>
                        </form>
                        <?php if ($isOwner): // Only restaurant owner can add ?>
                            <a href="poi-form.php" class="btn btn-primary"><i class="bi bi-plus-lg"></i> Thêm nhà hàng</a>
                        <?php endif; ?>
                    </div>
                </div>

                <?php if (isset($success)): ?>
                    <div class="alert alert-success"><?= $success ?></div>
                <?php endif; ?>

                <div class="card">
                    <div class="table-responsive">
                        <table class="table table-hover mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th>ID</th>
                                    <th>Tên nhà hàng</th>
                                    <th>Địa chỉ</th>
                                    <th>Trạng thái</th>
                                    <th>Lượt xem</th>
                                    <th>Thao tác</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($pois as $poi): ?>
                                <tr>
                                    <td>#<?= $poi['id'] ?></td>
                                    <td>
                                        <strong><?= htmlspecialchars($poi['nameVi'] ?? $poi['name'] ?? 'N/A') ?></strong>
                                        <?php if (!empty($poi['image'])): ?>
                                            <br><small class="text-muted">🖼️ Có ảnh</small>
                                        <?php endif; ?>
                                    </td>
                                    <td><?= htmlspecialchars($poi['address'] ?? 'Chưa có') ?></td>
                                    <td>
                                        <?php $status = $poi['status'] ?? 'pending'; ?>
                                        <span class="badge bg-<?= $status === 'approved' ? 'success' : ($status === 'rejected' ? 'danger' : 'warning') ?>">
                                            <?= $status === 'approved' ? 'Đã duyệt' : ($status === 'rejected' ? 'Từ chối' : 'Chờ duyệt') ?>
                                        </span>
                                    </td>
                                    <td><?= number_format($poi['visitCount'] ?? 0) ?></td>
                                    <td>
                                        <div class="btn-group btn-group-sm">
                                            <a href="poi-form.php?id=<?= $poi['id'] ?>" class="btn btn-outline-primary" title="Sửa">
                                                <i class="bi bi-pencil"></i>
                                            </a>
                                            <a href="analytics.php?poi_id=<?= $poi['id'] ?>" class="btn btn-outline-success" title="Thống kê">
                                                <i class="bi bi-graph-up"></i>
                                            </a>
                                            <?php if (!$isOwner): ?>
                                                <form method="POST" class="d-inline" onsubmit="return confirm('Xóa nhà hàng này?')">
                                                    <input type="hidden" name="delete_id" value="<?= $poi['id'] ?>">
                                                    <button type="submit" class="btn btn-outline-danger" title="Xóa">
                                                        <i class="bi bi-trash"></i>
                                                    </button>
                                                </form>
                                            <?php endif; ?>
                                        </div>
                                    </td>
                                </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                    <?php if (empty($pois)): ?>
                        <div class="text-center py-5 text-muted">
                            <i class="bi bi-shop fs-1"></i>
                            <p class="mt-2">Chưa có nhà hàng nào</p>
                        </div>
                    <?php endif; ?>
                </div>
            </div>
        </div>
    </div>
</body>
</html>
