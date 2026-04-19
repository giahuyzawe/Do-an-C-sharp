<?php
require_once 'config.php';
require_admin();

$users = load_json($USERS_FILE);
$pois = load_json($POIS_FILE);

// Handle add/edit user
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $userId = $_POST['user_id'] ?? null;
    $email = $_POST['email'] ?? '';
    $password = $_POST['password'] ?? '';
    $name = $_POST['name'] ?? '';
    $username = $_POST['username'] ?? '';
    $role = $_POST['role'] ?? 'restaurant_owner';
    $restaurantIds = $_POST['restaurant_ids'] ?? [];
    
    // Use email as username if username not provided
    $displayUsername = $username ?: explode('@', $email)[0];
    
    $userData = [
        'id' => $userId ?? generate_id('usr_'),
        'email' => $email,
        'username' => $displayUsername,
        'name' => $name,
        'role' => $role,
        'restaurantIds' => $role === 'restaurant_owner' ? array_map('intval', $restaurantIds) : [],
        'createdAt' => $userId ? null : date('Y-m-d H:i:s')
    ];
    
    // Only hash password if provided
    if ($password) {
        $userData['password'] = password_hash($password, PASSWORD_DEFAULT);
    }
    
    // Update or add
    $found = false;
    foreach ($users as &$u) {
        if ($u['id'] === $userData['id']) {
            $u = array_merge($u, array_filter($userData));
            $found = true;
            break;
        }
    }
    
    if (!$found) {
        $users[] = $userData;
    }
    
    save_json($USERS_FILE, $users);
    $success = $userId ? 'Đã cập nhật người dùng!' : 'Đã thêm người dùng mới!';
}

// Handle delete
if (isset($_GET['delete'])) {
    $deleteId = $_GET['delete'];
    $users = array_filter($users, fn($u) => $u['id'] !== $deleteId);
    save_json($USERS_FILE, array_values($users));
    header('Location: users.php');
    exit;
}

// Get user for edit
$editUser = null;
if (isset($_GET['edit'])) {
    foreach ($users as $u) {
        if ($u['id'] === $_GET['edit']) {
            $editUser = $u;
            break;
        }
    }
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>Quản lý người dùng - Food Tour</title>
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
                    
                    <a class="nav-link text-warning" href="restaurant-approval.php"><i class="bi bi-check-circle-fill"></i> <strong>Duyệt nhà hàng</strong></a>
                    <a class="nav-link" href="pois.php"><i class="bi bi-shop"></i> Nhà hàng</a>
                    <a class="nav-link active" href="users.php"><i class="bi bi-people"></i> Người dùng</a>
                    
                    <a class="nav-link" href="reviews.php"><i class="bi bi-star"></i> Đánh giá</a>
                    <a class="nav-link" href="statistics.php"><i class="bi bi-graph-up"></i> Thống kê</a>
                    
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <h4 class="mb-4">👥 Quản lý người dùng</h4>
                
                <?php if (isset($success)): ?>
                    <div class="alert alert-success"><?= $success ?></div>
                <?php endif; ?>

                <div class="row">
                    <!-- User List -->
                    <div class="col-md-7">
                        <div class="card">
                            <div class="card-header d-flex justify-content-between align-items-center">
                                <span class="fw-bold">Danh sách người dùng</span>
                                <span class="badge bg-primary"><?= count($users) ?> người</span>
                            </div>
                            <div class="table-responsive">
                                <table class="table table-hover mb-0">
                                    <thead class="table-light">
                                        <tr>
                                            <th>Tên</th>
                                            <th>Tài khoản</th>
                                            <th>Vai trò</th>
                                            <th>Nhà hàng</th>
                                            <th>Thao tác</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <?php foreach ($users as $u): ?>
                                        <tr>
                                            <td>
                                                <strong><?= htmlspecialchars($u['name'] ?? '') ?></strong>
                                                <br><small class="text-muted"><?= htmlspecialchars($u['email'] ?? '') ?></small>
                                            </td>
                                            <td><?= htmlspecialchars($u['username']) ?></td>
                                            <td>
                                                <span class="badge bg-<?= $u['role']==='admin'?'danger':'info' ?>">
                                                    <?= $u['role']==='admin'?'Admin':'Chủ quán' ?>
                                                </span>
                                            </td>
                                            <td>
                                                <?php if ($u['role'] === 'restaurant_owner'): ?>
                                                    <?= count($u['restaurantIds'] ?? 0) ?> quán
                                                <?php else: ?>
                                                    <span class="text-muted">-</span>
                                                <?php endif; ?>
                                            </td>
                                            <td>
                                                <a href="?edit=<?= $u['id'] ?>" class="btn btn-sm btn-outline-primary">
                                                    <i class="bi bi-pencil"></i>
                                                </a>
                                                <?php if ($u['id'] !== current_user()['id']): ?>
                                                <a href="?delete=<?= $u['id'] ?>" class="btn btn-sm btn-outline-danger" onclick="return confirm('Xóa người dùng này?')">
                                                    <i class="bi bi-trash"></i>
                                                </a>
                                                <?php endif; ?>
                                            </td>
                                        </tr>
                                        <?php endforeach; ?>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Add/Edit Form -->
                    <div class="col-md-5">
                        <div class="card">
                            <div class="card-header fw-bold">
                                <?= $editUser ? '✏️ Sửa người dùng' : '➕ Thêm người dùng' ?>
                            </div>
                            <div class="card-body">
                                <form method="POST">
                                    <?php if ($editUser): ?>
                                    <input type="hidden" name="user_id" value="<?= $editUser['id'] ?>">
                                    <?php endif; ?>
                                    
                                    <div class="mb-3">
                                        <label class="form-label">Email * (dùng để đăng nhập)</label>
                                        <input type="email" name="email" class="form-control" required 
                                               value="<?= $editUser['email'] ?? '' ?>"
                                               <?= $editUser ? 'readonly' : '' ?>>
                                    </div>
                                    
                                    <div class="mb-3">
                                        <label class="form-label">Mật khẩu <?= $editUser ? '(để trống nếu không đổi)' : '*' ?></label>
                                        <input type="password" name="password" class="form-control" 
                                               <?= $editUser ? '' : 'required' ?>>
                                    </div>
                                    
                                    <div class="mb-3">
                                        <label class="form-label">Họ tên *</label>
                                        <input type="text" name="name" class="form-control" required 
                                               value="<?= $editUser['name'] ?? '' ?>">
                                    </div>
                                    
                                    <div class="mb-3">
                                        <label class="form-label">Tên hiển thị (username)</label>
                                        <input type="text" name="username" class="form-control" 
                                               value="<?= $editUser['username'] ?? '' ?>"
                                               placeholder="tên để hiển thị">
                                    </div>
                                    
                                    <div class="mb-3">
                                        <label class="form-label">Vai trò *</label>
                                        <select name="role" class="form-select" id="roleSelect" onchange="toggleRestaurants()">
                                            <option value="admin" <?= ($editUser['role'] ?? '') === 'admin' ? 'selected' : '' ?>>Admin</option>
                                            <option value="restaurant_owner" <?= ($editUser['role'] ?? 'restaurant_owner') === 'restaurant_owner' ? 'selected' : '' ?>>Chủ nhà hàng</option>
                                        </select>
                                    </div>
                                    
                                    <!-- Restaurant assignment -->
                                    <div class="mb-3" id="restaurantSection" style="display: <?= ($editUser['role'] ?? 'restaurant_owner') === 'restaurant_owner' ? 'block' : 'none' ?>">
                                        <label class="form-label">Gán nhà hàng</label>
                                        <div style="max-height: 200px; overflow-y: auto; border: 1px solid #dee2e6; border-radius: 4px; padding: 10px;">
                                            <?php foreach ($pois as $poi): 
                                                $isChecked = $editUser && in_array($poi['id'], $editUser['restaurantIds'] ?? []);
                                            ?>
                                            <div class="form-check">
                                                <input class="form-check-input" type="checkbox" name="restaurant_ids[]" 
                                                       value="<?= $poi['id'] ?>" id="poi_<?= $poi['id'] ?>" 
                                                       <?= $isChecked ? 'checked' : '' ?>>
                                                <label class="form-check-label" for="poi_<?= $poi['id'] ?>">
                                                    <?= htmlspecialchars($poi['nameVi'] ?? $poi['name']) ?>
                                                </label>
                                            </div>
                                            <?php endforeach; ?>
                                        </div>
                                    </div>
                                    
                                    <div class="d-flex gap-2">
                                        <button type="submit" class="btn btn-primary">
                                            <?= $editUser ? 'Cập nhật' : 'Thêm mới' ?>
                                        </button>
                                        <?php if ($editUser): ?>
                                        <a href="users.php" class="btn btn-outline-secondary">Hủy</a>
                                        <?php endif; ?>
                                    </div>
                                </form>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        function toggleRestaurants() {
            const role = document.getElementById('roleSelect').value;
            document.getElementById('restaurantSection').style.display = role === 'restaurant_owner' ? 'block' : 'none';
        }
    </script>
</body>
</html>
