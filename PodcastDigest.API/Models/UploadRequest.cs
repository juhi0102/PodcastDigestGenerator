using Microsoft.AspNetCore.Http;

namespace PodcastDigest.API.Models
{
    public class UploadRequest
    {
        public IFormFile File { get; set; } = default!;
    }
}
