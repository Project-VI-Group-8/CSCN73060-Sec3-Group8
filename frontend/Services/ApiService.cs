using System.Net.Http.Json;

namespace frontend.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;

        public ApiService(HttpClient http)
        {
            _http = http;
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            return await _http.GetFromJsonAsync<T>(url);
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
        {
            return await _http.PostAsJsonAsync(url, data);
        }

        public async Task<HttpResponseMessage> GetRawAsync(string url)
        {
            return await _http.GetAsync(url);
        }
    }
}
