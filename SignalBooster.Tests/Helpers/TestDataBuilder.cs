using SignalBoosterRefactored.Models;

namespace SignalBooster.Tests.Helpers
{
    public static class TestDataBuilder
    {
        public static class PhysicianNotes
        {
            public static string CreateCpapNote() => 
                "Patient Name: John Doe\nCPAP therapy with full face mask and humidifier needed.\nAHI > 20 documented.\nOrdering Physician: Dr. House";

            public static string CreateOxygenNote() => 
                "Patient Name: Harold Finch\nDOB: 04/12/1952\nDiagnosis: COPD\nRequires portable oxygen tank delivering 2 L per minute.\nUsage: During sleep and exertion.\nOrdering Physician: Dr. Cuddy";
        }

        public static class DmeResults
        {
            public static DmeExtractionResult CreateCpapResult() => new()
            {
                Device = "CPAP",
                MaskType = "full face",
                AddOns = new[] { "humidifier" },
                Qualifier = "AHI > 20",
                OrderingProvider = "Dr. House",
                PatientName = "John Doe"
            };

            public static DmeExtractionResult CreateOxygenResult() => new()
            {
                Device = "Oxygen Tank",
                Liters = "2 L",
                Usage = "sleep and exertion",
                OrderingProvider = "Dr. Cuddy",
                PatientName = "Harold Finch"
            };
        }
    }
}