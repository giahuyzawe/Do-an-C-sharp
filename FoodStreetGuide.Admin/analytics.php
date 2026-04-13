<?php
session_start();
if (!isset($_SESSION['admin'])) {
    header('Location: login.php');
    exit;
}

// Load POIs
$storage_file = 'pois.json';
$pois = [];
if (file_exists($storage_file)) {
    $pois = json_decode(file_get_contents($storage_file), true) ?: [];
}

// Analytics data
$total_visits = array_sum(array_column($pois, 'visitCount'));
$avg_visits = $pois ? round($total_visits / count($pois), 1) : 0;

// Sort for most/least popular
usort($pois, fn($a, $b) => ($b['visitCount'] ?? 0) <=> ($a['visitCount'] ?? 0));
$most_popular = array_slice($pois, 0, 5);
$least_popular = array_slice(array_reverse($pois), 0, 5);

// Mock monthly data
$monthly_visits = [320, 450, 380, 520, 480, 600, 550, 620, 580, 700, 650, 750];
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Phân tích - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
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
        .chart-container {
            position: relative;
            height: 300px;
        }
        .stat-box {
            background: white;
            border-radius: 1rem;
            padding: 1.5rem;
            text-align: center;
        }
        .stat-box i {
            font-size: 2rem;
            color: var(--primary);
            margin-bottom: 0.5rem;
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
            <a class="nav-link active" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="qr-generator.php"><i class="bi bi-qr-code"></i> Tạo QR Code</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <h2 class="mb-4">Phân tích chi tiết</h2>

        <!-- Stats Row -->
        <div class="row g-4 mb-4">
            <div class="col-md-3">
                <div class="stat-box">
                    <i class="bi bi-eye"></i>
                    <h4><?php echo $total_visits; ?></h4>
                    <p class="text-muted mb-0">Tổng lượt xem</p>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-box">
                    <i class="bi bi-calculator"></i>
                    <h4><?php echo $avg_visits; ?></h4>
                    <p class="text-muted mb-0">Trung bình/POI</p>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-box">
                    <i class="bi bi-mic"></i>
                    <h4><?php echo $total_visits * 2; ?></h4>
                    <p class="text-muted mb-0">Narrations</p>
                </div>
            </div>
            <div class="col-md-3">
                <div class="stat-box">
                    <i class="bi bi-geo-alt"></i>
                    <h4><?php echo count($pois); ?></h4>
                    <p class="text-muted mb-0">Tổng POI</p>
                </div>
            </div>
        </div>

        <!-- Monthly Chart -->
        <div class="card mb-4">
            <div class="card-header">
                <h5 class="mb-0">Lượt truy cập theo tháng</h5>
            </div>
            <div class="card-body">
                <div class="chart-container">
                    <canvas id="monthlyChart"></canvas>
                </div>
            </div>
        </div>

        <div class="row g-4">
            <!-- Most Popular -->
            <div class="col-lg-6">
                <div class="card">
                    <div class="card-header bg-success bg-opacity-10">
                        <h5 class="mb-0 text-success"><i class="bi bi-trophy me-2"></i>POI phổ biến nhất</h5>
                    </div>
                    <div class="card-body p-0">
                        <table class="table table-hover mb-0">
                            <thead class="table-light">
                                <tr><th>POI</th><th>Lượt xem</th></tr>
                            </thead>
                            <tbody>
                                <?php foreach ($most_popular as $poi): ?>
                                <tr>
                                    <td><?php echo htmlspecialchars($poi['nameVi']); ?></td>
                                    <td><span class="badge bg-success"><?php echo $poi['visitCount'] ?? 0; ?></span></td>
                                </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            <!-- Least Popular -->
            <div class="col-lg-6">
                <div class="card">
                    <div class="card-header bg-warning bg-opacity-10">
                        <h5 class="mb-0 text-warning"><i class="bi bi-exclamation-circle me-2"></i>POI ít phổ biến</h5>
                    </div>
                    <div class="card-body p-0">
                        <table class="table table-hover mb-0">
                            <thead class="table-light">
                                <tr><th>POI</th><th>Lượt xem</th></tr>
                            </thead>
                            <tbody>
                                <?php foreach ($least_popular as $poi): ?>
                                <tr>
                                    <td><?php echo htmlspecialchars($poi['nameVi']); ?></td>
                                    <td><span class="badge bg-warning"><?php echo $poi['visitCount'] ?? 0; ?></span></td>
                                </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        new Chart(document.getElementById('monthlyChart'), {
            type: 'bar',
            data: {
                labels: ['T1', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'T8', 'T9', 'T10', 'T11', 'T12'],
                datasets: [{
                    label: 'Lượt truy cập',
                    data: <?php echo json_encode($monthly_visits); ?>,
                    backgroundColor: '#4F46E5',
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } }
            }
        });
    </script>
</body>
</html>
