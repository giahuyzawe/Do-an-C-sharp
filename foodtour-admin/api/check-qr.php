<?php
/**
 * API: Check QR code validity and process check-in
 * Method: POST
 * Body: {token, deviceId}
 */
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

require_once '../config.php';

$input = json_decode(file_get_contents('php://input'), true);

if (!$input) {
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => 'Invalid JSON']);
    exit;
}

$token = $input['token'] ?? '';
$deviceId = $input['deviceId'] ?? '';

if (empty($token) || empty($deviceId)) {
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => 'Missing token or deviceId']);
    exit;
}

// Load QR codes
$qrCodes = load_json($QR_CODES_FILE);
$qr = null;
foreach ($qrCodes as $q) {
    if ($q['token'] === $token) {
        $qr = $q;
        break;
    }
}

if (!$qr) {
    echo json_encode(['success' => false, 'error' => 'QR code not found']);
    exit;
}

// Check if expired
if (!empty($qr['expiresAt']) && strtotime($qr['expiresAt']) < time()) {
    echo json_encode(['success' => false, 'error' => 'QR code expired']);
    exit;
}

// Check max scans
if (!empty($qr['maxScans']) && ($qr['scanCount'] ?? 0) >= $qr['maxScans']) {
    echo json_encode(['success' => false, 'error' => 'QR code scan limit reached']);
    exit;
}

// Check if device already scanned (cooldown: 1 hour)
$oneHourAgo = date('Y-m-d H:i:s', strtotime('-1 hour'));
$existingScan = false;
foreach ($qr['scans'] ?? [] as $scan) {
    if ($scan['deviceId'] === $deviceId && $scan['timestamp'] > $oneHourAgo) {
        $existingScan = true;
        break;
    }
}

if ($existingScan) {
    echo json_encode(['success' => false, 'error' => 'Already checked in (cooldown: 1 hour)']);
    exit;
}

// Valid QR - update scan count and POI checkInCount
$scanCount = 0;
foreach ($qrCodes as &$q) {
    if ($q['token'] === $token) {
        $q['scanCount'] = ($q['scanCount'] ?? 0) + 1;
        $scanCount = $q['scanCount'];
        $q['scans'][] = [
            'deviceId' => $deviceId,
            'timestamp' => date('Y-m-d H:i:s'),
            'checkInNumber' => $q['scanCount']
        ];
        break;
    }
}

save_json($QR_CODES_FILE, $qrCodes);

// Update POI checkInCount
$pois = load_json($POIS_FILE);
foreach ($pois as &$p) {
    if ($p['id'] == $qr['poiId']) {
        $p['checkInCount'] = ($p['checkInCount'] ?? 0) + 1;
        $p['lastCheckIn'] = date('Y-m-d H:i:s');
        break;
    }
}
save_json($POIS_FILE, $pois);

// Get POI info
$pois = load_json($POIS_FILE);
$poi = null;
foreach ($pois as $p) {
    if ($p['id'] == $qr['poiId']) {
        $poi = $p;
        break;
    }
}

echo json_encode([
    'success' => true,
    'message' => 'Check-in successful',
    'data' => [
        'token' => $token,
        'poiId' => $qr['poiId'],
        'poiName' => $poi['nameVi'] ?? 'Unknown',
        'checkInNumber' => $q['scanCount'] ?? 1,
        'timestamp' => date('Y-m-d H:i:s')
    ]
]);
