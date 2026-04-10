using Microsoft.JSInterop;

namespace Diploma.Application.Services
{
    public static class TokenStorage
    {
        private static IJSRuntime? _jsRuntime;
        private static string? _cachedToken;

        public static async Task InitializeAsync(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            _cachedToken = await GetTokenFromStorageAsync();
        }

        public static async Task SaveTokenAsync(string token)
        {
            if (_jsRuntime is null) return;
            _cachedToken = token;
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
        }

        public static async Task<string?> GetTokenAsync()
        {
            if (_jsRuntime is null) return _cachedToken;
            _cachedToken = await GetTokenFromStorageAsync();
            return _cachedToken;
        }

        // ── Синхронный доступ к кешу (после InitializeAsync) ───────────────
        // Используется для декодирования JWT без async в MainLayout
        public static string? GetCachedToken() => _cachedToken;

        private static async Task<string?> GetTokenFromStorageAsync()
        {
            if (_jsRuntime is null) return null;
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_token");
        }

        public static async Task RemoveTokenAsync()
        {
            if (_jsRuntime is null) return;
            _cachedToken = null;
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
        }
    }
}