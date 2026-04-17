<?php
/**
 * Trang quản lý Audio riêng
 * Quản lý audio narration cho từng nhà hàng
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

// Only approved restaurants
$approved_pois = array_filter($pois, fn($p) => ($p['approvalStatus'] ?? 'pending') === 'approved');

// Handle audio upload/update
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $poi_id = $_POST['poi_id'] ?? null;
    $audio_type = $_POST['audio_type'] ?? 'tts'; // 'tts' or 'file'
    $audio_text = $_POST['audio_text'] ?? '';
    $audio_url = $_POST['audio_url'] ?? '';
    $auto_play = isset($_POST['auto_play']);
    
    if ($poi_id) {
        foreach ($pois as &$poi) {
            if ($poi['id'] == $poi_id) {
                $poi['audio'] = [
                    'type' => $audio_type,
                    'text' => $audio_text,
                    'url' => $audio_url,
                    'autoPlay' => $auto_play,
                    'updatedAt' => date('Y-m-d H:i:s')
                ];
                break;
            }
        }
        file_put_contents($storage_file, json_encode($pois, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
        
        header('Location: audio-management.php?success=1&poi_id=' . $poi_id);
        exit;
    }
}

$selected_poi_id = $_GET['poi_id'] ?? null;
$selected_poi = null;

if ($selected_poi_id) {
    foreach ($approved_pois as $poi) {
        if ($poi['id'] == $selected_poi_id) {
            $selected_poi = $poi;
            break;
        }
    }
}

// Audio stats
$with_audio = count(array_filter($approved_pois, fn($p) => !empty($p['audio'])));
$without_audio = count($approved_pois) - $with_audio;

?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Quản lý Audio - Food Street Guide Admin</title>
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
        .poi-item {
            cursor: pointer;
            padding: 15px;
            border-radius: 12px;
            transition: all 0.2s;
            border: 2px solid transparent;
            margin-bottom: 10px;
            background: white;
        }
        .poi-item:hover {
            border-color: var(--primary);
            transform: translateX(5px);
        }
        .poi-item.selected {
            border-color: var(--primary);
            background: rgba(255, 107, 53, 0.05);
        }
        .poi-item.has-audio {
            border-left: 4px solid #10B981;
        }
        .audio-indicator {
            width: 10px;
            height: 10px;
            border-radius: 50%;
            display: inline-block;
        }
        .audio-indicator.active { background: #10B981; }
        .audio-indicator.inactive { background: #EF4444; }
        .tts-preview {
            background: #F0F9FF;
            border: 1px solid #BAE6FD;
            border-radius: 12px;
            padding: 20px;
        }
        .waveform {
            height: 60px;
            background: linear-gradient(90deg, #FF6B35 0%, #2EC4B6 100%);
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-size: 1.5rem;
        }
        .stat-card {
            background: white;
            border-radius: 1rem;
            padding: 1.5rem;
            box-shadow: 0 1px 3px rgba(0,0,0,0.1);
        }
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
            <a class="nav-link" href="restaurant-approval.php"><i class="bi bi-check-circle"></i> Duyệt Nhà Hàng</a>
            <a class="nav-link active" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
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
                <h2 class="mb-1"><i class="bi bi-mic me-2 text-primary"></i>Quản lý Audio</h2>
                <p class="text-muted mb-0">Thêm audio thuyết minh cho từng nhà hàng</p>
            </div>
        </div>

        <!-- Stats -->
        <div class="row g-4 mb-4">
            <div class="col-md-6">
                <div class="stat-card">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <div class="text-muted small">Đã có audio</div>
                            <div class="h3 mb-0 text-success"><?php echo $with_audio; ?></div>
                        </div>
                        <i class="bi bi-volume-up-fill text-success fs-1"></i>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="stat-card">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <div class="text-muted small">Chưa có audio</div>
                            <div class="h3 mb-0 text-warning"><?php echo $without_audio; ?></div>
                        </div>
                        <i class="bi bi-volume-mute-fill text-warning fs-1"></i>
                    </div>
                </div>
            </div>
        </div>

        <div class="row">
            <!-- Restaurant List -->
            <div class="col-lg-4">
                <div class="card">
                    <div class="card-header">
                        <h5 class="mb-0"><i class="bi bi-shop me-2"></i>Chọn nhà hàng</h5>
                    </div>
                    <div class="card-body" style="max-height: 600px; overflow-y: auto;">
                        <?php foreach ($approved_pois as $poi): ?>
                        <a href="?poi_id=<?php echo $poi['id']; ?>" class="text-decoration-none text-dark">
                            <div class="poi-item <?php echo ($selected_poi_id == $poi['id']) ? 'selected' : ''; ?> <?php echo !empty($poi['audio']) ? 'has-audio' : ''; ?>">
                                <div class="d-flex align-items-center">
                                    <div class="me-3">
                                        <span class="audio-indicator <?php echo !empty($poi['audio']) ? 'active' : 'inactive'; ?>"></span>
                                    </div>
                                    <div class="flex-grow-1">
                                        <div class="fw-semibold"><?php echo htmlspecialchars($poi['nameVi']); ?></div>
                                        <small class="text-muted">
                                            <?php echo !empty($poi['audio']) ? '✓ Đã có audio' : '✗ Chưa có audio'; ?>
                                        </small>
                                    </div>
                                    <?php if ($selected_poi_id == $poi['id']): ?>
                                    <i class="bi bi-chevron-right text-primary"></i>
                                    <?php endif; ?>
                                </div>
                            </div>
                        </a>
                        <?php endforeach; ?>
                        
                        <?php if (empty($approved_pois)): ?>
                        <div class="text-center py-4">
                            <i class="bi bi-shop text-muted fs-1"></i>
                            <p class="text-muted mt-2">Chưa có nhà hàng nào được duyệt</p>
                            <a href="restaurant-approval.php" class="btn btn-primary btn-sm">Đi duyệt nhà hàng</a>
                        </div>
                        <?php endif; ?>
                    </div>
                </div>
            </div>

            <!-- Audio Editor -->
            <div class="col-lg-8">
                <?php if ($selected_poi): ?>
                <div class="card">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <h5 class="mb-0">
                            <i class="bi bi-mic me-2"></i>
                            Audio cho: <?php echo htmlspecialchars($selected_poi['nameVi']); ?>
                        </h5>
                        <?php if (!empty($selected_poi['audio'])): ?>
                        <span class="badge bg-success">
                            <i class="bi bi-check-circle me-1"></i>Đã cấu hình
                        </span>
                        <?php else: ?>
                        <span class="badge bg-warning">
                            <i class="bi bi-exclamation-circle me-1"></i>Chưa cấu hình
                        </span>
                        <?php endif; ?>
                    </div>
                    <div class="card-body">
                        <form method="POST">
                            <input type="hidden" name="poi_id" value="<?php echo $selected_poi['id']; ?>">
                            
                            <!-- Audio Type Selection -->
                            <div class="mb-4">
                                <label class="form-label fw-semibold">Loại audio:</label>
                                <div class="row g-3">
                                    <div class="col-md-6">
                                        <div class="form-check card p-3">
                                            <input class="form-check-input" type="radio" name="audio_type" value="tts" id="ttsRadio" 
                                                <?php echo (empty($selected_poi['audio']['type']) || $selected_poi['audio']['type'] === 'tts') ? 'checked' : ''; ?>>
                                            <label class="form-check-label" for="ttsRadio">
                                                <i class="bi bi-robot me-2 text-primary"></i>
                                                <strong>Text-to-Speech (TTS)</strong>
                                                <div class="small text-muted mt-1">Hệ thống đọc tự động bằng giọng AI</div>
                                            </label>
                                        </div>
                                    </div>
                                    <div class="col-md-6">
                                        <div class="form-check card p-3">
                                            <input class="form-check-input" type="radio" name="audio_type" value="file" id="fileRadio"
                                                <?php echo (!empty($selected_poi['audio']['type']) && $selected_poi['audio']['type'] === 'file') ? 'checked' : ''; ?>>
                                            <label class="form-check-label" for="fileRadio">
                                                <i class="bi bi-file-earmark-music me-2 text-primary"></i>
                                                <strong>File Audio</strong>
                                                <div class="small text-muted mt-1">Upload file MP3/WAV riêng</div>
                                            </label>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <!-- TTS Section -->
                            <div id="ttsSection" class="tts-preview mb-4">
                                <label class="form-label fw-semibold">
                                    <i class="bi bi-chat-left-text me-2"></i>Nội dung đọc (Tiếng Việt):
                                </label>
                                <textarea name="audio_text" class="form-control" rows="5" placeholder="Nhập nội dung thuyết minh về nhà hàng này..."><?php echo htmlspecialchars($selected_poi['audio']['text'] ?? $selected_poi['descriptionVi'] ?? ''); ?></textarea>
                                <div class="form-text mt-2">
                                    <i class="bi bi-info-circle me-1"></i>
                                    Hệ thống sẽ đọc nội dung này khi người dùng vào vùng nhà hàng
                                </div>
                            </div>

                            <!-- File Audio Section (hidden by default) -->
                            <div id="fileSection" class="mb-4" style="display: none;">
                                <label class="form-label fw-semibold">
                                    <i class="bi bi-link-45deg me-2"></i>URL file audio:
                                </label>
                                <input type="text" name="audio_url" class="form-control" 
                                    placeholder="https://example.com/audio.mp3"
                                    value="<?php echo htmlspecialchars($selected_poi['audio']['url'] ?? ''); ?>">
                                <div class="form-text mt-2">
                                    <i class="bi bi-info-circle me-1"></i>
                                    Nhập URL file MP3/WAV đã upload lên server/cloud
                                </div>
                            </div>

                            <!-- Settings -->
                            <div class="mb-4">
                                <div class="form-check form-switch">
                                    <input class="form-check-input" type="checkbox" id="autoPlaySwitch" name="auto_play" 
                                        <?php echo (!empty($selected_poi['audio']['autoPlay'])) ? 'checked' : ''; ?>>
                                    <label class="form-check-label" for="autoPlaySwitch">
                                        <strong>Tự động phát audio</strong> khi người dùng vào vùng nhà hàng
                                    </label>
                                </div>
                            </div>

                            <!-- Waveform Preview -->
                            <?php if (!empty($selected_poi['audio'])): ?>
                            <div class="mb-4">
                                <label class="form-label fw-semibold">
                                    <i class="bi bi-play-circle me-2"></i>Xem trước:
                                </label>
                                <div class="waveform">
                                    <i class="bi bi-volume-up"></i>
                                </div>
                                <div class="text-center mt-2">
                                    <button type="button" class="btn btn-outline-primary" onclick="previewAudio()">
                                        <i class="bi bi-play-fill me-1"></i>Nghe thử
                                    </button>
                                </div>
                            </div>
                            <?php endif; ?>

                            <!-- Submit -->
                            <div class="d-flex gap-2">
                                <button type="submit" class="btn btn-primary">
                                    <i class="bi bi-save me-1"></i>Lưu cấu hình
                                </button>
                                <a href="poi-form.php?id=<?php echo $selected_poi['id']; ?>" class="btn btn-outline-secondary">
                                    <i class="bi bi-pencil me-1"></i>Sửa thông tin nhà hàng
                                </a>
                            </div>
                        </form>
                    </div>
                </div>
                <?php else: ?>
                <div class="card">
                    <div class="card-body text-center py-5">
                        <i class="bi bi-mic text-muted" style="font-size: 4rem;"></i>
                        <h5 class="mt-3">Chọn một nhà hàng</h5>
                        <p class="text-muted">Vui lòng chọn nhà hàng từ danh sách bên trái để cấu hình audio</p>
                    </div>
                </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        // Toggle between TTS and File sections
        document.getElementById('ttsRadio')?.addEventListener('change', function() {
            document.getElementById('ttsSection').style.display = 'block';
            document.getElementById('fileSection').style.display = 'none';
        });
        
        document.getElementById('fileRadio')?.addEventListener('change', function() {
            document.getElementById('ttsSection').style.display = 'none';
            document.getElementById('fileSection').style.display = 'block';
        });
        
        // Initialize state
        if (document.getElementById('fileRadio')?.checked) {
            document.getElementById('ttsSection').style.display = 'none';
            document.getElementById('fileSection').style.display = 'block';
        }
        
        function previewAudio() {
            // In real implementation, this would play the audio
            alert('Tính năng nghe thử sẽ phát audio đã cấu hình');
        }
    </script>
</body>
</html>
