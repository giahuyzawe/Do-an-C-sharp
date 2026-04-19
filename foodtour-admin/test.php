<?php
/**
 * Test file - Kiểm tra PHP hoạt động
 */
echo "<h1>✅ PHP đang chạy!</h1>";
echo "<p>Phiên bản PHP: " . phpversion() . "</p>";
echo "<p>Thời gian server: " . date('Y-m-d H:i:s') . "</p>";

// Test JSON functions
$test = ['status' => 'OK', 'message' => 'PHP hoạt động bình thường'];
echo "<pre>";
echo json_encode($test, JSON_PRETTY_PRINT);
echo "</pre>";

// Test file permissions
echo "<h2>🔧 Kiểm tra quyền:</h2>";
echo "<ul>";
echo "<li>data/ đọc được: " . (is_readable('data/') ? '✅' : '❌') . "</li>";
echo "<li>data/ ghi được: " . (is_writable('data/') ? '✅' : '❌') . "</li>";
echo "</ul>";

// Check if files exist
echo "<h2>📁 Kiểm tra files:</h2>";
$files = ['config.php', 'index.php', 'login.php', 'data/pois.json', 'data/users.json'];
echo "<ul>";
foreach ($files as $file) {
    $exists = file_exists($file);
    echo "<li>$file: " . ($exists ? '✅' : '❌') . "</li>";
}
echo "</ul>";

echo "<hr>";
echo "<p><a href='index.php'>➡️ Vào Dashboard</a></p>";
