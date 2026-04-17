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

// Stats
$total_pois = count($pois);
$total_visits = array_sum(array_column($pois, 'visitCount'));
$most_popular = $pois ? max(array_column($pois, 'visitCount')) : 0;
$this_month = count(array_filter($pois, fn($p) => strpos($p['createdAt'] ?? '', date('Y-m')) === 0));

// Mock data for charts
$visits_per_day = [45, 52, 38, 65, 48, 72, 58];
$poi_names = array_slice(array_column($pois, 'nameVi'), 0, 5);
$poi_visits = array_slice(array_column($pois, 'visitCount'), 0, 5);
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Dashboard - Food Street Guide Admin</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.1/font/bootstrap-icons.css" rel="stylesheet">
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap" rel="stylesheet">
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
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
        .sidebar-brand i {
            font-size: 1.5rem;
            color: var(--primary);
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
            min-height: 100vh;
        }
        .stats-card {
            background: white;
            border-radius: 1rem;
            padding: 1.5rem;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            border-left: 4px solid var(--primary);
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
        #map {
            height: 400px;
            border-radius: 0.75rem;
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
            <a class="nav-link active" href="index.php"><i class="bi bi-grid"></i> Dashboard</a>
            <a class="nav-link" href="pois.php"><i class="bi bi-geo"></i> Quản lý POI</a>
            <a class="nav-link" href="restaurant-approval.php"><i class="bi bi-check-circle"></i> Duyệt Nhà Hàng</a>
            <a class="nav-link" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
            <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
            <a class="nav-link" href="activity.php"><i class="bi bi-clock-history"></i> Hoạt động</a>
            <a class="nav-link" href="settings.php"><i class="bi bi-gear"></i> Cài đặt</a>
            <a class="nav-link" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
        </nav>
    </div>

    <div class="main-content">
        <div class="d-flex justify-content-between align-items-center mb-4">
            <h2>Dashboard</h2>
            <a href="poi-form.php" class="btn btn-primary"><i class="bi bi-plus-lg me-2"></i>Thêm POI</a>
        </div>

        <!-- Stats Cards -->
        <div class="row g-4 mb-4">
            <?php
            $stats = [
                ['Tổng POI', $total_pois, 'bi-geo-alt', '#4F46E5'],
                ['Tổng lượt', $total_visits, 'bi-eye', '#10B981'],
                ['Phổ biến', $most_popular, 'bi-star', '#F59E0B'],
                ['Tháng này', $this_month, 'bi-calendar', '#8B5CF6'],
                ['Narrations', $total_visits * 2, 'bi-mic', '#EC4899'],
                ['Tracking', rand(5, 50), 'bi-broadcast', '#06B6D4']
            ];
            foreach ($stats as $stat):
            ?>
            <div class="col-md-4 col-lg-2">
                <div class="stats-card" style="border-left-color: <?php echo $stat[3]; ?>">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <p class="text-muted mb-1"><?php echo $stat[0]; ?></p>
                            <h3 class="mb-0"><?php echo $stat[1]; ?></h3>
                        </div>
                        <i class="bi <?php echo $stat[2]; ?>" style="font-size: 2rem; color: <?php echo $stat[3]; ?>"></i>
                    </div>
                </div>
            </div>
            <?php endforeach; ?>
        </div>

        <!-- Charts -->
        <div class="row g-4 mb-4">
            <div class="col-lg-8">
                <div class="card">
                    <div class="card-header"><h5>Lượt truy cập theo ngày</h5></div>
                    <div class="card-body">
                        <div class="chart-container">
                            <canvas id="visitsChart"></canvas>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-lg-4">
                <div class="card">
                    <div class="card-header"><h5>Top POI</h5></div>
                    <div class="card-body">
                        <div class="chart-container">
                            <canvas id="poiChart"></canvas>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Map & Table -->
        <div class="row g-4">
            <div class="col-lg-8">
                <div class="card">
                    <div class="card-header"><h5>Bản đồ POI</h5></div>
                    <div class="card-body p-0"><div id="map"></div></div>
                    <div class="card-footer bg-white border-0">
                        <small class="text-muted d-flex flex-wrap gap-1">
                            <span class="badge" style="background: #4F46E5; font-size: 0.65rem;">Landmark</span>
                            <span class="badge" style="background: #EF4444; font-size: 0.65rem;">Restaurant</span>
                            <span class="badge" style="background: #F59E0B; font-size: 0.65rem;">Cafe</span>
                            <span class="badge" style="background: #8B5CF6; font-size: 0.65rem;">Bar</span>
                            <span class="badge" style="background: #14B8A6; font-size: 0.65rem;">Market</span>
                            <span class="badge" style="background: #EC4899; font-size: 0.65rem;">Night Market</span>
                            <span class="badge" style="background: #F97316; font-size: 0.65rem;">Street Food</span>
                            <span class="badge" style="background: #10B981; font-size: 0.65rem;">Temple</span>
                            <span class="badge" style="background: #22C55E; font-size: 0.65rem;">Park</span>
                            <span class="badge" style="background: #06B6D4; font-size: 0.65rem;">Museum</span>
                            <span class="badge" style="background: #6366F1; font-size: 0.65rem;">Shopping</span>
                            <span class="badge" style="background: #3B82F6; font-size: 0.65rem;">Bridge</span>
                        </small>
                    </div>
                </div>
            </div>
            <div class="col-lg-4">
                <div class="card">
                    <div class="card-header"><h5>Top 10 POI</h5></div>
                    <div class="card-body p-0">
                        <table class="table table-hover mb-0">
                            <thead class="table-light"><tr><th>POI</th><th>Lượt</th></tr></thead>
                            <tbody>
                                <?php 
                                usort($pois, fn($a, $b) => ($b['visitCount'] ?? 0) <=> ($a['visitCount'] ?? 0));
                                foreach (array_slice($pois, 0, 10) as $poi): 
                                ?>
                                <tr>
                                    <td><?php echo htmlspecialchars($poi['nameVi']); ?></td>
                                    <td><?php echo $poi['visitCount'] ?? 0; ?></td>
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
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <script>
        new Chart(document.getElementById('visitsChart'), {
            type: 'line',
            data: {
                labels: ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'],
                datasets: [{
                    data: <?php echo json_encode($visits_per_day); ?>,
                    borderColor: '#4F46E5',
                    backgroundColor: 'rgba(79, 70, 229, 0.1)',
                    fill: true,
                    tension: 0.4
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
        });

        new Chart(document.getElementById('poiChart'), {
            type: 'bar',
            data: {
                labels: <?php echo json_encode($poi_names); ?>,
                datasets: [{ data: <?php echo json_encode($poi_visits); ?>, backgroundColor: '#4F46E5' }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
        });

        // Category colors configuration
        const categoryColors = {
            'landmark': '#4F46E5',      // Indigo
            'restaurant': '#EF4444',    // Red
            'cafe': '#F59E0B',         // Amber
            'bar': '#8B5CF6',          // Purple
            'market': '#14B8A6',        // Teal
            'night_market': '#EC4899', // Pink
            'street_food': '#F97316',  // Orange
            'temple': '#10B981',       // Emerald
            'park': '#22C55E',         // Green
            'museum': '#06B6D4',       // Cyan
            'shopping': '#6366F1',     // Indigo
            'bridge': '#3B82F6'        // Blue
        };

        // Create custom colored marker icon
        function createCustomIcon(color) {
            return L.divIcon({
                className: 'custom-marker',
                html: `<div style="
                    background-color: ${color};
                    width: 24px;
                    height: 24px;
                    border-radius: 50% 50% 50% 0;
                    border: 2px solid white;
                    box-shadow: 0 2px 6px rgba(0,0,0,0.4);
                    transform: rotate(-45deg);
                "></div>`,
                iconSize: [24, 24],
                iconAnchor: [12, 24],
                popupAnchor: [0, -24]
            });
        }

        const map = L.map('map').setView([10.762622, 106.660172], 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);

        const markers = [];
        <?php foreach ($pois as $poi): ?>
        (function() {
            const color = categoryColors['<?php echo $poi['category'] ?? 'landmark'; ?>'] || '#4F46E5';
            const marker = L.marker([<?php echo $poi['latitude']; ?>, <?php echo $poi['longitude']; ?>], {
                icon: createCustomIcon(color)
            }).addTo(map).bindPopup(`
                <div style="text-align: center;">
                    <div class="fw-semibold"><?php echo htmlspecialchars($poi['nameVi']); ?></div>
                    <span class="badge" style="background-color: ${color}; font-size: 0.7rem;"><?php echo $poi['category'] ?? 'landmark'; ?></span>
                    <?php if (!empty($poi['narrationTextVi'])): ?>
                    <div class="mt-1">
                        <span class="badge bg-info" style="font-size: 0.6rem;"><i class="bi bi-mic-fill me-1"></i>TTS</span>
                    </div>
                    <?php endif; ?>
                </div>
            `);
            markers.push(marker);
        })();
        <?php endforeach; ?>

        // Fit map to show all markers if there are POIs
        if (markers.length > 0) {
            const group = new L.featureGroup(markers);
            map.fitBounds(group.getBounds().pad(0.1));
        }
    </script>
</body>
</html>
