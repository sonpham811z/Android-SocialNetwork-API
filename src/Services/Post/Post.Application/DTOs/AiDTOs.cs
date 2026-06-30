using System.ComponentModel.DataAnnotations;

namespace Post.Application.DTOs
{
    /// <summary>
    /// Yêu cầu sinh / cải thiện caption bằng AI.
    /// Mode = "generate": tạo caption mới từ <see cref="Topic"/>.
    /// Mode = "improve":  viết lại / làm hay hơn <see cref="ExistingContent"/>.
    /// </summary>
    public class AiCaptionRequestDto
    {
        /// <summary>"generate" hoặc "improve". Mặc định "generate".</summary>
        public string Mode { get; set; } = "generate";

        /// <summary>Chủ đề/ý tưởng người dùng muốn viết (dùng cho mode generate).</summary>
        [MaxLength(500)]
        public string? Topic { get; set; }

        /// <summary>Nội dung đang gõ dở (dùng cho mode improve).</summary>
        [MaxLength(2000)]
        public string? ExistingContent { get; set; }
    }

    public class AiCaptionResultDto
    {
        public string Caption { get; set; } = string.Empty;
    }
}
