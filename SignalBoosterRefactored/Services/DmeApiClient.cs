using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalBoosterRefactored.Interfaces;
using SignalBoosterRefactored.Models;

namespace SignalBoosterRefactored.Services
{
    /// <summary>
    /// Handles API communication for submitting DME data to external services
    /// </summary>
    public class DmeApiClient : SignalBoosterRefactored.Interfaces.IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DmeApiClient> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiEndpoint;

        public DmeApiClient(HttpClient httpClient, ILogger<DmeApiClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Get API endpoint from configuration, with fallback to original URL
            _apiEndpoint = _configuration["DmeApiEndpoint"] ?? "https://alert-api.com/DrExtract";
            
            // Configure HTTP client timeout
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> SubmitDmeDataAsync(DmeExtractionResult dmeData)
        {
            if (dmeData == null)
            {
                _logger?.LogError("DME data is null, cannot submit to API");
                throw new ArgumentNullException(nameof(dmeData));
            }

            try
            {
                _logger?.LogInformation("Preparing to submit DME data to API endpoint: {Endpoint}", _apiEndpoint);

                // Convert DME data to JSON payload
                var jsonPayload = SerializeDmeData(dmeData);
                _logger?.LogInformation("Generated JSON payload for submission: {Payload}", jsonPayload);

                // Check if this is a test/fake endpoint
                var isTestEndpoint = _apiEndpoint.Contains("alert-api.com") || _apiEndpoint.Contains("test") || _apiEndpoint.Contains("localhost");
                if (isTestEndpoint)
                {
                    _logger?.LogWarning("⚠️  Attempting to submit to test/fake API endpoint: {Endpoint}", _apiEndpoint);
                    _logger?.LogInformation("💡 In a real environment, configure a valid API endpoint using the 'DmeApiEndpoint' configuration setting");
                }

                // Create HTTP content
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Submit to API
                _logger?.LogInformation("🚀 Submitting DME data to external API...");
                var response = await _httpClient.PostAsync(_apiEndpoint, content);

                // Log response details
                _logger?.LogInformation("✅ API response received. Status: {StatusCode} ({StatusCodeNumber}), Reason: {ReasonPhrase}", 
                    response.StatusCode, (int)response.StatusCode, response.ReasonPhrase);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogInformation("🎉 API submission successful! Response: {Content}", responseContent);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger?.LogError("❌ API submission failed. Status: {StatusCode} ({StatusCodeNumber}), Error: {Error}", 
                        response.StatusCode, (int)response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("No such host is known") || ex.Message.Contains("Name or service not known"))
            {
                _logger?.LogError("🌐 DNS/Network Error: Unable to resolve API endpoint '{Endpoint}'. This is expected for test/fake URLs.", _apiEndpoint);
                _logger?.LogInformation("💡 To test with a real API:");
                _logger?.LogInformation("   1. Set 'DmeApiEndpoint' in appsettings.json to a valid URL");
                _logger?.LogInformation("   2. Or set environment variable 'DmeApiEndpoint'");
                _logger?.LogInformation("   3. Or use a tool like ngrok to create a test endpoint");
                _logger?.LogDebug("Full network error details: {Exception}", ex);
                return false;
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogError(ex, "🌐 HTTP request failed when submitting DME data to API: {Endpoint}", _apiEndpoint);
                _logger?.LogInformation("💡 Check network connectivity and API endpoint configuration");
                return false;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.Message.Contains("timeout"))
            {
                _logger?.LogError("⏰ API request timed out after {Timeout} seconds when submitting to: {Endpoint}", 
                    _httpClient.Timeout.TotalSeconds, _apiEndpoint);
                _logger?.LogInformation("💡 Consider increasing timeout or checking API response time");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger?.LogError(ex, "🚫 API request was cancelled when submitting DME data to: {Endpoint}", _apiEndpoint);
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "💥 Unexpected error occurred when submitting DME data to API: {Endpoint}", _apiEndpoint);
                _logger?.LogInformation("💡 This might indicate a configuration or code issue");
                return false;
            }
        }

        /// <summary>
        /// Serializes DME data to JSON format compatible with the external API
        /// </summary>
        private string SerializeDmeData(DmeExtractionResult dmeData)
        {
            var jsonObject = new JObject
            {
                ["device"] = dmeData.Device
            };

            // Add optional fields only if they have values
            if (!string.IsNullOrEmpty(dmeData.MaskType))
                jsonObject["mask_type"] = dmeData.MaskType;

            if (dmeData.AddOns != null && dmeData.AddOns.Length > 0)
                jsonObject["add_ons"] = new JArray(dmeData.AddOns);

            if (!string.IsNullOrEmpty(dmeData.Qualifier))
                jsonObject["qualifier"] = dmeData.Qualifier;

            if (!string.IsNullOrEmpty(dmeData.OrderingProvider))
                jsonObject["ordering_provider"] = dmeData.OrderingProvider;

            // Oxygen tank specific fields
            if (dmeData.Device == "Oxygen Tank")
            {
                if (!string.IsNullOrEmpty(dmeData.Liters))
                    jsonObject["liters"] = dmeData.Liters;

                if (!string.IsNullOrEmpty(dmeData.Usage))
                    jsonObject["usage"] = dmeData.Usage;
            }

            // Patient information (if available)
            if (!string.IsNullOrEmpty(dmeData.PatientName))
                jsonObject["patient_name"] = dmeData.PatientName;

            if (!string.IsNullOrEmpty(dmeData.DateOfBirth))
                jsonObject["dob"] = dmeData.DateOfBirth;

            if (!string.IsNullOrEmpty(dmeData.Diagnosis))
                jsonObject["diagnosis"] = dmeData.Diagnosis;

            return jsonObject.ToString(Formatting.None);
        }
    }
}