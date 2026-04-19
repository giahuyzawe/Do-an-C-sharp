<?php
/**
 * API: Post analytics event
 * Method: POST
 * Body: {type, deviceId, poiId?, timestamp}
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

// Get POST data
$input = json_decode(file_get_contents('php://input'), true);

if (!$input) {
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => 'Invalid JSON']);
    exit;
}

$type = $input['type'] ?? '';
$deviceId = $input['deviceId'] ?? '';
$poiId = $input['poiId'] ?? null;
$timestamp = $input['timestamp'] ?? date('Y-m-d H:i:s');

if (!in_array($type, ['app_visit', 'poi_view', 'check_in'])) {
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => 'Invalid type']);
    exit;
}

// Load existing analytics
$analytics = load_json($ANALYTICS_FILE);

// Create new record
$newRecord = [
    'id' => 'anl_' . uniqid(),
    'type' => $type,
    'date' => date('Y-m-d', strtotime($timestamp)),
    'timestamp' => $timestamp,
    'deviceId' => $deviceId,
    'metadata' => []
];

if ($poiId) {
    $newRecord['poiId'] = $poiId;
}

if ($type === 'check_in' && isset($input['qrToken'])) {
    $newRecord['qrToken'] = $input['qrToken'];
}

// Add to analytics
$analytics[] = $newRecord;

// Save
if (save_json($ANALYTICS_FILE, $analytics)) {
    echo json_encode([
        'success' => true,
        'id' => $newRecord['id'],
        'message' => 'Analytics recorded'
    ]);
} else {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Failed to save']);
}
