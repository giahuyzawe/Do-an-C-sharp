<?php
session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

// Define permission matrix structure
$permissions = [
    'dashboard' => ['name' => 'Dashboard', 'icon' => 'bi-grid'],
    'pois_view' => ['name' => 'View POIs', 'icon' => 'bi-geo'],
    'pois_create' => ['name' => 'Create POI', 'icon' => 'bi-plus-circle'],
    'pois_edit' => ['name' => 'Edit POI', 'icon' => 'bi-pencil'],
    'pois_delete' => ['name' => 'Delete POI', 'icon' => 'bi-trash'],
    'restaurants_view' => ['name' => 'View Restaurants', 'icon' => 'bi-shop'],
    'restaurants_manage' => ['name' => 'Manage Restaurants', 'icon' => 'bi-shop'],
    'narrations_view' => ['name' => 'View Narrations', 'icon' => 'bi-mic'],
    'narrations_manage' => ['name' => 'Manage Narrations', 'icon' => 'bi-mic'],
    'users_view' => ['name' => 'View Users', 'icon' => 'bi-people'],
    'users_manage' => ['name' => 'Manage Users', 'icon' => 'bi-people'],
    'analytics_view' => ['name' => 'View Analytics', 'icon' => 'bi-graph-up'],
    'media_manage' => ['name' => 'Manage Media', 'icon' => 'bi-images'],
    'settings_view' => ['name' => 'View Settings', 'icon' => 'bi-gear'],
    'settings_edit' => ['name' => 'Edit Settings', 'icon' => 'bi-gear'],
    'logs_view' => ['name' => 'View System Logs', 'icon' => 'bi-shield'],
    'notifications_send' => ['name' => 'Send Notifications', 'icon' => 'bi-bell'],
    'backup_manage' => ['name' => 'Backup/Restore', 'icon' => 'bi-cloud-arrow-down'],
];

$roles = [
    'SuperAdmin' => ['name' => 'Super Admin', 'color' => 'danger', 'description' => 'Full system access'],
    'Admin' => ['name' => 'Admin', 'color' => 'primary', 'description' => 'Manage content and users'],
    'Editor' => ['name' => 'Editor', 'color' => 'info', 'description' => 'Manage POIs and content'],
    'Moderator' => ['name' => 'Moderator', 'color' => 'warning', 'description' => 'View and moderate content'],
    'Viewer' => ['name' => 'Viewer', 'color' => 'secondary', 'description' => 'Read-only access'],
];

// Default permission matrix
$rolePermissions = [
    'SuperAdmin' => array_fill_keys(array_keys($permissions), true),
    'Admin' => [
        'dashboard' => true,
        'pois_view' => true, 'pois_create' => true, 'pois_edit' => true, 'pois_delete' => true,
        'restaurants_view' => true, 'restaurants_manage' => true,
        'narrations_view' => true, 'narrations_manage' => true,
        'users_view' => true, 'users_manage' => true,
        'analytics_view' => true,
        'media_manage' => true,
        'settings_view' => true, 'settings_edit' => true,
        'logs_view' => true,
        'notifications_send' => true,
        'backup_manage' => true,
    ],
    'Editor' => [
        'dashboard' => true,
        'pois_view' => true, 'pois_create' => true, 'pois_edit' => true, 'pois_delete' => false,
        'restaurants_view' => true, 'restaurants_manage' => true,
        'narrations_view' => true, 'narrations_manage' => true,
        'users_view' => false, 'users_manage' => false,
        'analytics_view' => true,
        'media_manage' => true,
        'settings_view' => false, 'settings_edit' => false,
        'logs_view' => false,
        'notifications_send' => false,
        'backup_manage' => false,
    ],
    'Moderator' => [
        'dashboard' => true,
        'pois_view' => true, 'pois_create' => false, 'pois_edit' => false, 'pois_delete' => false,
        'restaurants_view' => true, 'restaurants_manage' => false,
        'narrations_view' => true, 'narrations_manage' => false,
        'users_view' => true, 'users_manage' => false,
        'analytics_view' => true,
        'media_manage' => false,
        'settings_view' => false, 'settings_edit' => false,
        'logs_view' => true,
        'notifications_send' => false,
        'backup_manage' => false,
    ],
    'Viewer' => [
        'dashboard' => true,
        'pois_view' => true, 'pois_create' => false, 'pois_edit' => false, 'pois_delete' => false,
        'restaurants_view' => true, 'restaurants_manage' => false,
        'narrations_view' => true, 'narrations_manage' => false,
        'users_view' => false, 'users_manage' => false,
        'analytics_view' => true,
        'media_manage' => false,
        'settings_view' => false, 'settings_edit' => false,
        'logs_view' => false,
        'notifications_send' => false,
        'backup_manage' => false,
    ],
];

// Load saved permissions from file
$permsFile = 'permissions.json';
if (file_exists($permsFile)) {
    $savedPerms = json_decode(file_get_contents($permsFile), true);
    if ($savedPerms) {
        $rolePermissions = $savedPerms;
    }
}

// Save permissions
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['save_permissions'])) {
    foreach ($roles as $roleKey => $role) {
        foreach ($permissions as $permKey => $perm) {
            $rolePermissions[$roleKey][$permKey] = isset($_POST['perm_' . $roleKey . '_' . $permKey]);
        }
    }
    file_put_contents($permsFile, json_encode($rolePermissions, JSON_PRETTY_PRINT));
    $success = 'Permissions saved successfully!';
}

// Add new role
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['add_role'])) {
    $newRoleKey = preg_replace('/[^a-zA-Z0-9]/', '', $_POST['role_key']);
    if ($newRoleKey && !isset($roles[$newRoleKey])) {
        $roles[$newRoleKey] = [
            'name' => $_POST['role_name'],
            'color' => $_POST['role_color'],
            'description' => $_POST['role_description']
        ];
        // Initialize with no permissions
        $rolePermissions[$newRoleKey] = array_fill_keys(array_keys($permissions), false);
        file_put_contents($permsFile, json_encode($rolePermissions, JSON_PRETTY_PRINT));
        $success = 'New role added!';
    }
}

// Helper function to group permissions
function groupPermissions($permissions) {
    $groups = [
        'Dashboard' => ['dashboard'],
        'POI Management' => array_filter(array_keys($permissions), fn($k) => str_starts_with($k, 'pois_')),
        'Restaurant Management' => array_filter(array_keys($permissions), fn($k) => str_starts_with($k, 'restaurants_')),
        'Narration Management' => array_filter(array_keys($permissions), fn($k) => str_starts_with($k, 'narrations_')),
        'User Management' => array_filter(array_keys($permissions), fn($k) => str_starts_with($k, 'users_')),
        'Analytics & Reports' => ['analytics_view'],
        'Media Library' => ['media_manage'],
        'System Settings' => array_filter(array_keys($permissions), fn($k) => 
            str_starts_with($k, 'settings_') || 
            str_starts_with($k, 'logs_') || 
            str_starts_with($k, 'notifications_') || 
            str_starts_with($k, 'backup_')
        ),
    ];
    return $groups;
}

$permissionGroups = groupPermissions($permissions);
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Permission Matrix - Food Street Guide Admin</title>
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
        .permission-table th {
            font-weight: 600;
            font-size: 0.85rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            background: #F1F5F9;
        }
        .permission-row:hover {
            background: #F8FAFC;
        }
        .perm-checkbox {
            width: 20px;
            height: 20px;
            cursor: pointer;
        }
        .permission-group {
            background: #EEF2FF;
            font-weight: 600;
            color: var(--primary);
        }
        .role-header {
            min-width: 100px;
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
            <a class="nav-link" href="reviews.php"><i class="bi bi-star-fill"></i> Reviews</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
            <a class="nav-link active" href="permissions.php"><i class="bi bi-shield-lock"></i> Phân quyền</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <div>
                <h1 class="h3 mb-1">Permission Matrix</h1>
                <p class="text-muted mb-0">Quản lý phân quyền cho từng vai trò</p>
            </div>
            <div class="d-flex gap-2">
                <button type="button" class="btn btn-outline-primary" data-bs-toggle="modal" data-bs-target="#addRoleModal">
                    <i class="bi bi-plus-lg me-2"></i>Thêm Role
                </button>
                <a href="users.php" class="btn btn-outline-secondary">
                    <i class="bi bi-people me-2"></i>Quản lý Users
                </a>
            </div>
        </div>

        <?php if (isset($success)): ?>
        <div class="alert alert-success alert-dismissible fade show" role="alert">
            <i class="bi bi-check-circle me-2"></i><?php echo $success; ?>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
        <?php endif; ?>

        <!-- Role Stats -->
        <div class="row g-4 mb-4">
            <?php foreach ($roles as $roleKey => $role): ?>
            <div class="col-md-3">
                <div class="stats-card">
                    <span class="badge bg-<?php echo $role['color']; ?> mb-2"><?php echo $role['name']; ?></span>
                    <div class="stats-number">
                        <?php echo count(array_filter($rolePermissions[$roleKey] ?? [])); ?>
                    </div>
                    <small class="text-muted">permissions granted</small>
                    <div class="mt-2"><small class="text-muted"><?php echo $role['description']; ?></small></div>
                </div>
            </div>
            <?php endforeach; ?>
        </div>

        <!-- Permission Matrix -->
        <form method="POST">
            <div class="card">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h5 class="mb-0"><i class="bi bi-shield-lock me-2"></i>Permission Matrix</h5>
                    <button type="submit" name="save_permissions" class="btn btn-primary">
                        <i class="bi bi-save me-2"></i>Lưu thay đổi
                    </button>
                </div>
                <div class="card-body p-0">
                    <div class="table-responsive">
                        <table class="table table-bordered mb-0 permission-table">
                            <thead>
                                <tr>
                                    <th class="permission-group">Feature / Permission</th>
                                    <?php foreach ($roles as $roleKey => $role): ?>
                                    <th class="text-center role-header bg-<?php echo $role['color']; ?> bg-opacity-10">
                                        <span class="badge bg-<?php echo $role['color']; ?>"><?php echo $role['name']; ?></span>
                                    </th>
                                    <?php endforeach; ?>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($permissionGroups as $groupName => $groupPerms): ?>
                                <tr>
                                    <td colspan="<?php echo count($roles) + 1; ?>" class="permission-group">
                                        <i class="bi bi-folder me-2"></i><?php echo $groupName; ?>
                                    </td>
                                </tr>
                                <?php foreach ($groupPerms as $permKey): 
                                    if (!isset($permissions[$permKey])) continue;
                                    $perm = $permissions[$permKey];
                                ?>
                                <tr class="permission-row">
                                    <td class="ps-4">
                                        <i class="bi <?php echo $perm['icon']; ?> me-2 text-muted"></i>
                                        <?php echo $perm['name']; ?>
                                    </td>
                                    <?php foreach ($roles as $roleKey => $role): ?>
                                    <td class="text-center">
                                        <input type="checkbox" 
                                               class="form-check-input perm-checkbox" 
                                               name="perm_<?php echo $roleKey; ?>_<?php echo $permKey; ?>"
                                               <?php echo ($rolePermissions[$roleKey][$permKey] ?? false) ? 'checked' : ''; ?>>
                                    </td>
                                    <?php endforeach; ?>
                                </tr>
                                <?php endforeach; ?>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </form>

        <!-- Quick Actions -->
        <div class="card mt-4">
            <div class="card-header">
                <h5 class="mb-0"><i class="bi bi-lightning me-2"></i>Quick Actions</h5>
            </div>
            <div class="card-body">
                <div class="row g-3">
                    <div class="col-md-6">
                        <div class="d-flex align-items-center p-3 border rounded">
                            <div class="bg-primary bg-opacity-10 text-primary rounded-circle p-2 me-3">
                                <i class="bi bi-shield-check fs-4"></i>
                            </div>
                            <div>
                                <h6 class="mb-1">Default Permissions</h6>
                                <small class="text-muted">Reset to system default permission matrix</small>
                            </div>
                            <button type="button" class="btn btn-sm btn-outline-primary ms-auto" onclick="resetDefaults()">
                                Reset
                            </button>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="d-flex align-items-center p-3 border rounded">
                            <div class="bg-success bg-opacity-10 text-success rounded-circle p-2 me-3">
                                <i class="bi bi-download fs-4"></i>
                            </div>
                            <div>
                                <h6 class="mb-1">Export Config</h6>
                                <small class="text-muted">Download permission matrix as JSON</small>
                            </div>
                            <a href="permissions.json" download class="btn btn-sm btn-outline-success ms-auto">
                                Download
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Add Role Modal -->
    <div class="modal fade" id="addRoleModal" tabindex="-1">
        <div class="modal-dialog">
            <div class="modal-content">
                <form method="POST">
                    <div class="modal-header">
                        <h5 class="modal-title">Thêm Role mới</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label">Role Key</label>
                            <input type="text" name="role_key" class="form-control" placeholder="e.g., ContentManager" required>
                            <small class="text-muted">Unique identifier (no spaces)</small>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Role Name</label>
                            <input type="text" name="role_name" class="form-control" placeholder="e.g., Content Manager" required>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Color</label>
                            <select name="role_color" class="form-select">
                                <option value="primary">Blue</option>
                                <option value="secondary">Gray</option>
                                <option value="success">Green</option>
                                <option value="danger">Red</option>
                                <option value="warning">Yellow</option>
                                <option value="info">Cyan</option>
                                <option value="dark">Dark</option>
                            </select>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">Description</label>
                            <textarea name="role_description" class="form-control" rows="2" placeholder="Brief description of this role..."></textarea>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Hủy</button>
                        <button type="submit" name="add_role" class="btn btn-primary">Thêm Role</button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        function resetDefaults() {
            if (confirm('Reset all permissions to system defaults? This cannot be undone.')) {
                // Clear localStorage and reload
                localStorage.clear();
                location.reload();
            }
        }

        // Highlight row on hover for better UX
        document.querySelectorAll('.permission-row').forEach(row => {
            row.addEventListener('mouseenter', () => {
                row.style.backgroundColor = '#F1F5F9';
            });
            row.addEventListener('mouseleave', () => {
                row.style.backgroundColor = '';
            });
        });
    </script>
</body>
</html>
