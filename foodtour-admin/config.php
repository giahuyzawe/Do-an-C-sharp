<?php
/**
 * Food Tour Admin - Configuration
 */

// Base URL - HARD CODE ngrok URL for now
// TODO: Change back to localhost when done testing
$BASE_URL = 'https://false-awaken-uncooked.ngrok-free.dev/foodtour-admin';

// Original auto-detect (commented for now):
// $protocol = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ? 'https' : 'http';
// $host = $_SERVER['HTTP_HOST'] ?? 'localhost';
// if (strpos($host, 'ngrok') !== false) {
//     $BASE_URL = $protocol . '://' . $host . '/foodtour-admin';
// } else {
//     $BASE_URL = 'http://localhost/foodtour-admin';
// }

// Data directories
$DATA_DIR = __DIR__ . '/data';
$UPLOADS_DIR = __DIR__ . '/uploads';

// Create dirs
if (!is_dir($DATA_DIR)) mkdir($DATA_DIR, 0755, true);
if (!is_dir($UPLOADS_DIR)) mkdir($UPLOADS_DIR, 0755, true);

// Data files
$POIS_FILE = $DATA_DIR . '/pois.json';
$USERS_FILE = $DATA_DIR . '/users.json';
$ACTIVITIES_FILE = $DATA_DIR . '/activities.json';
$ANALYTICS_FILE = $DATA_DIR . '/analytics.json';
$REVIEWS_FILE = $DATA_DIR . '/reviews.json';
$QR_CODES_FILE = $DATA_DIR . '/qr_codes.json';

// Init files
foreach ([$POIS_FILE, $USERS_FILE, $ACTIVITIES_FILE, $ANALYTICS_FILE, $REVIEWS_FILE, $QR_CODES_FILE] as $f) {
    if (!file_exists($f)) file_put_contents($f, '[]');
}

// Session
if (session_status() === PHP_SESSION_NONE) session_start();

// Helper functions
function load_json($file) {
    return file_exists($file) ? json_decode(file_get_contents($file), true) ?: [] : [];
}

function save_json($file, $data) {
    file_put_contents($file, json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));
}

function require_auth() {
    if (!isset($_SESSION['user'])) {
        header('Location: login.php');
        exit;
    }
}

function require_admin() {
    require_auth();
    if ($_SESSION['user']['role'] !== 'admin' && $_SESSION['user']['role'] !== 'superadmin') {
        header('Location: index.php');
        exit;
    }
}

function generate_id($prefix = '') {
    return $prefix . date('Ymd') . bin2hex(random_bytes(4));
}

// Get current user
function current_user() {
    return $_SESSION['user'] ?? null;
}

// Check if user is restaurant owner
function is_restaurant_owner() {
    $user = current_user();
    return $user && $user['role'] === 'restaurant_owner';
}

// Get restaurants owned by current user
function get_my_restaurants() {
    global $POIS_FILE;
    if (!is_restaurant_owner()) return [];
    $user = current_user();
    $pois = load_json($POIS_FILE);
    return array_filter($pois, fn($p) => ($p['ownerId'] ?? '') === $user['id']);
}

// Record analytics
function record_analytics($type, $data) {
    global $ANALYTICS_FILE;
    $analytics = load_json($ANALYTICS_FILE);
    $analytics[] = [
        'id' => generate_id('anl_'),
        'type' => $type,
        'timestamp' => date('Y-m-d H:i:s'),
        'date' => date('Y-m-d'),
        'data' => $data
    ];
    save_json($ANALYTICS_FILE, $analytics);
}
