<?php
require_once 'config.php';
require_auth();

$pois = load_json($POIS_FILE);
$user = current_user();
$isOwner = $user['role'] === 'restaurant_owner';

// Filter for owner
if ($isOwner) {
    $myIds = $user['restaurantIds'] ?? [];
    $pois = array_filter($pois, fn($p) => in_array($p['id'], $myIds));
}

// Handle save
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $poiId = $_POST['poi_id'] ?? null;
    $audioType = $_POST['audio_type'] ?? 'tts';
    $audioText = $_POST['audio_text'] ?? '';
    $autoPlay = isset($_POST['auto_play']);
    $radius = $_POST['radius'] ?? 80;
    
    if ($poiId) {
        foreach ($pois as &$poi) {
            if ($poi['id'] == $poiId) {
                // Check permission for owner
                if ($isOwner && !in_array($poiId, $user['restaurantIds'] ?? [])) {
                    break;
                }
                
                $poi['audioType'] = $audioType;
                $poi['audioText'] = $audioText;
                $poi['autoPlayAudio'] = $autoPlay;
                $poi['audioRadius'] = (int)$radius;
                $poi['audioUpdatedAt'] = date('Y-m-d H:i:s');
                break;
            }
        }
        save_json($POIS_FILE, $pois);
        $success = "Đã cập nhật audio thuyết minh!";
    }
}

$selectedPoiId = $_GET['poi_id'] ?? null;
$selectedPoi = null;

if ($selectedPoiId) {
    foreach ($pois as $p) {
        if ($p['id'] == $selectedPoiId) {
            $selectedPoi = $p;
            break;
        }
    }
}

$pageTitle = $isOwner ? 'Audio nhà hàng của tôi' : 'Quản lý Audio';
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
        .audio-type-btn.active { background: #667eea; color: white; border-color: #667eea; }
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
                    <a class="nav-link active" href="audio-management.php"><i class="bi bi-mic"></i> Quản lý Audio</a>
                    <a class="nav-link" href="qr-generator.php"><i class="bi bi-qr-code"></i> QR Code</a>
                    <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <h4 class="mb-4">🎙️ <?= $pageTitle ?></h4>
                
                <?php if (isset($success)): ?>
                    <div class="alert alert-success"><?= $success ?></div>
                <?php endif; ?>

                <div class="row">
                    <!-- POI List -->
                    <div class="col-md-4">
                        <div class="card">
                            <div class="card-header fw-bold">Chọn nhà hàng</div>
                            <div class="list-group list-group-flush" style="max-height: 600px; overflow-y: auto;">
                                <?php foreach ($pois as $poi): ?>
                                <a href="?poi_id=<?= $poi['id'] ?>" 
                                   class="list-group-item list-group-item-action <?= $selectedPoiId==$poi['id']?'active':'' ?>">
                                    <div class="d-flex justify-content-between align-items-center">
                                        <div>
                                            <strong><?= htmlspecialchars($poi['nameVi'] ?? $poi['name'] ?? 'N/A') ?></strong>
                                            <br>
                                            <small class="<?= $selectedPoiId==$poi['id']?'text-white':'text-muted' ?>">
                                                <?= ($poi['audioType'] ?? '') ? '('.(($poi['audioType'] ?? '')==='tts'?'TTS':'File').')' : '(Chưa có audio)' ?>
                                            </small>
                                        </div>
                                        <?php if ($poi['autoPlayAudio'] ?? false): ?>
                                            <span class="badge bg-success">Auto</span>
                                        <?php endif; ?>
                                    </div>
                                </a>
                                <?php endforeach; ?>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Audio Config -->
                    <div class="col-md-8">
                        <?php if ($selectedPoi): ?>
                        <div class="card">
                            <div class="card-header">
                                <h5 class="mb-0">🎵 Cấu hình Audio: <?= htmlspecialchars($selectedPoi['nameVi'] ?? '') ?></h5>
                            </div>
                            <div class="card-body">
                                <form method="POST">
                                    <input type="hidden" name="poi_id" value="<?= $selectedPoi['id'] ?>">
                                    
                                    <!-- Audio Type -->
                                    <div class="mb-3">
                                        <label class="form-label fw-bold">Loại Audio</label>
                                        <div class="btn-group w-100" role="group">
                                            <input type="radio" class="btn-check" name="audio_type" id="type_tts" value="tts" 
                                                <?= ($selectedPoi['audioType'] ?? 'tts') === 'tts' ? 'checked' : '' ?>>
                                            <label class="btn btn-outline-primary audio-type-btn" for="type_tts">
                                                <i class="bi bi-mic"></i> Text-to-Speech (TTS)
                                            </label>
                                            
                                            <input type="radio" class="btn-check" name="audio_type" id="type_file" value="file"
                                                <?= ($selectedPoi['audioType'] ?? '') === 'file' ? 'checked' : '' ?>>
                                            <label class="btn btn-outline-primary audio-type-btn" for="type_file">
                                                <i class="bi bi-file-music"></i> File Audio
                                            </label>
                                        </div>
                                    </div>
                                    
                                    <!-- TTS Script -->
                                    <div id="tts_section" class="mb-3" style="display: <?= ($selectedPoi['audioType'] ?? 'tts') === 'tts' ? 'block' : 'none' ?>">
                                        <label class="form-label">Nội dung TTS</label>
                                        <textarea name="audio_text" class="form-control" rows="5" placeholder="Nhập nội dung thuyết minh..."><?= htmlspecialchars($selectedPoi['audioText'] ?? $selectedPoi['descriptionVi'] ?? '') ?></textarea>
                                        <small class="text-muted">
                                            💡 Gợi ý: Hệ thống sẽ đọc tên nhà hàng + nội dung này khi user đến gần.
                                            <br>Nếu để trống, sẽ tự động lấy từ mô tả nhà hàng.
                                        </small>
                                        <div class="mt-2">
                                            <button type="button" class="btn btn-sm btn-outline-info" onclick="previewTTS()">
                                                <i class="bi bi-play-circle"></i> Nghe thử
                                            </button>
                                        </div>
                                    </div>
                                    
                                    <!-- File Upload -->
                                    <div id="file_section" class="mb-3" style="display: <?= ($selectedPoi['audioType'] ?? '') === 'file' ? 'block' : 'none' ?>">
                                        <label class="form-label">File Audio</label>
                                        <input type="file" class="form-control" accept="audio/mp3,audio/wav">
                                        <small class="text-muted">Định dạng: MP3 hoặc WAV, tối đa 10MB</small>
                                        
                                        <?php if ($selectedPoi['audioUrl'] ?? false): ?>
                                        <div class="mt-2">
                                            <audio controls src="<?= $selectedPoi['audioUrl'] ?>" style="width: 100%;"></audio>
                                        </div>
                                        <?php endif; ?>
                                    </div>
                                    
                                    <!-- Settings -->
                                    <div class="row mb-3">
                                        <div class="col-md-6">
                                            <label class="form-label">Bán kính kích hoạt (m)</label>
                                            <input type="number" name="radius" class="form-control" value="<?= $selectedPoi['audioRadius'] ?? 80 ?>">
                                            <small class="text-muted">User vào trong vòng này sẽ trigger audio</small>
                                        </div>
                                        <div class="col-md-6">
                                            <label class="form-label">Chế độ phát</label>
                                            <div class="form-check form-switch mt-2">
                                                <input class="form-check-input" type="checkbox" name="auto_play" id="auto_play" 
                                                    <?= ($selectedPoi['autoPlayAudio'] ?? false) ? 'checked' : '' ?>>
                                                <label class="form-check-label" for="auto_play">
                                                    Tự động phát khi vào vùng
                                                </label>
                                            </div>
                                            <small class="text-muted">
                                                Nếu tắt, user phải bấm nút để nghe
                                            </small>
                                        </div>
                                    </div>
                                    
                                    <div class="d-flex gap-2">
                                        <button type="submit" class="btn btn-primary">
                                            <i class="bi bi-save"></i> Lưu cấu hình
                                        </button>
                                        <a href="audio-management.php" class="btn btn-outline-secondary">Hủy</a>
                                    </div>
                                </form>
                            </div>
                        </div>
                        
                        <!-- Preview Card -->
                        <div class="card mt-3">
                            <div class="card-header">👀 Xem trước trên App</div>
                            <div class="card-body">
                                <div class="alert alert-info">
                                    <strong>Nội dung sẽ đọc:</strong><br>
                                    "Bạn đang đến gần <?= htmlspecialchars($selectedPoi['nameVi'] ?? '') ?>. 
                                    <?= htmlspecialchars($selectedPoi['audioText'] ?? $selectedPoi['descriptionVi'] ?? '') ?>"
                                </div>
                                <div class="text-muted small">
                                    <i class="bi bi-info-circle"></i> 
                                    Audio chỉ phát khi user ở trong bán kính <?= $selectedPoi['audioRadius'] ?? 80 ?>m 
                                    và có tốc độ di chuyển phù hợp (0.5 - 20 m/s).
                                </div>
                            </div>
                        </div>
                        <?php else: ?>
                        <div class="card">
                            <div class="card-body text-center py-5 text-muted">
                                <i class="bi bi-mic fs-1"></i>
                                <p class="mt-3">Chọn một nhà hàng để cấu hình audio thuyết minh</p>
                            </div>
                        </div>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        // Toggle sections based on audio type
        document.getElementById('type_tts').addEventListener('change', function() {
            document.getElementById('tts_section').style.display = 'block';
            document.getElementById('file_section').style.display = 'none';
        });
        document.getElementById('type_file').addEventListener('change', function() {
            document.getElementById('tts_section').style.display = 'none';
            document.getElementById('file_section').style.display = 'block';
        });
        
        function previewTTS() {
            // In real implementation, this would call the TTS API
            alert('Tính năng nghe thử TTS sẽ kết nối với Google TTS API');
        }
    </script>
</body>
</html>
