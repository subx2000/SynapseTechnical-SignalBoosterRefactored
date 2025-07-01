using System.Threading.Tasks;
using SignalBoosterRefactored.Models;

namespace SignalBoosterRefactored.Interfaces

{
    /// <summary>
    /// Main orchestrator for the DME data processing workflow
    /// </summary>
    public interface IDmeDataProcessor
    {
        Task ProcessPhysicianNoteAsync();
    }

    /// <summary>
    /// Handles reading physician notes from various sources
    /// </summary>
    public interface IPhysicianNoteReader
    {
        Task<string> ReadPhysicianNoteAsync(string filePath = null);
    }

    /// <summary>
    /// Extracts structured DME data from physician notes
    /// </summary>
    public interface IDmeDataExtractor
    {
        DmeExtractionResult ExtractDmeData(string physicianNote);
    }

    /// <summary>
    /// Handles API communication for submitting DME data
    /// </summary>
    public interface IApiClient
    {
        Task<bool> SubmitDmeDataAsync(DmeExtractionResult dmeData);
    }
}