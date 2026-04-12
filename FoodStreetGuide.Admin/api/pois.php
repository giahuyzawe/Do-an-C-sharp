<?php
/**
 * FoodStreetGuide Mobile API
 * REST API endpoint for mobile app synchronization
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
$validApiKeys = ['foodstreet_mobile_2024']; // In production, use secure keys
$apiKey = $_SERVER['HTTP_AUTHORIZATION'] ?? $_GET['api_key'] ?? '';
$apiKey = str_replace('Bearer ', '', $apiKey);

if (!in_array($apiKey, $validApiKeys)) {
    http_response_code(401);
    echo json_encode(['success' => false, 'error' => 'Unauthorized']);
    exit;
}

$storageFile = '../pois.json';
$pois = [];
if (file_exists($storageFile)) {
    $pois = json_decode(file_get_contents($storageFile), true) ?: [];
}

$method = $_SERVER['REQUEST_METHOD'];

switch ($method) {
    case 'GET':
        // Get all POIs or single POI
        if (isset($_GET['id'])) {
            $id = $_GET['id'];
            $poi = array_values(array_filter($pois, fn($p) => $p['id'] == $id));
            if (count($poi) > 0) {
                echo json_encode(['success' => true, 'data' => $poi[0]]);
            } else {
                http_response_code(404);
                echo json_encode(['success' => false, 'error' => 'POI not found']);
            }
        } else {
            // Return all POIs
            echo json_encode(['success' => true, 'data' => $pois, 'count' => count($pois)]);
        }
        break;
        
    case 'POST':
        // Create new POI
        $input = json_decode(file_get_contents('php://input'), true);
        if (!$input) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'Invalid JSON']);
            exit;
        }
        
        // Generate ID if not provided
        if (!isset($input['id'])) {
            $input['id'] = time() . rand(1000, 9999);
        }
        $input['createdAt'] = date('Y-m-d H:i:s');
        $input['updatedAt'] = date('Y-m-d H:i:s');
        
        $pois[] = $input;
        file_put_contents($storageFile, json_encode($pois, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
        
        echo json_encode(['success' => true, 'data' => $input, 'message' => 'POI created']);
        break;
        
    case 'PUT':
        // Update POI
        $input = json_decode(file_get_contents('php://input'), true);
        if (!$input || !isset($input['id'])) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'Invalid JSON or missing ID']);
            exit;
        }
        
        $id = $input['id'];
        $found = false;
        
        foreach ($pois as &$poi) {
            if ($poi['id'] == $id) {
                $poi = array_merge($poi, $input);
                $poi['updatedAt'] = date('Y-m-d H:i:s');
                $found = true;
                break;
            }
        }
        
        if ($found) {
            file_put_contents($storageFile, json_encode($pois, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
            echo json_encode(['success' => true, 'data' => $input, 'message' => 'POI updated']);
        } else {
            http_response_code(404);
            echo json_encode(['success' => false, 'error' => 'POI not found']);
        }
        break;
        
    case 'DELETE':
        // Delete POI
        $id = $_GET['id'] ?? null;
        if (!$id) {
            http_response_code(400);
            echo json_encode(['success' => false, 'error' => 'ID required']);
            exit;
        }
        
        $originalCount = count($pois);
        $pois = array_values(array_filter($pois, fn($p) => $p['id'] != $id));
        
        if (count($pois) < $originalCount) {
            file_put_contents($storageFile, json_encode($pois, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
            echo json_encode(['success' => true, 'message' => 'POI deleted']);
        } else {
            http_response_code(404);
            echo json_encode(['success' => false, 'error' => 'POI not found']);
        }
        break;
        
    default:
        http_response_code(405);
        echo json_encode(['success' => false, 'error' => 'Method not allowed']);
}
