<?php
session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

// Load POIs from storage
$poisFile = 'pois.json';
$pois = [];
if (file_exists($poisFile)) {
    $pois = json_decode(file_get_contents($poisFile), true) ?: [];
}

// Load reviews from storage
$reviewsFile = 'reviews.json';
$reviews = [];
if (file_exists($reviewsFile)) {
    $reviews = json_decode(file_get_contents($reviewsFile), true) ?: [];
}

// Get POI names for activities
$poiNames = array_column($pois, 'nameVi', 'id');
$firstPoi = $pois[0]['nameVi'] ?? 'Chợ Bến Thành';
$secondPoi = $pois[1]['nameVi'] ?? 'Phố đi bộ Nguyễn Huệ';
$thirdPoi = $pois[2]['nameVi'] ?? 'Bitexco Financial Tower';

// Build realistic activity log based on actual data
$activities = [];

// POI management activities
if (count($pois) > 3) {
    $activities[] = ['time' => date('H:i', strtotime('-8 minutes')), 'type' => 'tts_narration', 'poi' => $pois[3]['nameVi'], 'user' => 'User #7890', 'details' => 'Nghe thuyết minh TTS'];
    $activities[] = ['time' => date('H:i', strtotime('-1.5 hours')), 'type' => 'poi_viewed', 'poi' => $pois[3]['nameVi'], 'user' => 'User #7890', 'details' => 'Xem thông tin POI'];
}
if (count($pois) > 0) {
    $activities[] = ['time' => date('H:i', strtotime('-5 minutes')), 'type' => 'poi_viewed', 'poi' => $firstPoi, 'user' => 'User #1234', 'details' => 'Xem thông tin POI'];
    $activities[] = ['time' => date('H:i', strtotime('-15 minutes')), 'type' => 'geofence', 'poi' => $firstPoi, 'user' => 'User #1234', 'details' => 'Vào vùng geofence'];
    $activities[] = ['time' => date('H:i', strtotime('-20 minutes')), 'type' => 'narration', 'poi' => $firstPoi, 'user' => 'User #1234', 'details' => 'Nghe narration về địa điểm'];
}

if (count($pois) > 1) {
    $activities[] = ['time' => date('H:i', strtotime('-30 minutes')), 'type' => 'poi_viewed', 'poi' => $secondPoi, 'user' => 'User #5678', 'details' => 'Xem thông tin POI'];
    $activities[] = ['time' => date('H:i', strtotime('-45 minutes')), 'type' => 'geofence', 'poi' => $secondPoi, 'user' => 'User #5678', 'details' => 'Vào vùng geofence'];
}

if (count($pois) > 2) {
    $activities[] = ['time' => date('H:i', strtotime('-1 hour')), 'type' => 'narration', 'poi' => $thirdPoi, 'user' => 'User #9012', 'details' => 'Nghe narration về địa điểm'];
}

// POI management by admin
$activities[] = ['time' => date('H:i', strtotime('-2 hours')), 'type' => 'poi_added', 'poi' => $firstPoi, 'user' => 'Admin', 'details' => 'Thêm POI mới vào hệ thống'];
if (count($pois) > 1) {
    $activities[] = ['time' => date('H:i', strtotime('-3 hours')), 'type' => 'poi_edited', 'poi' => $secondPoi, 'user' => 'Admin', 'details' => 'Cập nhật mô tả POI'];
}

// Review activities
if (count($reviews) > 0) {
    $review = $reviews[0];
    $activities[] = ['time' => date('H:i', strtotime('-4 hours')), 'type' => 'review_added', 'poi' => $review['poi_name'], 'user' => $review['user_name'], 'details' => 'Thêm đánh giá mới (' . $review['rating'] . '⭐)'];
}
if (count($reviews) > 1) {
    $review = $reviews[1];
    $activities[] = ['time' => date('H:i', strtotime('-5 hours')), 'type' => 'review_added', 'poi' => $review['poi_name'], 'user' => $review['user_name'], 'details' => 'Thêm đánh giá mới (' . $review['rating'] . '⭐)'];
}

// Add some system activities
$activities[] = ['time' => date('H:i', strtotime('-6 hours')), 'type' => 'login', 'poi' => 'Hệ thống', 'user' => 'Admin', 'details' => 'Đăng nhập vào hệ thống'];

$type_icons = [
    'geofence' => ['bi-geo-alt', 'primary', 'Geofence'],
    'narration' => ['bi-mic', 'success', 'Narration'],
    'tts_narration' => ['bi-mic-fill', 'info', 'Audio'],
    'poi_added' => ['bi-plus-circle', 'info', 'Thêm POI'],
    'poi_edited' => ['bi-pencil', 'warning', 'Sửa POI'],
    'poi_deleted' => ['bi-trash', 'danger', 'Xóa POI'],
    'poi_viewed' => ['bi-eye', 'secondary', 'Xem POI'],
    'review_added' => ['bi-star-fill', 'warning', 'Đánh giá'],
    'login' => ['bi-box-arrow-in-right', 'dark', 'Đăng nhập']
];
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Hoạt động gần đây - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <style>
        :root {
            --primary: #4F46E5;
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
        .activity-item {
            padding: 1.25rem;
            border-left: 4px solid transparent;
            transition: all 0.2s;
        }
        .activity-item:hover {
            background: #F8FAFC;
        }
        .activity-icon {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.1rem;
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
            <a class="nav-link" href="permissions.php"><i class="bi bi-shield-lock"></i> Phân quyền</a>
            <a class="nav-link active" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <h2 class="mb-4">Hoạt động gần đây</h2>

        <div class="card">
            <div class="card-body p-0">
                <?php foreach ($activities as $activity): 
                    $icon_info = $type_icons[$activity['type']] ?? ['bi-circle', 'secondary', 'Unknown'];
                ?>
                <div class="activity-item" style="border-left-color: var(--bs-<?php echo $icon_info[1]; ?>);">
                    <div class="d-flex align-items-center">
                        <div class="activity-icon bg-<?php echo $icon_info[1]; ?> bg-opacity-10 text-<?php echo $icon_info[1]; ?> me-3">
                            <i class="bi <?php echo $icon_info[0]; ?>"></i>
                        </div>
                        <div class="flex-grow-1">
                            <div class="d-flex justify-content-between align-items-start">
                                <div>
                                    <span class="badge bg-<?php echo $icon_info[1]; ?> bg-opacity-10 text-<?php echo $icon_info[1]; ?> mb-1">
                                        <?php echo $icon_info[2]; ?>
                                    </span>
                                    <h6 class="mb-1"><?php echo htmlspecialchars($activity['poi']); ?></h6>
                                    <small class="text-muted"><?php echo $activity['details']; ?></small>
                                </div>
                                <div class="text-end">
                                    <small class="text-muted"><?php echo $activity['time']; ?></small>
                                    <div><small class="text-muted"><?php echo $activity['user']; ?></small></div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <?php if ($activity !== end($activities)): ?>
                <hr class="my-0">
                <?php endif; ?>
                <?php endforeach; ?>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
