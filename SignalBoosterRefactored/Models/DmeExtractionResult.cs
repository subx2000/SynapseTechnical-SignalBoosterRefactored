namespace SignalBoosterRefactored.Models
{
    public class DmeExtractionResult
    {
        public string Device { get; set; } = string.Empty;
        public string? MaskType { get; set; }
        public string[]? AddOns { get; set; }
        public string? Qualifier { get; set; }
        public string OrderingProvider { get; set; } = string.Empty;
        public string? Liters { get; set; }
        public string? Usage { get; set; }
        public string? PatientName { get; set; }
        public string? DateOfBirth { get; set; }
        public string? Diagnosis { get; set; }
    }
}