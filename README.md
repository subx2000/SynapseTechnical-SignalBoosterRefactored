Signal Booster - DME Data Extraction Tool
A refactored application for extracting Durable Medical Equipment (DME) information from physician notes and submitting structured data to external APIs.

Development Environment
IDE: Visual Studio Code with C# Dev Kit extension

AI Development Tools:

Claude 3.5 Sonnet - 
    * Used for initial project creation (I've never created one from scratch in VS Code, always VS.)  
    * Streamlined creating classes/interfaces instead of hand writing
    * Created the regex for text matching

Improvements
* Improved architecture, readability, maintainability, and scalability
* Handles text and JSON formats.  Easily expandable to additional formats like HL7
* Enhanced logging with no swallowed exceptions.
* Added Unit testing

Assumptions

* Physician notes follow a consistent format with recognizable keywords
* The API endpoint accepts JSON payloads in the specified format
* Input files are UTF-8 encoded text files

Known Limitations

* Test API Endpoint: The configured endpoint https://alert-api.com/DrExtract is not real and will result in DNS resolution failures (this is expected behavior for the assignment)
* Single File Processing: Currently processes one physician note at a time
* Regex-Based Extraction: Primary extraction relies on keyword matching and regex patterns

Future Improvements

* Enhanced LLM Integration: More sophisticated AI-powered text extraction - Started this but ran out of time.  This would be easy to add with fallback to the regex extractor.
* Batch Processing: Handle multiple files simultaneously
* Database: Store extraction results, have more flexibility on configurable devices


How to Run
1: Restore Dependencies and build
    * bash: dotnet restore
    * bash: dotnet build

2: Configure Input File
   * Place your physician note in the root directory
   * Update `PhysicianNoteFilePath` in appsettings.json if needed

3: Run Application
    * bash: dotnet run --project .\SignalBoosterRefactored\

4: Run Tests
    * bash: dotnet test (or use Test Explorer)