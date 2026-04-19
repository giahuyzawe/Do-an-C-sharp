<?php
// Prevent browser caching
header('Cache-Control: no-cache, no-store, must-revalidate');
header('Pragma: no-cache');
header('Expires: 0');

require_once 'config.php';

// FORCE recreate default users (for debugging)
$users = [
    [
        'id' => 'usr_admin_001',
        'email' => 'admin@foodtour.vn',
        'username' => 'admin',
        'password' => password_hash('admin123', PASSWORD_DEFAULT),
        'name' => 'Administrator',
        'role' => 'admin',
        'createdAt' => date('Y-m-d H:i:s')
    ],
    [
        'id' => 'usr_owner_001',
        'email' => 'owner@phohoa.vn',
        'username' => 'pho_hoa',
        'password' => password_hash('owner123', PASSWORD_DEFAULT),
        'name' => 'Chủ quán Phở Hòa',
        'role' => 'restaurant_owner',
        'restaurantIds' => [1, 4],
        'createdAt' => date('Y-m-d H:i:s')
    ]
];
save_json($USERS_FILE, $users);

$error = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $email = $_POST['email'] ?? '';
    $password = $_POST['password'] ?? '';
    
    $users = load_json($USERS_FILE);
    foreach ($users as $user) {
        if (($user['email'] === $email || $user['username'] === $email) && password_verify($password, $user['password'])) {
            $_SESSION['user'] = $user;
            record_analytics('login', ['userId' => $user['id'], 'role' => $user['role']]);
            header('Location: index.php');
            exit;
        }
    }
    $error = 'Email hoặc mật khẩu không đúng! Vui lòng kiểm tra lại.<br><small class="text-muted">Email: admin@foodtour.vn | Pass: admin123</small>';
}

if (isset($_SESSION['user'])) {
    header('Location: index.php');
    exit;
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Đăng nhập - Food Tour Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <style>
        body {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .login-card {
            background: white;
            border-radius: 16px;
            padding: 40px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            width: 100%;
            max-width: 400px;
        }
        .login-logo {
            text-align: center;
            margin-bottom: 30px;
        }
        .login-logo h2 {
            color: #667eea;
            font-weight: bold;
        }
        .btn-login {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border: none;
            width: 100%;
            padding: 12px;
            font-weight: 500;
        }
        .demo-accounts {
            margin-top: 20px;
            padding: 15px;
            background: #f8f9fa;
            border-radius: 8px;
            font-size: 0.85rem;
        }
        .demo-accounts code {
            color: #e83e8c;
            background: #fff;
            padding: 2px 6px;
            border-radius: 4px;
        }
    </style>
</head>
<body>
    <div class="login-card">
        <div class="login-logo">
            <h2>🍜 Food Tour</h2>
            <p class="text-muted">Hệ thống quản lý ẩm thực Vĩnh Khánh</p>
        </div>
        
        <?php if ($error): ?>
        <div class="alert alert-danger"><?= $error ?></div>
        <?php endif; ?>
        
        <form method="POST">
            <div class="mb-3">
                <label class="form-label">Email</label>
                <input type="email" name="email" class="form-control" required 
                       placeholder="email@example.com">
            </div>
            <div class="mb-3">
                <label class="form-label">Mật khẩu</label>
                <input type="password" name="password" class="form-control" required 
                       placeholder="••••••">
            </div>
            <button type="submit" class="btn btn-primary btn-login">Đăng nhập</button>
        </form>
        
        <div class="demo-accounts">
            <strong>Tài khoản demo:</strong><br>
            <strong>Admin:</strong> <code>admin@foodtour.vn</code> / <code>admin123</code><br>
            <strong>Chủ quán:</strong> <code>owner@phohoa.vn</code> / <code>owner123</code><br>
            <small><a href="debug.php" target="_blank">🔧 Debug info</a></small>
        </div>
    </div>
</body>
</html>
