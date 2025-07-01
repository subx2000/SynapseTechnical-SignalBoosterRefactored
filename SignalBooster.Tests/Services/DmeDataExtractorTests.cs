using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SignalBoosterRefactored.Services;
using SignalBooster.Tests.Helpers;

namespace SignalBooster.Tests.Services
{
    [TestClass]
    public class DmeDataExtractorTests
    {
        private Mock<ILogger<DmeDataExtractor>>? _mockLogger;
        private IConfiguration? _configuration;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<DmeDataExtractor>>();
            
            // Create a basic configuration for tests
            var configData = new Dictionary<string, string>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
        }

        [TestMethod]
        public void ExtractDmeData_WithCpapNote_ExtractsCorrectData()
        {
            // Arrange
            var extractor = new DmeDataExtractor(_mockLogger!.Object, _configuration!);
            var cpapNote = TestDataBuilder.PhysicianNotes.CreateCpapNote();

            // Act
            var result = extractor.ExtractDmeData(cpapNote);

            // Assert
            result.Device.Should().Be("CPAP");
            result.MaskType.Should().Be("full face");
            result.AddOns.Should().ContainSingle().Which.Should().Be("humidifier");
            result.Qualifier.Should().Be("AHI > 20");
            result.OrderingProvider.Should().Be("Dr. House");
            result.PatientName.Should().Be("John Doe");
        }

        [TestMethod]
        public void ExtractDmeData_WithOxygenNote_ExtractsCorrectData()
        {
            // Arrange
            var extractor = new DmeDataExtractor(_mockLogger!.Object, _configuration!);
            var oxygenNote = TestDataBuilder.PhysicianNotes.CreateOxygenNote();

            // Act
            var result = extractor.ExtractDmeData(oxygenNote);

            // Assert
            result.Device.Should().Be("Oxygen Tank");
            result.Liters.Should().Be("2 L");
            result.Usage.Should().Be("sleep and exertion");
            result.OrderingProvider.Should().Be("Dr. Cuddy");
            result.PatientName.Should().Be("Harold Finch");
        }

        [TestMethod]
        public void ExtractDmeData_WithEmptyNote_ThrowsArgumentException()
        {
            // Arrange
            var extractor = new DmeDataExtractor(_mockLogger!.Object, _configuration!);

            // Act & Assert
            extractor.Invoking(x => x.ExtractDmeData(""))
                .Should().Throw<ArgumentException>()
                .WithParameterName("physicianNote");
        }

        [TestMethod]
        public void ExtractDmeData_WithConfigurableDevice_DetectsNewDevice()
        {
            // Arrange - Create configuration with custom Hospital Bed device
            var configData = new Dictionary<string, string>
            {
                ["DmeDevices:0:Name"] = "Hospital Bed",
                ["DmeDevices:0:Keywords:0"] = "hospital bed",
                ["DmeDevices:0:Keywords:1"] = "adjustable bed",
                ["DmeDevices:0:Priority"] = "1"
            };

            var customConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            var extractor = new DmeDataExtractor(_mockLogger!.Object, customConfig);

            var note = "Patient Name: Test Patient\nHospital bed needed for recovery.\nOrdering Physician: Dr. Test";

            // Act
            var result = extractor.ExtractDmeData(note);

            // Assert
            result.Device.Should().Be("Hospital Bed");
            result.PatientName.Should().Be("Test Patient");
            result.OrderingProvider.Should().Be("Dr. Test");
        }
    }
}