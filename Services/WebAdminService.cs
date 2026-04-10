using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FoodStreetGuide.Models;

namespace FoodStreetGuide.Services;

public interface IWebAdminService
{
    Task<List<POI>> GetAllPOIsAsync();
    Task<POI> GetPOIAsync(int id);
    Task<POI> CreatePOIAsync(POI poi);
    Task<POI> UpdatePOIAsync(int id, POI poi);
    Task<bool> DeletePOIAsync(int id);
    Task SyncFromWebAdminAsync();
}

public class WebAdminService : IWebAdminService
{
    private readonly HttpClient _httpClient;
    private readonly IDatabaseService _databaseService;
    private const string API_BASE_URL = "http://10.0.2.2/FoodStreetGuide.Admin/api";

    public WebAdminService(IDatabaseService databaseService)
    {
        _databaseService = databaseService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<List<POI>> GetAllPOIsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{API_BASE_URL}/pois.php");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<POI>>>();
            return result?.Data ?? new List<POI>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching POIs: {ex.Message}");
            return new List<POI>();
        }
    }

    public async Task<POI> GetPOIAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{API_BASE_URL}/pois.php/{id}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<POI>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching POI {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<POI> CreatePOIAsync(POI poi)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{API_BASE_URL}/pois.php", poi);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<POI>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating POI: {ex.Message}");
            return null;
        }
    }

    public async Task<POI> UpdatePOIAsync(int id, POI poi)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{API_BASE_URL}/pois.php/{id}", poi);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<POI>>();
            return result?.Data;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error updating POI {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeletePOIAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{API_BASE_URL}/pois.php/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting POI {id}: {ex.Message}");
            return false;
        }
    }

    public async Task SyncFromWebAdminAsync()
    {
        try
        {
            var remotePOIs = await GetAllPOIsAsync();
            foreach (var poi in remotePOIs)
            {
                await _databaseService.SavePOIAsync(poi);
            }
            System.Diagnostics.Debug.WriteLine($"Synced {remotePOIs.Count} POIs");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Sync error: {ex.Message}");
        }
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Error { get; set; }
}
