using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SignalBoosterRefactored.Interfaces;

namespace SignalBoosterRefactored.Services
{
    /// <summary>
    /// Main orchestrator that coordinates the entire DME data processing workflow
    /// </summary>
    public class DmeDataProcessor : IDmeDataProcessor
    {
        private readonly ILogger<DmeDataProcessor> _logger;
        private readonly IPhysicianNoteReader _noteReader;
        private readonly IDmeDataExtractor _dataExtractor;
        private readonly IApiClient _apiClient;
        private readonly IConfiguration _configuration;

        public DmeDataProcessor(
            ILogger<DmeDataProcessor> logger,
            IPhysicianNoteReader noteReader,
            IDmeDataExtractor dataExtractor,
            IApiClient apiClient,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _noteReader = noteReader ?? throw new ArgumentNullException(nameof(noteReader));
            _dataExtractor = dataExtractor ?? throw new ArgumentNullException(nameof(dataExtractor));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task ProcessPhysicianNoteAsync()
        {
            try
            {
                _logger.LogInformation("Beginning physician note processing");

                // Step 1: Read the physician note
                var defaultFilePath = _configuration["PhysicianNoteFilePath"] ?? "physician_note.txt";
                var physicianNote = await _noteReader.ReadPhysicianNoteAsync(defaultFilePath);
                _logger.LogInformation("Successfully read physician note (length: {Length} characters)", physicianNote.Length);

                // Step 2: Extract structured DME data
                var extractedData = _dataExtractor.ExtractDmeData(physicianNote);
                _logger.LogInformation("Extracted DME data for device: {Device}", extractedData.Device);

                // Step 3: Submit to external API
                var submissionSuccess = await _apiClient.SubmitDmeDataAsync(extractedData);
                
                if (submissionSuccess)
                {
                    _logger.LogInformation("Successfully submitted DME data to external API");
                }
                else
                {
                    _logger.LogWarning("Failed to submit DME data to external API");
                    throw new InvalidOperationException("API submission failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during physician note processing");
                throw;
            }
        }
    }
}