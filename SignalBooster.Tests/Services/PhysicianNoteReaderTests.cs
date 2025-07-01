using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SignalBoosterRefactored.Services;

namespace SignalBooster.Tests.Services
{
    [TestClass]
    public class PhysicianNoteReaderTests
    {
        private PhysicianNoteReader _reader;
        private Mock<ILogger<PhysicianNoteReader>> _mockLogger;
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<PhysicianNoteReader>>();
            _reader = new PhysicianNoteReader(_mockLogger.Object);
            _testDirectory = Path.Combine(Path.GetTempPath(), "PhysicianNoteReaderTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public async Task ReadPhysicianNoteAsync_WithValidFile_ReturnsFileContent()
        {
            // Arrange
            var testContent = "Test physician note content";
            var testFile = Path.Combine(_testDirectory, "test_note.txt");
            await File.WriteAllTextAsync(testFile, testContent);

            // Act
            var result = await _reader.ReadPhysicianNoteAsync(testFile);

            // Assert
            result.Should().Be(testContent);
        }

        [TestMethod]
        public async Task ReadPhysicianNoteAsync_WithNonExistentFile_ReturnsFallbackContent()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "does_not_exist.txt");

            // Act
            var result = await _reader.ReadPhysicianNoteAsync(nonExistentFile);

            // Assert
            result.Should().Contain("CPAP with full face mask");
            result.Should().Contain("Dr. Cameron");
        }
    }
}