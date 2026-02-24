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

        public async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
        {
            return await _http.PutAsJsonAsync(url, data);
        }

        public async Task<HttpResponseMessage> PatchAsync<T>(string url, T data)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(data)
            };
            return await _http.SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            return await _http.DeleteAsync(url);
        }

        public async Task<HttpResponseMessage> OptionsAsync(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Options, url);
            return await _http.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetRawAsync(string url)
        {
            return await _http.GetAsync(url);
        }
    }
}
