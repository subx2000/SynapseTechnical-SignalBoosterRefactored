
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SignalBoosterRefactored.Interfaces;

namespace SignalBoosterRefactored.Services
{
    /// <summary>
    /// Handles reading physician notes from files with support for multiple formats
    /// </summary>
    public class PhysicianNoteReader : IPhysicianNoteReader
    {
        private readonly ILogger<PhysicianNoteReader> _logger;
        private const string FallbackNote = "Patient needs a CPAP with full face mask and humidifier. AHI > 20. Ordered by Dr. Cameron.";

        public PhysicianNoteReader(ILogger<PhysicianNoteReader> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> ReadPhysicianNoteAsync(string filePath = null)
        {
            var targetPath = filePath ?? "physician_note.txt";
            
            try
            {
                if (!File.Exists(targetPath))
                {
                    _logger.LogWarning("Physician note file not found at path: {FilePath}. Using fallback content.", targetPath);
                    return FallbackNote;
                }

                _logger.LogInformation("Reading physician note from file: {FilePath}", targetPath);
                var fileContent = await File.ReadAllTextAsync(targetPath);
                
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    _logger.LogWarning("Physician note file is empty. Using fallback content.");
                    return FallbackNote;
                }

                // Handle JSON-wrapped notes (stretch goal implementation)
                var processedContent = ProcessNoteContent(fileContent);
                _logger.LogDebug("Successfully processed physician note content");
                
                return processedContent;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when reading physician note file: {FilePath}", targetPath);
                throw new InvalidOperationException($"Cannot access physician note file: {targetPath}", ex);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error when reading physician note file: {FilePath}", targetPath);
                throw new InvalidOperationException($"Error reading physician note file: {targetPath}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error reading physician note. Using fallback content.");
                return FallbackNote;
            }
        }

        /// <summary>
        /// Processes note content to handle different formats (plain text, JSON-wrapped)
        /// </summary>
        private string ProcessNoteContent(string rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
                return rawContent;

            // Try to parse as JSON first (for JSON-wrapped notes)
            try
            {
                var jsonObject = JObject.Parse(rawContent);
                
                // Look for common JSON properties that might contain the note
                if (jsonObject["data"] != null)
                {
                    _logger.LogDebug("Found JSON-wrapped note with 'data' property");
                    return jsonObject["data"].ToString();
                }
                
                if (jsonObject["note"] != null)
                {
                    _logger.LogDebug("Found JSON-wrapped note with 'note' property");
                    return jsonObject["note"].ToString();
                }
                
                if (jsonObject["content"] != null)
                {
                    _logger.LogDebug("Found JSON-wrapped note with 'content' property");
                    return jsonObject["content"].ToString();
                }

                _logger.LogDebug("JSON detected but no recognized note property found. Using raw JSON.");
                return rawContent;
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Not JSON, treat as plain text
                _logger.LogDebug("Content is plain text format");
                return rawContent;
            }
        }
    }
}