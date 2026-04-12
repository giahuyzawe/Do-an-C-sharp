<?php
// Google Cloud Text-to-Speech API endpoint
// Requires Google Cloud API Key

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');

// Configuration - User needs to set their API key
$GOOGLE_API_KEY = $_ENV['GOOGLE_TTS_API_KEY'] ?? ''; // Or set directly here

if (empty($GOOGLE_API_KEY)) {
    http_response_code(500);
    echo json_encode(['error' => 'Google API Key not configured']);
    exit;
}

// Get text from request
$input = json_decode(file_get_contents('php://input'), true);
$text = $input['text'] ?? '';
$voiceName = $input['voice'] ?? 'vi-VN-Standard-A';

if (empty($text)) {
    http_response_code(400);
    echo json_encode(['error' => 'Text is required']);
    exit;
}

// Google Cloud TTS API endpoint
$url = "https://texttospeech.googleapis.com/v1/text:synthesize?key={$GOOGLE_API_KEY}";

// Request payload
$data = [
    'input' => ['text' => $text],
    'voice' => [
        'languageCode' => 'vi-VN',
        'name' => $voiceName,
        'ssmlGender' => 'FEMALE'
    ],
    'audioConfig' => [
        'audioEncoding' => 'MP3',
        'speakingRate' => 0.9,
        'pitch' => 0.0
    ]
];

// Make API request
$ch = curl_init($url);
curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($data));
curl_setopt($ch, CURLOPT_HTTPHEADER, [
    'Content-Type: application/json'
]);

$response = curl_exec($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
curl_close($ch);

if ($httpCode === 200) {
    $result = json_decode($response, true);
    if (isset($result['audioContent'])) {
        echo json_encode([
            'success' => true,
            'audioContent' => $result['audioContent'],
            'voice' => $voiceName
        ]);
    } else {
        http_response_code(500);
        echo json_encode(['error' => 'Invalid response from Google API']);
    }
} else {
    http_response_code($httpCode);
    echo json_encode(['error' => 'Google API error', 'details' => json_decode($response, true)]);
}
