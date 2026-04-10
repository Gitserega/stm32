using Diploma.DTO;
using Diploma.DTO.Auth;
using Diploma.DTO.Config;
using Diploma.DTO.History;
using Diploma.DTO.User;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Diploma.Application.Services
{
    public class ApiService : IApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        private async Task<HttpClient> CreateClientAsync()
        {
            // ✅ Имя совпадает с регистрацией в Program.cs: AddHttpClient("AttendanceAPI", ...)
            var client = _httpClientFactory.CreateClient("AttendanceAPI");
            var token = await TokenStorage.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<LoginResponse?> LoginAsync(string login, string password)
        {
            var client = await CreateClientAsync();

            var request = new LoginRequest { Login = login, Password = password };
            var content = new StringContent(
                JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/auth/login", content);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<LoginResponse>(json, _jsonOptions);
        }
        public async Task<List<HistoryDataPoint>?> GetHistoryDataAsync(DateTime start, DateTime end, string parameter)
        {
            var client = await CreateClientAsync();
            var startUtc = start.ToUniversalTime();
            var endUtc = end.ToUniversalTime();
            var url = $"/api/history/data?start={startUtc:yyyy-MM-ddTHH:mm:ssZ}&end={endUtc:yyyy-MM-ddTHH:mm:ssZ}&parameter={parameter}";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<HistoryDataPoint>>(json, _jsonOptions);
        }
        // ApiService.cs
        public async Task<List<AlertDto>?> GetAlertsAsync(DateTime? from, DateTime? to)
        {
            var client = await CreateClientAsync();
            var url = "/api/alerts";
            var parameters = new List<string>();
            if (from.HasValue)
                parameters.Add($"from={from.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (to.HasValue)
                parameters.Add($"to={to.Value:yyyy-MM-ddTHH:mm:ssZ}");
            if (parameters.Any())
                url += "?" + string.Join("&", parameters);

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<AlertDto>>(json, _jsonOptions);
        }
        public async Task<List<UserDto>?> GetUsersAsync()
        {
            var client = await CreateClientAsync();
            var response = await client.GetAsync("/api/admin/users");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<UserDto>>(json, _jsonOptions);
        }

        public async Task<bool> CreateUserAsync(CreateUserRequest request)
        {
            var client = await CreateClientAsync();
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/admin/users", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteUserAsync(long userId)
        {
            var client = await CreateClientAsync();
            var response = await client.DeleteAsync($"/api/admin/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> UpdateUserAsync(long userId, UpdateUserRequest request)
        {
            var client = await CreateClientAsync();
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"/api/admin/users/{userId}", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<List<DeviceThresholdDto>?> GetDevicesThresholdsAsync()
        {
            var client = await CreateClientAsync();
            var response = await client.GetAsync("/api/admin/devices/thresholds");
            if (!response.IsSuccessStatusCode) return null;
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<DeviceThresholdDto>>(json, _jsonOptions);
        }

        public async Task<bool> UpdateThresholdsAsync(UpdateThresholdsRequest request)
        {
            var client = await CreateClientAsync();
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PutAsync("/api/admin/devices/thresholds", content);
            return response.IsSuccessStatusCode;
        }
        public async Task<bool> CreateDeviceAsync(CreateDeviceRequest request)
        {
            var client = await CreateClientAsync();
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/admin/devices", content);
            return response.IsSuccessStatusCode;
        }
    }

}