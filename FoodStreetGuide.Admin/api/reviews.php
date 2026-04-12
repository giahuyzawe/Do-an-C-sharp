<?php
/**
 * FoodStreetGuide Reviews API
 * REST API endpoint for mobile app review synchronization
 */

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization');

// Handle preflight requests
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

// Simple API key authentication
$validApiKeys = ['foodstreet_mobile_2024'];
$apiKey = $_SERVER['HTTP_AUTHORIZATION'] ?? $_GET['api_key'] ?? '';
$apiKey = str_replace('Bearer ', '', $apiKey);

if (!in_array($apiKey, $validApiKeys)) {
    http_response_code(401);
    echo json_encode(['success' => false, 'error' => 'Unauthorized']);
    exit;
}

$reviewsFile = '../reviews.json';
$reviews = [];
if (file_exists($reviewsFile)) {
    $reviews = json_decode(file_get_contents($reviewsFile), true) ?: [];
}

$poisFile = '../pois.json';
$pois = [];
if (file_exists($poisFile)) {
    $pois = json_decode(file_get_contents($poisFile), true) ?: [];
}

$method = $_SERVER['REQUEST_METHOD'];

switch ($method) {
    case 'GET':
        // Get reviews for a POI or all reviews
        if (isset($_GET['poi_id'])) {
            $poiId = intval($_GET['poi_id']);
            $poiReviews = array_filter($reviews, fn($r) => $r['poi_id'] == $poiId && $r['status'] === 'approved');
            echo json_encode(['success' => true, 'data' => array_values($poiReviews), 'count' => count($poiReviews)]);
        } else if (isset($_GET['id'])) {
            $id = intval($_GET['id']);
            $review = array_values(array_filter($reviews, fn($r) => $r['id'] == $id));
            if (count($review) > 0) {
                echo json_encode(['success' => true, 'data' => $review[0]]);
            } else {
                http_response_code(404);
                echo json_encode(['success' => false, 'error' => 'Review not found']);
            }
        } else {
            // Return all approved reviews
            $approvedReviews = array_filter($reviews, fn($r) => $r['status'] === 'approved');
            echo json_encode(['success' => true, 'data' => array_values($approvedReviews), 'count' => count($approvedReviews)]);
        }
        break;
        
    case 'POST':
        // Create new review from app
        $input = json_decode(file_get_contents('php://input'), true);
        if (!$input) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'Invalid JSON']);
            exit;
        }
        
        // Validate required fields
        if (!isset($input['poi_id']) || !isset($input['rating']) || !isset($input['comment'])) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'Missing required fields: poi_id, rating, comment']);
            exit;
        }
        
        // Validate rating
        $rating = intval($input['rating']);
        if ($rating < 1 || $rating > 5) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'Rating must be between 1 and 5']);
            exit;
        }
        
        // Get POI name
        $poiName = '';
        foreach ($pois as $poi) {
            if ($poi['id'] == $input['poi_id']) {
                $poiName = $poi['nameVi'];
                break;
            }
        }
        
        $newReview = [
            'id' => time() . rand(1000, 9999),
            'poi_id' => intval($input['poi_id']),
            'poi_name' => $poiName,
            'user_id' => $input['user_id'] ?? 'app_user_' . time(),
            'user_name' => $input['user_name'] ?? 'Người dùng App',
            'user_avatar' => $input['user_avatar'] ?? '',
            'rating' => $rating,
            'comment' => $input['comment'],
            'images' => $input['images'] ?? [],
            'status' => 'pending', // Reviews from app need moderation
            'is_spam' => false,
            'spam_reports' => 0,
            'helpful_count' => 0,
            'created_at' => date('Y-m-d H:i:s'),
            'reported_by' => [],
            'source' => 'mobile_app'
        ];
        
        $reviews[] = $newReview;
        file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
        
        // Update POI visit count or rating average if needed
        echo json_encode(['success' => true, 'data' => $newReview, 'message' => 'Review submitted for moderation']);
        break;
        
    case 'PUT':
        // Update review (helpful count, etc.)
        $input = json_decode(file_get_contents('php://input'), true);
        if (!$input || !isset($input['id'])) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'Invalid JSON or missing ID']);
            exit;
        }
        
        $id = $input['id'];
        $found = false;
        
        foreach ($reviews as &$review) {
            if ($review['id'] == $id) {
                // Only allow updating certain fields from app
                if (isset($input['helpful_count'])) {
                    $review['helpful_count'] = intval($input['helpful_count']);
                }
                $found = true;
                break;
            }
        }
        
        if ($found) {
            file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
            echo json_encode(['success' => true, 'message' => 'Review updated']);
        } else {
            http_response_code(404);
            echo json_encode(['success' => false, 'error' => 'Review not found']);
        }
        break;
        
    case 'DELETE':
        // Delete review (only by original user or admin)
        $id = $_GET['id'] ?? null;
        $userId = $_GET['user_id'] ?? null;
        
        if (!$id) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'ID required']);
            exit;
        }
        
        $originalCount = count($reviews);
        
        // Only allow deletion if user_id matches
        if ($userId) {
            $reviews = array_values(array_filter($reviews, fn($r) => !($r['id'] == $id && $r['user_id'] == $userId)));
        } else {
            $reviews = array_values(array_filter($reviews, fn($r) => $r['id'] != $id));
        }
        
        if (count($reviews) < $originalCount) {
            file_put_contents($reviewsFile, json_encode($reviews, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
            echo json_encode(['success' => true, 'message' => 'Review deleted']);
        } else {
            http_response_code(404);
            echo json_encode(['success' => false, 'error' => 'Review not found or unauthorized']);
        }
        break;
        
    default:
        http_response_code(405);
        echo json_encode(['success' => false, 'error' => 'Method not allowed']);
}
