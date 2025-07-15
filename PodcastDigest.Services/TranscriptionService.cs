using System.Diagnostics;
using System.Text.Json;

namespace PodcastDigest.Services
{
    public class TranscriptionService
    {
        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            Console.WriteLine($"🎧 Starting transcription for: {audioFilePath}");

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"C:\\Users\\creat\\PodcastDigestGenerator\\PythonScripts\\whisper_transcribe.py\" \"{audioFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                throw new Exception("❌ Failed to start Whisper Python process.");

            var stdout = await process.StandardOutput.ReadToEndAsync();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            Console.WriteLine("🟢 Whisper STDOUT:\n" + stdout);
            Console.WriteLine("🔴 Whisper STDERR:\n" + stderr);

            if (string.IsNullOrWhiteSpace(stdout))
                throw new Exception("❌ Whisper returned empty output. Check script, Python environment, or input file.");

            try
            {
                var json = JsonDocument.Parse(stdout);
                var transcript = json.RootElement.GetProperty("transcript").GetString();

                if (string.IsNullOrWhiteSpace(transcript))
                    throw new Exception("❌ Transcript was empty or missing from Whisper output.");

                return transcript;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("❌ Failed to parse Whisper JSON output.");
                throw new Exception("❌ Whisper output was not valid JSON.", ex);
            }
        }
    }
}
