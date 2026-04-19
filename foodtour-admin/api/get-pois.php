<?php
/**
 * API: Get all POIs (restaurants)
 * Method: GET
 */
header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET');

require_once '../config.php';

$pois = load_json($POIS_FILE);

// Filter approved only for public access
$approved = array_filter($pois, function($p) {
    return ($p['status'] ?? '') === 'approved';
});

// Format response
$response = [];
foreach ($approved as $poi) {
    $response[] = [
        'id' => $poi['id'],
        'nameVi' => $poi['nameVi'] ?? '',
        'nameEn' => $poi['nameEn'] ?? '',
        'address' => $poi['address'] ?? '',
        'description' => $poi['description'] ?? '',
        'rating' => $poi['rating'] ?? 0,
        'visitCount' => $poi['visitCount'] ?? 0,
        'checkInCount' => $poi['checkInCount'] ?? 0,
        'latitude' => $poi['latitude'] ?? null,
        'longitude' => $poi['longitude'] ?? null,
        'imageUrl' => $poi['imageUrl'] ?? null,
        'audioUrl' => $poi['audioUrl'] ?? null,
        'hasTTS' => ($poi['audioType'] ?? '') === 'tts',
        'autoPlayAudio' => $poi['autoPlayAudio'] ?? false
    ];
}

echo json_encode([
    'success' => true,
    'count' => count($response),
    'data' => array_values($response)
]);
