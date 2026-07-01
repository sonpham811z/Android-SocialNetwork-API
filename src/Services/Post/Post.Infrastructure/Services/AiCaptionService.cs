using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Post.Application.DTOs;
using Post.Application.Interfaces;

namespace Post.Infrastructure.Services
{
    /// <summary>
    /// Gọi Groq API (OpenAI-compatible) để sinh / cải thiện caption.
    /// API key được giữ ở server (config "Groq:ApiKey"), KHÔNG bao giờ lộ ra app Flutter.
    /// Dùng HttpClient thuần để không thêm dependency và đồng nhất với UserProfileHttpClient.
    /// </summary>
    public class AiCaptionService : IAiCaptionService
    {
        private const string GroqEndpoint = "https://api.groq.com/openai/v1/chat/completions";

        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _model;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AiCaptionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Groq:ApiKey"];
            // gpt-oss-120b trên Groq — nhanh & rẻ, hợp tác vụ viết caption ngắn.
            _model = configuration["Groq:Model"] ?? "openai/gpt-oss-120b";
        }

        public async Task<ApiResponse<AiCaptionResultDto>> GenerateCaptionAsync(AiCaptionRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return ApiResponse<AiCaptionResultDto>.ErrorResponse(
                    "Tính năng AI chưa được cấu hình (thiếu Groq API key).");
            }

            var isImprove = string.Equals(request.Mode, "improve", StringComparison.OrdinalIgnoreCase);
            var source = isImprove ? request.ExistingContent : request.Topic;
            if (string.IsNullOrWhiteSpace(source))
            {
                return ApiResponse<AiCaptionResultDto>.ErrorResponse(
                    isImprove ? "Chưa có nội dung để cải thiện." : "Hãy nhập chủ đề bạn muốn viết.");
            }

            const string systemPrompt =
                "Bạn là trợ lý viết caption cho mạng xã hội Zest. " +
                "Viết caption bằng tiếng Việt, tự nhiên, hấp dẫn, tối đa 2-3 câu, " +
                "có thể thêm vài emoji phù hợp. " +
                "CHỈ trả về nội dung caption, không thêm lời dẫn, không dấu ngoặc kép, không giải thích.";

            var userPrompt = isImprove
                ? $"Viết lại đoạn sau cho hay và cuốn hút hơn, giữ nguyên ý chính:\n\n{source}"
                : $"Viết một caption cho bài đăng về chủ đề: {source}";

            try
            {
                var payload = new
                {
                    model = _model,
                    max_completion_tokens = 1024,
                    // gpt-oss là model reasoning — để "low" cho nhanh & rẻ với tác vụ ngắn.
                    reasoning_effort = "low",
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    }
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, GroqEndpoint)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                using var response = await _httpClient.SendAsync(httpRequest);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AiCaptionService] Groq -> {(int)response.StatusCode}. Body: {body}");
                    return ApiResponse<AiCaptionResultDto>.ErrorResponse("Không tạo được caption, vui lòng thử lại.");
                }

                var caption = ExtractText(body);
                if (string.IsNullOrWhiteSpace(caption))
                {
                    return ApiResponse<AiCaptionResultDto>.ErrorResponse("AI không trả về nội dung, vui lòng thử lại.");
                }

                return ApiResponse<AiCaptionResultDto>.SuccessResponse(
                    new AiCaptionResultDto { Caption = caption.Trim() });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiCaptionService] Error: {ex.Message}");
                return ApiResponse<AiCaptionResultDto>.ErrorResponse("Lỗi kết nối tới dịch vụ AI.");
            }
        }

        /// <summary>Lấy text từ choices[0].message.content (định dạng OpenAI/Groq).</summary>
        private string ExtractText(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (!doc.RootElement.TryGetProperty("choices", out var choices)
                    || choices.ValueKind != JsonValueKind.Array
                    || choices.GetArrayLength() == 0)
                {
                    return string.Empty;
                }

                var first = choices[0];
                if (first.TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var content)
                    && content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString() ?? string.Empty;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiCaptionService] Parse error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
