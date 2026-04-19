<?php
require_once 'config.php';

echo "<h1>Debug Info</h1>";

echo "<h2>Session</h2>";
echo "<pre>";
print_r($_SESSION);
echo "</pre>";

echo "<h2>Users File</h2>";
$users = load_json($USERS_FILE);
echo "<pre>";
print_r($users);
echo "</pre>";

echo "<h2>Test Password Verify</h2>";
$testPass = 'admin123';
$hash = password_hash($testPass, PASSWORD_DEFAULT);
echo "Password: $testPass<br>";
echo "Hash: $hash<br>";
echo "Verify: " . (password_verify($testPass, $hash) ? 'TRUE' : 'FALSE') . "<br>";

// Test with stored hash
if (!empty($users)) {
    echo "<h2>Test Stored Hash</h2>";
    $storedHash = $users[0]['password'] ?? '';
    echo "Stored hash: $storedHash<br>";
    echo "Verify with 'admin123': " . (password_verify('admin123', $storedHash) ? 'TRUE' : 'FALSE') . "<br>";
}

echo "<hr><a href='login.php'>Go to Login</a>";
