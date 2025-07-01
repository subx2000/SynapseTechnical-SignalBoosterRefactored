using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SignalBoosterRefactored.Interfaces;
using SignalBoosterRefactored.Models;

namespace SignalBoosterRefactored.Services
{
    public partial class DmeDataExtractor : IDmeDataExtractor
    {
        private readonly ILogger<DmeDataExtractor> _logger;
        private readonly IConfiguration _configuration;
        private readonly List<DmeDeviceConfiguration> _deviceConfigurations;

        [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*L(?:iters?)?", RegexOptions.IgnoreCase)]
        private static partial Regex LitersPattern();
        
        [GeneratedRegex(@"Dr\.\s*([A-Za-z\s]+)", RegexOptions.IgnoreCase)]
        private static partial Regex ProviderPattern();
        
        [GeneratedRegex(@"Patient\s+Name:\s*([^\n\r]+)", RegexOptions.IgnoreCase)]
        private static partial Regex PatientNamePattern();
        
        [GeneratedRegex(@"DOB:\s*([^\n\r]+)", RegexOptions.IgnoreCase)]
        private static partial Regex DateOfBirthPattern();
        
        [GeneratedRegex(@"Diagnosis:\s*([^\n\r]+)", RegexOptions.IgnoreCase)]
        private static partial Regex DiagnosisPattern();

        public DmeDataExtractor(ILogger<DmeDataExtractor> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            // Load device configurations from appsettings
            _deviceConfigurations = LoadDeviceConfigurations();
            
            _logger.LogInformation("Loaded {Count} DME device configurations", _deviceConfigurations.Count);
            foreach (var device in _deviceConfigurations.OrderBy(d => d.Priority))
            {
                _logger.LogDebug("Device: {Name}, Keywords: [{Keywords}], Priority: {Priority}", 
                    device.Name, 
                    string.Join(", ", device.Keywords), 
                    device.Priority);
            }
        }

        private List<DmeDeviceConfiguration> LoadDeviceConfigurations()
        {
            var devices = new List<DmeDeviceConfiguration>();
            
            try
            {
                var dmeSection = _configuration.GetSection("DmeDevices");
                if (dmeSection.Exists())
                {
                    devices = dmeSection.Get<List<DmeDeviceConfiguration>>() ?? new List<DmeDeviceConfiguration>();
                }
                
                // If no configuration found, use default fallback
                if (devices.Count == 0)
                {
                    _logger.LogWarning("No DME device configurations found in appsettings. Using default configurations.");
                    devices = GetDefaultDeviceConfigurations();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading DME device configurations. Using defaults.");
                devices = GetDefaultDeviceConfigurations();
            }
            
            return devices.OrderBy(d => d.Priority).ToList();
        }

        private static List<DmeDeviceConfiguration> GetDefaultDeviceConfigurations()
        {
            return new List<DmeDeviceConfiguration>
            {
                new() { Name = "CPAP", Keywords = new[] { "CPAP", "continuous positive airway pressure" }, Priority = 1 },
                new() { Name = "Oxygen Tank", Keywords = new[] { "oxygen", "O2", "oxygen tank" }, Priority = 2 },
                new() { Name = "Wheelchair", Keywords = new[] { "wheelchair", "mobility chair" }, Priority = 3 }
            };
        }

        public DmeExtractionResult ExtractDmeData(string physicianNote)
        {
            if (string.IsNullOrWhiteSpace(physicianNote))
            {
                _logger.LogWarning("Physician note is empty or null");
                throw new ArgumentException("Physician note cannot be empty or null", nameof(physicianNote));
            }

            _logger.LogDebug("Starting DME data extraction from physician note");

            var result = new DmeExtractionResult
            {
                Device = ExtractDeviceType(physicianNote),
                OrderingProvider = ExtractOrderingProvider(physicianNote),
                PatientName = ExtractPatientName(physicianNote),
                DateOfBirth = ExtractDateOfBirth(physicianNote),
                Diagnosis = ExtractDiagnosis(physicianNote)
            };

            ExtractDeviceSpecificData(physicianNote, result);

            _logger.LogInformation("DME data extraction completed. Device: {Device}", result.Device);
            return result;
        }

        private string ExtractDeviceType(string note)
        {
            _logger.LogDebug("Starting device type detection for note with {Length} characters", note.Length);
            
            // Check each configured device in priority order
            foreach (var deviceConfig in _deviceConfigurations)
            {
                _logger.LogDebug("Checking device: {DeviceName} with priority {Priority}", 
                    deviceConfig.Name, deviceConfig.Priority);
                
                foreach (var keyword in deviceConfig.Keywords)
                {
                    if (ContainsIgnoreCase(note, keyword))
                    {
                        _logger.LogInformation("Identified device type: {DeviceType} (matched keyword: '{Keyword}')", 
                            deviceConfig.Name, keyword);
                        return deviceConfig.Name;
                    }
                    
                    _logger.LogDebug("Keyword '{Keyword}' not found for device {DeviceName}", 
                        keyword, deviceConfig.Name);
                }
            }

            _logger.LogWarning("Could not identify device type from note. No configured keywords matched.");
            return "Unknown";
        }

        private void ExtractDeviceSpecificData(string note, DmeExtractionResult result)
        {
            switch (result.Device)
            {
                case "CPAP":
                    ExtractCpapData(note, result);
                    break;
                case "Oxygen Tank":
                    ExtractOxygenTankData(note, result);
                    break;
                default:
                    _logger.LogDebug("No specific extraction logic for device type: {Device}", result.Device);
                    break;
            }
        }

        private void ExtractCpapData(string note, DmeExtractionResult result)
        {
            _logger.LogDebug("Extracting CPAP-specific data");
            
            if (ContainsIgnoreCase(note, "full face"))
            {
                result.MaskType = "full face";
                _logger.LogDebug("Identified CPAP mask type: full face");
            }
            else if (ContainsIgnoreCase(note, "nasal pillow"))
            {
                result.MaskType = "nasal pillow";
                _logger.LogDebug("Identified CPAP mask type: nasal pillow");
            }
            else if (ContainsIgnoreCase(note, "nasal"))
            {
                result.MaskType = "nasal";
                _logger.LogDebug("Identified CPAP mask type: nasal");
            }

            var addOns = new List<string>();
            if (ContainsIgnoreCase(note, "heated humidifier"))
            {
                addOns.Add("heated humidifier");
                _logger.LogDebug("Identified CPAP add-on: heated humidifier");
            }
            else if (ContainsIgnoreCase(note, "humidifier"))
            {
                addOns.Add("humidifier");
                _logger.LogDebug("Identified CPAP add-on: humidifier");
            }
            
            result.AddOns = addOns.ToArray();

            // Look for AHI values
            if (ContainsIgnoreCase(note, "AHI > 20"))
            {
                result.Qualifier = "AHI > 20";
                _logger.LogDebug("Identified CPAP qualifier: AHI > 20");
            }
            else if (ContainsIgnoreCase(note, "AHI:"))
            {
                // Try to extract specific AHI value
                var ahiMatch = Regex.Match(note, @"AHI:\s*(\d+)", RegexOptions.IgnoreCase);
                if (ahiMatch.Success)
                {
                    result.Qualifier = $"AHI: {ahiMatch.Groups[1].Value}";
                    _logger.LogDebug("Identified CPAP qualifier: {Qualifier}", result.Qualifier);
                }
            }
        }

        private void ExtractOxygenTankData(string note, DmeExtractionResult result)
        {
            _logger.LogDebug("Extracting Oxygen Tank-specific data");
            
            var litersMatch = LitersPattern().Match(note);
            if (litersMatch.Success)
            {
                result.Liters = litersMatch.Groups[1].Value + " L";
                _logger.LogDebug("Identified oxygen flow rate: {Liters}", result.Liters);
            }

            var usageScenarios = new List<string>();
            if (ContainsIgnoreCase(note, "sleep")) usageScenarios.Add("sleep");
            if (ContainsIgnoreCase(note, "exertion")) usageScenarios.Add("exertion");
            if (ContainsIgnoreCase(note, "continuous")) usageScenarios.Add("continuous");
            if (ContainsIgnoreCase(note, "as needed")) usageScenarios.Add("as needed");

            if (usageScenarios.Count > 0)
            {
                result.Usage = string.Join(" and ", usageScenarios);
                _logger.LogDebug("Identified oxygen usage: {Usage}", result.Usage);
            }
        }

        private string ExtractOrderingProvider(string note)
        {
            var match = ProviderPattern().Match(note);
            if (match.Success)
            {
                var provider = match.Groups[1].Value.Trim();
                _logger.LogDebug("Identified ordering provider: {Provider}", provider);
                return "Dr. " + provider;
            }

            _logger.LogWarning("Could not identify ordering provider from note");
            return "Unknown";
        }

        private string? ExtractPatientName(string note)
        {
            var match = PatientNamePattern().Match(note);
            if (match.Success)
            {
                var name = match.Groups[1].Value.Trim();
                _logger.LogDebug("Identified patient name: {Name}", name);
                return name;
            }

            return null;
        }

        private string? ExtractDateOfBirth(string note)
        {
            var match = DateOfBirthPattern().Match(note);
            if (match.Success)
            {
                var dob = match.Groups[1].Value.Trim();
                _logger.LogDebug("Identified date of birth: {DOB}", dob);
                return dob;
            }

            return null;
        }

        private string? ExtractDiagnosis(string note)
        {
            var match = DiagnosisPattern().Match(note);
            if (match.Success)
            {
                var diagnosis = match.Groups[1].Value.Trim();
                _logger.LogDebug("Identified diagnosis: {Diagnosis}", diagnosis);
                return diagnosis;
            }

            return null;
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            return source.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }
}