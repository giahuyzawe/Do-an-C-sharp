<?php
/**
 * API: Get reviews for a POI
 * Method: GET
 * Params: poiId
 */
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, OPTIONS');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

require_once '../config.php';

$poiId = $_GET['poiId'] ?? null;

// Load reviews
$reviews = load_json($REVIEWS_FILE);

// Filter by poiId if provided
if ($poiId) {
    $reviews = array_filter($reviews, fn($r) => ($r['poiId'] ?? 0) == $poiId);
}

// Only approved reviews
$reviews = array_filter($reviews, fn($r) => ($r['status'] ?? 'pending') === 'approved');

// Sort by newest
usort($reviews, function($a, $b) {
    return strtotime($b['createdAt'] ?? '0') - strtotime($a['createdAt'] ?? '0');
});

// Format response
$formatted = [];
foreach ($reviews as $r) {
    $formatted[] = [
        'id' => $r['id'],
        'poiId' => $r['poiId'],
        'userId' => $r['userId'] ?? '',
        'userName' => $r['userName'] ?? 'Khách tham quan',
        'rating' => (int)($r['rating'] ?? 0),
        'comment' => $r['comment'] ?? '',
        'createdAt' => $r['createdAt'] ?? date('Y-m-d H:i:s')
    ];
}

echo json_encode([
    'success' => true,
    'count' => count($formatted),
    'data' => $formatted
]);
