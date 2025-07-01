namespace SignalBoosterRefactored.Models
{
    /// <summary>
    /// Configuration for a DME device type
    /// </summary>
    public class DmeDeviceConfiguration
    {
        /// <summary>
        /// The name of the device (e.g., "CPAP", "Oxygen Tank")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Keywords to search for in physician notes to identify this device
        /// </summary>
        public string[] Keywords { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Priority for device detection (lower number = higher priority)
        /// Used when multiple devices are mentioned in the same note
        /// </summary>
        public int Priority { get; set; } = 999;
    }

    /// <summary>
    /// Configuration section for all DME devices
    /// </summary>
    public class DmeConfiguration
    {
        public const string SectionName = "DmeDevices";
        
        public List<DmeDeviceConfiguration> Devices { get; set; } = new();
    }
}