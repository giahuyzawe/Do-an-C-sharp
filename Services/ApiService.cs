using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;

namespace FoodStreetGuide.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        // 🌐 NGROK URL - Dùng cho mọi thiết bị (emulator + điện thoại thật)
        // Khởi động ngrok: ngrok http 880
        // Sau đó copy URL https vào đây
        private const string BASE_URL = "https://false-awaken-uncooked.ngrok-free.dev/foodtour-admin/api";

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        /// <summary>
        /// Get all POIs from Web Admin
        /// </summary>
        public async Task<ApiResponse<POIListResponse>> GetPOIsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_URL}/get-pois.php");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<POIListResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return new ApiResponse<POIListResponse>
                {
                    Success = result?.Success ?? false,
                    Data = result,
                    Error = result?.Success == false ? result?.Error : null
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<POIListResponse>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Post analytics event to Web Admin
        /// </summary>
        public async Task<ApiResponse<object>> PostAnalyticsAsync(string type, string deviceId, int? poiId = null, string qrToken = null, string language = null)
        {
            try
            {
                // Get current language if not provided
                if (string.IsNullOrEmpty(language))
                {
                    language = Preferences.Get("AppLanguage", "vi");
                }
                
                var request = new
                {
                    type,
                    deviceId,
                    poiId,
                    qrToken,
                    language,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/post-analytics.php", request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ApiResponse<object> { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Check QR code validity
        /// </summary>
        public async Task<ApiResponse<QRCheckInResult>> CheckQRAsync(string token, string deviceId)
        {
            try
            {
                var request = new
                {
                    token,
                    deviceId
                };

                var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/check-qr.php", request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<QRCheckInResult>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ApiResponse<QRCheckInResult> { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<QRCheckInResult>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Get reviews for a POI from Web Admin
        /// </summary>
        public async Task<ApiResponse<ReviewsResponse>> GetReviewsAsync(int poiId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BASE_URL}/get-reviews.php?poiId={poiId}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<ReviewsResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ApiResponse<ReviewsResponse> { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ReviewsResponse>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Post review to Web Admin
        /// </summary>
        public async Task<ApiResponse<object>> PostReviewAsync(int poiId, string userId, string userName, int rating, string comment)
        {
            try
            {
                var request = new
                {
                    poiId,
                    userId,
                    userName,
                    rating,
                    comment,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/post-review.php", request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ApiResponse<object> { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Sync SQLite data to Web Admin (reviews, check-ins, saved POIs)
        /// </summary>
        public async Task<ApiResponse<SyncResponse>> SyncDataAsync(string deviceId, string type, object[] data)
        {
            try
            {
                var request = new
                {
                    deviceId,
                    type, // 'reviews', 'checkins', 'saved_pois'
                    data,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var response = await _httpClient.PostAsJsonAsync($"{BASE_URL}/sync-data.php", request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<SyncResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ApiResponse<SyncResponse> { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<SyncResponse>
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class POIListResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public POIApiData[] Data { get; set; }
        public string Error { get; set; }
    }

    public class POIApiData
    {
        public int Id { get; set; }
        public string NameVi { get; set; }
        public string NameEn { get; set; }
        public string Address { get; set; }
        public string DescriptionVi { get; set; }
        public string DescriptionEn { get; set; }
        public string Phone { get; set; }
        public string OpeningHours { get; set; }
        public double Rating { get; set; }
        public int VisitCount { get; set; }
        public int CheckInCount { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double Radius { get; set; } = 100;  // Default 100m
        public int Priority { get; set; } = 1;   // Default priority 1
        public string ImageUrl { get; set; }
        public string AudioUrl { get; set; }
        public string Status { get; set; }
        public bool HasTTS { get; set; }
        public bool AutoPlayAudio { get; set; }
    }

    public class QRCheckInResult
    {
        public string Token { get; set; }
        public int PoiId { get; set; }
        public string PoiName { get; set; }
        public int CheckInNumber { get; set; }
        public string Timestamp { get; set; }
    }

    public class ReviewsResponse
    {
        public bool Success { get; set; }
        public int Count { get; set; }
        public ReviewApiData[] Data { get; set; }
        public string Error { get; set; }
    }

    public class ReviewApiData
    {
        public string Id { get; set; }
        public int PoiId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string CreatedAt { get; set; }
    }

    public class SyncResponse
    {
        public int Synced { get; set; }
        public string Type { get; set; }
        public List<string> Errors { get; set; }
    }
}
