<?php
require_once 'config.php';
require_admin();

$pois = load_json($POIS_FILE);

// Handle approval actions
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $poiId = $_POST['poi_id'] ?? null;
    $action = $_POST['action'] ?? null;
    $reason = $_POST['reason'] ?? '';
    
    if ($poiId && $action) {
        foreach ($pois as &$poi) {
            if ($poi['id'] == $poiId) {
                $poi['status'] = $action;
                $poi['approvedAt'] = date('Y-m-d H:i:s');
                $poi['approvedBy'] = current_user()['id'];
                if ($action === 'rejected') {
                    $poi['rejectReason'] = $reason;
                }
                break;
            }
        }
        save_json($POIS_FILE, $pois);
        $success = $action === 'approved' ? 'Đã duyệt nhà hàng!' : 'Đã từ chối nhà hàng!';
    }
}

// Filter
$filter = $_GET['filter'] ?? 'pending';
$filteredPois = array_filter($pois, fn($p) => ($p['status'] ?? 'pending') === $filter);

$counts = [
    'pending' => count(array_filter($pois, fn($p) => ($p['status'] ?? 'pending') === 'pending')),
    'approved' => count(array_filter($pois, fn($p) => ($p['status'] ?? '') === 'approved')),
    'rejected' => count(array_filter($pois, fn($p) => ($p['status'] ?? '') === 'rejected'))
];
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>Duyệt nhà hàng - Food Tour</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
    <style>
        .sidebar { min-height: 100vh; background: #1e293b; color: white; }
        .sidebar .nav-link { color: #94a3b8; padding: 12px 20px; }
        .sidebar .nav-link:hover, .sidebar .nav-link.active { color: white; background: rgba(255,255,255,0.1); }
        .status-badge { font-size: 0.85rem; padding: 6px 12px; border-radius: 20px; }
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
                    <a class="nav-link" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng</a>
                    <a class="nav-link active" href="restaurant-approval.php"><i class="bi bi-check-circle"></i> Duyệt nhà hàng</a>
                    <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <h4 class="mb-4">✅ Duyệt nhà hàng</h4>
                
                <?php if (isset($success)): ?>
                    <div class="alert alert-success"><?= $success ?></div>
                <?php endif; ?>

                <!-- Filter Tabs -->
                <ul class="nav nav-pills mb-4">
                    <li class="nav-item">
                        <a class="nav-link <?= $filter=='pending'?'active':'' ?>" href="?filter=pending">
                            Chờ duyệt <span class="badge bg-warning ms-1"><?= $counts['pending'] ?></span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link <?= $filter=='approved'?'active':'' ?>" href="?filter=approved">
                            Đã duyệt <span class="badge bg-success ms-1"><?= $counts['approved'] ?></span>
                        </a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link <?= $filter=='rejected'?'active':'' ?>" href="?filter=rejected">
                            Từ chối <span class="badge bg-danger ms-1"><?= $counts['rejected'] ?></span>
                        </a>
                    </li>
                </ul>

                <div class="card">
                    <div class="table-responsive">
                        <table class="table table-hover mb-0">
                            <thead class="table-light">
                                <tr>
                                    <th>Nhà hàng</th>
                                    <th>Địa chỉ</th>
                                    <th>Ngày tạo</th>
                                    <th>Trạng thái</th>
                                    <th>Ghi chú / Lý do</th>
                                    <th>Thao tác</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($filteredPois as $poi): ?>
                                <tr>
                                    <td>
                                        <strong><?= htmlspecialchars($poi['nameVi'] ?? $poi['name'] ?? 'N/A') ?></strong>
                                    </td>
                                    <td><?= htmlspecialchars($poi['address'] ?? '') ?></td>
                                    <td><?= $poi['createdAt'] ?? 'N/A' ?></td>
                                    <td>
                                        <?php $status = $poi['status'] ?? 'pending'; ?>
                                        <span class="badge bg-<?= $status==='approved'?'success':($status==='rejected'?'danger':'warning') ?>">
                                            <?= $status==='approved'?'Đã duyệt':($status==='rejected'?'Từ chối':'Chờ duyệt') ?>
                                        </span>
                                        <?php if ($poi['approvedAt'] ?? false): ?>
                                            <br><small class="text-muted"><?= date('d/m H:i', strtotime($poi['approvedAt'])) ?></small>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <?php if ($status === 'rejected' && !empty($poi['rejectReason'])): ?>
                                            <span class="text-danger"><i class="bi bi-exclamation-triangle"></i> <?= htmlspecialchars($poi['rejectReason']) ?></span>
                                        <?php elseif ($poi['note'] ?? false): ?>
                                            <small class="text-muted"><?= htmlspecialchars($poi['note']) ?></small>
                                        <?php else: ?>
                                            <span class="text-muted">-</span>
                                        <?php endif; ?>
                                    </td>
                                    <td>
                                        <?php if ($status === 'pending'): ?>
                                            <form method="POST" class="d-inline">
                                                <input type="hidden" name="poi_id" value="<?= $poi['id'] ?>">
                                                <input type="hidden" name="action" value="approved">
                                                <button type="submit" class="btn btn-sm btn-success">
                                                    <i class="bi bi-check-lg"></i> Duyệt
                                                </button>
                                            </form>
                                            <button type="button" class="btn btn-sm btn-danger" data-bs-toggle="modal" data-bs-target="#rejectModal<?= $poi['id'] ?>">
                                                <i class="bi bi-x-lg"></i> Từ chối
                                            </button>
                                            
                                            <!-- Reject Modal -->
                                            <div class="modal fade" id="rejectModal<?= $poi['id'] ?>" tabindex="-1">
                                                <div class="modal-dialog">
                                                    <div class="modal-content">
                                                        <div class="modal-header">
                                                            <h5 class="modal-title">Từ chối nhà hàng</h5>
                                                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                                                        </div>
                                                        <form method="POST">
                                                            <div class="modal-body">
                                                                <input type="hidden" name="poi_id" value="<?= $poi['id'] ?>">
                                                                <input type="hidden" name="action" value="rejected">
                                                                <div class="mb-3">
                                                                    <label class="form-label">Lý do từ chối</label>
                                                                    <textarea name="reason" class="form-control" required placeholder="Vui lòng nhập lý do từ chối..."></textarea>
                                                                </div>
                                                            </div>
                                                            <div class="modal-footer">
                                                                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                                                                <button type="submit" class="btn btn-danger">Từ chối</button>
                                                            </div>
                                                        </form>
                                                    </div>
                                                </div>
                                            </div>
                                        <?php else: ?>
                                            <a href="poi-form.php?id=<?= $poi['id'] ?>" class="btn btn-sm btn-outline-primary">
                                                <i class="bi bi-eye"></i> Xem
                                            </a>
                                        <?php endif; ?>
                                    </td>
                                </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                    
                    <?php if (empty($filteredPois)): ?>
                        <div class="text-center py-5 text-muted">
                            <i class="bi bi-check-circle fs-1 text-success"></i>
                            <p class="mt-2">Không có nhà hàng nào <?= $filter==='pending'?'chờ duyệt':'trong mục này' ?></p>
                        </div>
                    <?php endif; ?>
                </div>
            </div>
        </div>
    </div>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
