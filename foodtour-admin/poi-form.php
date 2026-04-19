<?php
require_once 'config.php';
require_auth();

$user = current_user();
$role = $user['role'];
$isOwner = $role === 'restaurant_owner';

$id = $_GET['id'] ?? null;
$pois = load_json($POIS_FILE);
$poi = null;

// Admin CAN edit but CANNOT create new
// Owner CAN create new and edit their own
if ($id) {
    // EDIT mode - check edit permission
    foreach ($pois as $p) {
        if ($p['id'] == $id) {
            $poi = $p;
            break;
        }
    }
    
    // Owner can only edit their restaurants
    if ($isOwner && $poi && !in_array($poi['id'], $user['restaurantIds'] ?? [])) {
        header('Location: pois.php');
        exit;
    }
    // Admin can edit any
} else {
    // CREATE mode - only owner can add
    if (!$isOwner) {
        header('Location: pois.php');
        exit;
    }
}

// Handle form submit
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $data = [
        'id' => $id ? (int)$id : (count($pois) > 0 ? max(array_column($pois, 'id')) + 1 : 1),
        'name' => $_POST['nameEn'] ?? '',
        'nameVi' => $_POST['nameVi'] ?? '',
        'nameEn' => $_POST['nameEn'] ?? '',
        'address' => $_POST['address'] ?? '',
        'description' => $_POST['description'] ?? '',
        'descriptionVi' => $_POST['descriptionVi'] ?? '',
        'descriptionEn' => $_POST['descriptionEn'] ?? '',
        'phone' => $_POST['phone'] ?? '',
        'openingHours' => $_POST['openingHours'] ?? '',
        'latitude' => (float)($_POST['latitude'] ?? 0),
        'longitude' => (float)($_POST['longitude'] ?? 0),
        'status' => $poi['status'] ?? 'pending', // Keep existing status, or default to pending for new
        'visitCount' => $poi['visitCount'] ?? 0,
        'updatedAt' => date('Y-m-d H:i:s')
    ];
    
    // Owner assignment
    if (!$id && $isOwner) {
        $data['ownerId'] = $user['id'];
        // Add to user's restaurant list
        $users = load_json($USERS_FILE);
        foreach ($users as &$u) {
            if ($u['id'] === $user['id']) {
                $u['restaurantIds'][] = $data['id'];
                break;
            }
        }
        save_json($USERS_FILE, $users);
    } elseif ($poi && isset($poi['ownerId'])) {
        $data['ownerId'] = $poi['ownerId'];
    }
    
    // Update or add
    $found = false;
    foreach ($pois as &$p) {
        if ($p['id'] == $data['id']) {
            $p = array_merge($p, $data);
            $found = true;
            break;
        }
    }
    
    if (!$found) {
        $data['createdAt'] = date('Y-m-d H:i:s');
        $pois[] = $data;
    }
    
    save_json($POIS_FILE, $pois);
    
    header('Location: pois.php');
    exit;
}

$pageTitle = $id ? 'Sửa nhà hàng' : 'Thêm nhà hàng';
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
                    <a class="nav-link active" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng</a>
                    <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <div class="d-flex justify-content-between align-items-center mb-4">
                    <h4><?= $pageTitle ?></h4>
                    <a href="pois.php" class="btn btn-outline-secondary"><i class="bi bi-arrow-left"></i> Quay lại</a>
                </div>

                <div class="card">
                    <div class="card-body">
                        <form method="POST">
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Tên nhà hàng (Tiếng Việt) *</label>
                                    <input type="text" name="nameVi" class="form-control" required 
                                           value="<?= htmlspecialchars($poi['nameVi'] ?? '') ?>">
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Tên nhà hàng (English)</label>
                                    <input type="text" name="nameEn" class="form-control" 
                                           value="<?= htmlspecialchars($poi['nameEn'] ?? '') ?>">
                                </div>
                            </div>
                            
                            <div class="mb-3">
                                <label class="form-label">Địa chỉ *</label>
                                <input type="text" name="address" class="form-control" required 
                                       value="<?= htmlspecialchars($poi['address'] ?? '') ?>">
                            </div>
                            
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Số điện thoại</label>
                                    <input type="text" name="phone" class="form-control" 
                                           value="<?= htmlspecialchars($poi['phone'] ?? '') ?>">
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Giờ mở cửa</label>
                                    <input type="text" name="openingHours" class="form-control" placeholder="VD: 06:00 - 22:00"
                                           value="<?= htmlspecialchars($poi['openingHours'] ?? '') ?>">
                                </div>
                            </div>
                            
                            <div class="mb-3">
                                <label class="form-label">Mô tả (Tiếng Việt)</label>
                                <textarea name="descriptionVi" class="form-control" rows="3"><?= htmlspecialchars($poi['descriptionVi'] ?? '') ?></textarea>
                            </div>
                            
                            <div class="mb-3">
                                <label class="form-label">Mô tả (English)</label>
                                <textarea name="descriptionEn" class="form-control" rows="3"><?= htmlspecialchars($poi['descriptionEn'] ?? '') ?></textarea>
                            </div>
                            
                            <div class="row">
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Vĩ độ (Latitude)</label>
                                    <input type="number" step="any" name="latitude" class="form-control" 
                                           value="<?= $poi['latitude'] ?? '' ?>">
                                </div>
                                <div class="col-md-6 mb-3">
                                    <label class="form-label">Kinh độ (Longitude)</label>
                                    <input type="number" step="any" name="longitude" class="form-control" 
                                           value="<?= $poi['longitude'] ?? '' ?>">
                                </div>
                            </div>
                            
                            <!-- Status: Always PENDING for new restaurants, owners cannot change -->
                            <input type="hidden" name="status" value="pending">
                            
                            <div class="alert alert-warning">
                                <i class="bi bi-info-circle"></i> 
                                <strong>Lưu ý:</strong> Nhà hàng mới sẽ ở trạng thái <strong>"Chờ duyệt"</strong>. 
                                Admin sẽ duyệt trước khi hiển thị trên ứng dụng.
                            </div>
                            
                            <div class="d-flex gap-2">
                                <button type="submit" class="btn btn-primary">
                                    <i class="bi bi-check-lg"></i> <?= $id ? 'Cập nhật' : 'Thêm mới' ?>
                                </button>
                                <a href="pois.php" class="btn btn-outline-secondary">Hủy</a>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>
