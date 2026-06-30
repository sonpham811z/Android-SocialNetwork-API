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
    /// Gọi Claude Messages API (Anthropic) để sinh / cải thiện caption.
    /// API key được giữ ở server (config "Anthropic:ApiKey"), KHÔNG bao giờ lộ ra app Flutter.
    /// Dùng HttpClient thuần để không thêm dependency và đồng nhất với UserProfileHttpClient.
    /// </summary>
    public class AiCaptionService : IAiCaptionService
    {
        private const string AnthropicEndpoint = "https://api.anthropic.com/v1/messages";
        private const string AnthropicVersion = "2023-06-01";

        private readonly HttpClient _httpClient;
        private readonly string? _apiKey;
        private readonly string _model;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AiCaptionService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Anthropic:ApiKey"];
            // Claude Haiku 4.5 — nhanh & rẻ, hợp tác vụ viết caption ngắn.
            _model = configuration["Anthropic:Model"] ?? "claude-haiku-4-5";
        }

        public async Task<ApiResponse<AiCaptionResultDto>> GenerateCaptionAsync(AiCaptionRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return ApiResponse<AiCaptionResultDto>.ErrorResponse(
                    "Tính năng AI chưa được cấu hình (thiếu Anthropic API key).");
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
                    max_tokens = 512,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, AnthropicEndpoint)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                httpRequest.Headers.TryAddWithoutValidation("x-api-key", _apiKey);
                httpRequest.Headers.TryAddWithoutValidation("anthropic-version", AnthropicVersion);

                using var response = await _httpClient.SendAsync(httpRequest);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[AiCaptionService] Anthropic -> {(int)response.StatusCode}. Body: {body}");
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

        /// <summary>Lấy text từ content[] của Messages API (gộp mọi block type="text").</summary>
        private string ExtractText(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (!doc.RootElement.TryGetProperty("content", out var content)
                    || content.ValueKind != JsonValueKind.Array)
                {
                    return string.Empty;
                }

                var sb = new StringBuilder();
                foreach (var block in content.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var type)
                        && type.GetString() == "text"
                        && block.TryGetProperty("text", out var text))
                    {
                        sb.Append(text.GetString());
                    }
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AiCaptionService] Parse error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
