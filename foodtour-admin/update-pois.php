<?php
/**
 * Update POIs with default radius and priority
 */

require_once 'config.php';

$pois = load_json($POIS_FILE);

$updated = 0;
foreach ($pois as &$poi) {
    // Add default radius if not set
    if (!isset($poi['radius']) || $poi['radius'] <= 0) {
        $poi['radius'] = 100;  // Default 100m
    }
    
    // Add default priority if not set
    if (!isset($poi['priority']) || $poi['priority'] <= 0) {
        $poi['priority'] = 1;  // Default priority 1
    }
    
    $updated++;
}

save_json($POIS_FILE, $pois);

echo json_encode([
    'success' => true,
    'message' => "Updated $updated POIs with default radius=100m and priority=1"
]);
