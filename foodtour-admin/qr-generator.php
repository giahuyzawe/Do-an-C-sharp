<?php
require_once 'config.php';
require_auth();

$pois = load_json($POIS_FILE);
$qrCodes = load_json($QR_CODES_FILE);

$user = current_user();
$isOwner = $user['role'] === 'restaurant_owner';

// Filter POIs for owner
if ($isOwner) {
    $pois = array_filter($pois, fn($p) => ($p['ownerId'] ?? '') === $user['id'] && ($p['status'] ?? '') === 'approved');
} else {
    $pois = array_filter($pois, fn($p) => ($p['status'] ?? '') === 'approved');
}

// Handle generate QR
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $poiId = $_POST['poi_id'] ?? null;
    $qrType = $_POST['qr_type'] ?? 'unlimited';
    $expiresHours = $_POST['expires_hours'] ?? 24;
    $maxScans = $_POST['max_scans'] ?? null;
    $note = $_POST['note'] ?? '';
    
    if ($poiId) {
        // Generate unique token
        $token = 'ft-' . date('Ymd') . '-' . bin2hex(random_bytes(6));
        
        $qrCode = [
            'id' => generate_id('qr_'),
            'token' => $token,
            'poiId' => (int)$poiId,
            'type' => $qrType,
            'createdAt' => date('Y-m-d H:i:s'),
            'expiresAt' => $qrType === 'time_limited' ? date('Y-m-d H:i:s', strtotime("+$expiresHours hours")) : null,
            'maxScans' => $qrType === 'single_use' ? 1 : ($qrType === 'limited' ? (int)$maxScans : null),
            'scanCount' => 0,
            'isUsed' => false,
            'createdBy' => $user['id'],
            'note' => $note,
            'status' => 'active'
        ];
        
        $qrCodes[] = $qrCode;
        save_json($QR_CODES_FILE, $qrCodes);
        
        $success = "Đã tạo QR Code thành công!";
        $newQR = $qrCode;
    }
}

// Handle delete
if (isset($_GET['delete'])) {
    $deleteId = $_GET['delete'];
    $qrCodes = array_filter($qrCodes, fn($q) => $q['id'] !== $deleteId);
    save_json($QR_CODES_FILE, array_values($qrCodes));
    header('Location: qr-generator.php');
    exit;
}

// Get QR codes for display (filter by creator if owner)
$displayQRCodes = $isOwner 
    ? array_filter($qrCodes, fn($q) => $q['createdBy'] === $user['id'])
    : $qrCodes;

// Sort by newest
usort($displayQRCodes, fn($a, $b) => strtotime($b['createdAt']) - strtotime($a['createdAt']));
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>Tạo QR Code - Food Tour</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.0/font/bootstrap-icons.css">
    <script src="https://cdnjs.cloudflare.com/ajax/libs/qrcodejs/1.0.0/qrcode.min.js"></script>
    <style>
        .sidebar { min-height: 100vh; background: #1e293b; color: white; }
        .sidebar .nav-link { color: #94a3b8; padding: 12px 20px; }
        .sidebar .nav-link:hover, .sidebar .nav-link.active { color: white; background: rgba(255,255,255,0.1); }
        .qr-card { border: 2px dashed #dee2e6; border-radius: 12px; padding: 20px; text-align: center; }
        .qr-card.active { border-color: #667eea; background: #f8f9ff; }
        .qr-display { margin: 20px auto; }
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
                    <a class="nav-link" href="audio-management.php"><i class="bi bi-mic"></i> Audio</a>
                    <a class="nav-link active" href="qr-generator.php"><i class="bi bi-qr-code"></i> QR Code</a>
                    <a class="nav-link" href="analytics.php"><i class="bi bi-graph-up"></i> Phân tích</a>
                    <a class="nav-link text-danger" href="logout.php"><i class="bi bi-box-arrow-right"></i> Đăng xuất</a>
                </nav>
            </div>
            
            <div class="col-md-10 p-4">
                <h4 class="mb-4">📱 Tạo QR Code động</h4>
                
                <?php if (isset($success)): ?>
                    <div class="alert alert-success d-flex justify-content-between align-items-center">
                        <span><?= $success ?></span>
                        <button type="button" class="btn btn-sm btn-outline-success" onclick="location.reload()">Tạo thêm</button>
                    </div>
                <?php endif; ?>

                <div class="row">
                    <!-- Generate Form -->
                    <div class="col-md-5">
                        <div class="card">
                            <div class="card-header fw-bold">⚙️ Cấu hình QR Code</div>
                            <div class="card-body">
                                <form method="POST" id="qrForm">
                                    <div class="mb-3">
                                        <label class="form-label">Chọn nhà hàng *</label>
                                        <select name="poi_id" class="form-select" required>
                                            <option value="">-- Chọn nhà hàng --</option>
                                            <?php foreach ($pois as $poi): ?>
                                            <option value="<?= $poi['id'] ?>">
                                                <?= htmlspecialchars($poi['nameVi'] ?? $poi['name']) ?> 
                                                (<?= $poi['visitCount'] ?? 0 ?> lượt xem)
                                            </option>
                                            <?php endforeach; ?>
                                        </select>
                                    </div>
                                    
                                    <div class="mb-3">
                                        <label class="form-label">Loại QR Code</label>
                                        <div class="row g-2">
                                            <div class="col-6">
                                                <div class="form-check">
                                                    <input class="form-check-input" type="radio" name="qr_type" id="type_unlimited" value="unlimited" checked onchange="toggleOptions()">
                                                    <label class="form-check-label" for="type_unlimited">
                                                        <strong>Không giới hạn</strong>
                                                        <br><small class="text-muted">Dùng mãi mãi</small>
                                                    </label>
                                                </div>
                                            </div>
                                            <div class="col-6">
                                                <div class="form-check">
                                                    <input class="form-check-input" type="radio" name="qr_type" id="type_time" value="time_limited" onchange="toggleOptions()">
                                                    <label class="form-check-label" for="type_time">
                                                        <strong>Có thời hạn</strong>
                                                        <br><small class="text-muted">Hết hạn sau X giờ</small>
                                                    </label>
                                                </div>
                                            </div>
                                            <div class="col-6">
                                                <div class="form-check">
                                                    <input class="form-check-input" type="radio" name="qr_type" id="type_single" value="single_use" onchange="toggleOptions()">
                                                    <label class="form-check-label" for="type_single">
                                                        <strong>Chỉ 1 lần</strong>
                                                        <br><small class="text-muted">Dùng 1 lần duy nhất</small>
                                                    </label>
                                                </div>
                                            </div>
                                            <div class="col-6">
                                                <div class="form-check">
                                                    <input class="form-check-input" type="radio" name="qr_type" id="type_limited" value="limited" onchange="toggleOptions()">
                                                    <label class="form-check-label" for="type_limited">
                                                        <strong>Giới hạn số lần</strong>
                                                        <br><small class="text-muted">Tối đa X lần quét</small>
                                                    </label>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <!-- Time limited options -->
                                    <div id="timeOptions" class="mb-3" style="display:none;">
                                        <label class="form-label">Thời hạn (giờ)</label>
                                        <input type="number" name="expires_hours" class="form-control" value="24" min="1" max="168">
                                    </div>
                                    
                                    <!-- Limited scans options -->
                                    <div id="limitedOptions" class="mb-3" style="display:none;">
                                        <label class="form-label">Số lần quét tối đa</label>
                                        <input type="number" name="max_scans" class="form-control" value="10" min="1">
                                    </div>
                                    
                                    <div class="mb-3">
                                        <label class="form-label">Ghi chú</label>
                                        <input type="text" name="note" class="form-control" placeholder="VD: QR cho sự kiện Tết 2024">
                                    </div>
                                    
                                    <button type="submit" class="btn btn-primary w-100">
                                        <i class="bi bi-qr-code"></i> Tạo QR Code
                                    </button>
                                </form>
                            </div>
                        </div>
                    </div>
                    
                    <!-- QR Preview & List -->
                    <div class="col-md-7">
                        <?php if (isset($newQR)): ?>
                        <div class="qr-card active mb-4">
                            <h5 class="text-success mb-3">🎉 QR Code mới tạo!</h5>
                            <div id="qrcode" class="qr-display"></div>
                            <div class="mt-3">
                                <p class="mb-1"><strong>Token:</strong> <code><?= $newQR['token'] ?></code></p>
                                <p class="mb-1"><strong>Loại:</strong> <?= $newQR['type'] ?></p>
                                <p class="mb-1 text-muted" style="font-size: 0.75rem; word-break: break-all;">
                                    <?= $BASE_URL ?>/qr-redirect.php?token=<?= $newQR['token'] ?>
                                </p>
                                <small class="text-success">
                                    <i class="bi bi-check-circle"></i> Hỗ trợ cả 2 chế độ: Có app / Không có app
                                </small>
                            </div>
                            <div class="mt-3 d-flex gap-2 justify-content-center">
                                <button class="btn btn-sm btn-outline-primary" onclick="downloadQR()">
                                    <i class="bi bi-download"></i> Tải ảnh
                                </button>
                                <button class="btn btn-sm btn-outline-secondary" onclick="window.print()">
                                    <i class="bi bi-printer"></i> In
                                </button>
                            </div>
                            <script>
                                // Use web URL for universal access (works with or without app)
                                const qrUrl = '<?= $BASE_URL ?>/qr-redirect.php?token=<?= $newQR['token'] ?>';
                                
                                new QRCode(document.getElementById('qrcode'), {
                                    text: qrUrl,
                                    width: 200,
                                    height: 200,
                                    colorDark: '#000000',
                                    colorLight: '#ffffff'
                                });
                                
                                function downloadQR() {
                                    const canvas = document.querySelector('#qrcode canvas');
                                    const link = document.createElement('a');
                                    link.download = 'qr-<?= $newQR['token'] ?>.png';
                                    link.href = canvas.toDataURL();
                                    link.click();
                                }
                            </script>
                        </div>
                        <?php endif; ?>
                        
                        <!-- QR List -->
                        <div class="card">
                            <div class="card-header d-flex justify-content-between align-items-center">
                                <span class="fw-bold">📋 QR Code đã tạo</span>
                                <span class="badge bg-primary"><?= count($displayQRCodes) ?> mã</span>
                            </div>
                            <div class="table-responsive" style="max-height: 400px; overflow-y: auto;">
                                <table class="table table-hover table-sm mb-0">
                                    <thead class="table-light">
                                        <tr>
                                            <th>Nhà hàng</th>
                                            <th>Loại</th>
                                            <th>Đã quét</th>
                                            <th>Trạng thái</th>
                                            <th>Tạo lúc</th>
                                            <th></th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <?php foreach (array_slice($displayQRCodes, 0, 10) as $qr): 
                                            $poi = array_filter($pois, fn($p) => $p['id'] == $qr['poiId']);
                                            $poi = $poi ? array_values($poi)[0] : null;
                                            $isExpired = $qr['expiresAt'] && strtotime($qr['expiresAt']) < time();
                                            $isMaxed = $qr['maxScans'] && $qr['scanCount'] >= $qr['maxScans'];
                                        ?>
                                        <tr>
                                            <td><?= htmlspecialchars($poi['nameVi'] ?? 'N/A') ?></td>
                                            <td>
                                                <span class="badge bg-<?= $qr['type']==='unlimited'?'success':($qr['type']==='single_use'?'danger':'warning') ?>">
                                                    <?= $qr['type'] ?>
                                                </span>
                                            </td>
                                            <td><?= $qr['scanCount'] ?>/<?= $qr['maxScans'] ?? '∞' ?></td>
                                            <td>
                                                <?php if ($isExpired): ?>
                                                    <span class="badge bg-secondary">Hết hạn</span>
                                                <?php elseif ($isMaxed): ?>
                                                    <span class="badge bg-dark">Hết lượt</span>
                                                <?php else: ?>
                                                    <span class="badge bg-success">Hoạt động</span>
                                                <?php endif; ?>
                                            </td>
                                            <td><?= date('d/m H:i', strtotime($qr['createdAt'])) ?></td>
                                            <td>
                                                <a href="?delete=<?= $qr['id'] ?>" class="btn btn-sm btn-outline-danger" onclick="return confirm('Xóa QR này?')">
                                                    <i class="bi bi-trash"></i>
                                                </a>
                                            </td>
                                        </tr>
                                        <?php endforeach; ?>
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js"></script>
    <script>
        function toggleOptions() {
            const type = document.querySelector('input[name="qr_type"]:checked').value;
            document.getElementById('timeOptions').style.display = type === 'time_limited' ? 'block' : 'none';
            document.getElementById('limitedOptions').style.display = type === 'limited' ? 'block' : 'none';
        }
    </script>
</body>
</html>
