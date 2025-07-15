using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PodcastDigest.API.Models;
using PodcastDigest.Services;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PodcastDigest.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PodcastController : ControllerBase
    {
        private readonly TranscriptionService _transcriptionService;
        private readonly SummaryService _summaryService;
        private readonly string _uploadPath;

        public PodcastController(TranscriptionService transcriptionService, SummaryService summaryService)
        {
            _transcriptionService = transcriptionService;
            _summaryService = summaryService;

            _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(_uploadPath))
                Directory.CreateDirectory(_uploadPath);
        }

        // -------------------- Upload & Summarize MP3 --------------------
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPodcast([FromForm] UploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("No audio file uploaded.");

            try
            {
                var filePath = SaveUploadedFile(request.File);
                var transcript = await _transcriptionService.TranscribeAsync(filePath);
                var summary = await _summaryService.SummarizeAsync(transcript);

                return Ok(new { transcript, summary });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Upload processing failed: " + ex.Message);
                return StatusCode(500, "Error processing uploaded audio.");
            }
        }

        // -------------------- Summarize from YouTube --------------------
        [HttpPost("youtube")]
        public async Task<IActionResult> SummarizeFromYouTube([FromBody] YoutubeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest("YouTube URL is required.");

            try
            {
                Console.WriteLine("🎯 Downloading audio...");
                var audioPath = await DownloadYoutubeAudioAsync(request.Url);
                Console.WriteLine($"✅ Audio downloaded to: {audioPath}");

                Console.WriteLine("📝 Transcribing...");
                var transcript = await _transcriptionService.TranscribeAsync(audioPath);
                Console.WriteLine("✅ Transcript created.");

                Console.WriteLine("🧠 Summarizing...");
                var summary = await _summaryService.SummarizeAsync(transcript);
                Console.WriteLine("✅ Summary complete.");

                return Ok(new { transcript, summary });
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ YouTube processing failed: " + ex.Message);
                Console.WriteLine("🔎 Stack Trace: " + ex.StackTrace); // log full stack trace
                return StatusCode(500, $"Failed to summarize YouTube audio. Details: {ex.Message}");
            }

        }


        // -------------------- Helper: Save Uploaded File --------------------
        private string SaveUploadedFile(IFormFile file)
        {
            var fileName = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(_uploadPath, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            file.CopyTo(stream);

            return fullPath;
        }

        // -------------------- Helper: Download YouTube Audio --------------------
        private async Task<string> DownloadYoutubeAudioAsync(string url)
        {
            var fileName = $"yt_audio_{DateTime.Now.Ticks}.mp3";  // removed request.Url
            var outputPath = Path.Combine(_uploadPath, fileName);

            var psi = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = $"--extract-audio --audio-format mp3 -o \"{outputPath}\" \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new Exception("Failed to start yt-dlp process.");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            Console.WriteLine("🎥 yt-dlp Output:\n" + output);
            Console.WriteLine("❌ yt-dlp Error:\n" + error);

            if (!System.IO.File.Exists(outputPath))
                throw new Exception("Audio file was not downloaded.");

            return outputPath;
        }
    }
}
