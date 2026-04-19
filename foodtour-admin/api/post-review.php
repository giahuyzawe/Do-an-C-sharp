<?php
/**
 * API: Post review from mobile app
 * Method: POST
 * Body: {poiId, userId, userName, rating, comment, timestamp}
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

$poiId = $input['poiId'] ?? null;
$userId = $input['userId'] ?? '';
$userName = $input['userName'] ?? 'Khách tham quan';
$rating = $input['rating'] ?? 0;
$comment = $input['comment'] ?? '';
$timestamp = $input['timestamp'] ?? date('Y-m-d H:i:s');

if (!$poiId || $rating < 1 || $rating > 5) {
    http_response_code(400);
    echo json_encode(['success' => false, 'error' => 'Invalid poiId or rating']);
    exit;
}

// Load reviews
$reviews = load_json($REVIEWS_FILE);

// Create new review
$newReview = [
    'id' => 'rev_' . uniqid(),
    'poiId' => $poiId,
    'userId' => $userId,
    'userName' => $userName,
    'rating' => (int)$rating,
    'comment' => $comment,
    'status' => 'approved', // Auto-approve from app
    'createdAt' => $timestamp,
    'source' => 'mobile_app'
];

$reviews[] = $newReview;

// Save reviews
if (save_json($REVIEWS_FILE, $reviews)) {
    // Update POI rating
    $pois = load_json($POIS_FILE);
    foreach ($pois as &$p) {
        if ($p['id'] == $poiId) {
            // Recalculate average rating
            $poiReviews = array_filter($reviews, fn($r) => $r['poiId'] == $poiId && $r['status'] === 'approved');
            $totalRating = array_sum(array_column($poiReviews, 'rating'));
            $count = count($poiReviews);
            $p['rating'] = $count > 0 ? round($totalRating / $count, 1) : 0;
            $p['reviewCount'] = $count;
            break;
        }
    }
    save_json($POIS_FILE, $pois);
    
    echo json_encode([
        'success' => true,
        'id' => $newReview['id'],
        'message' => 'Review added successfully'
    ]);
} else {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => 'Failed to save review']);
}
