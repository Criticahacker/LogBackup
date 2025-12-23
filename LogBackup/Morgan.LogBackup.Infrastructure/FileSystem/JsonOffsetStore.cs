using System.Text.Json;
using Microsoft.Extensions.Logging;
using Morgan.LogBackup.Core.Contracts;
using Morgan.LogBackup.Core.Models;

namespace Morgan.LogBackup.Infrastructure.FileSystem
{
    /// <summary>
    /// Provides persistent storage for backup state information,
    /// including last processed file offset and associated backup file name.
    /// Stored state allows log processing to resume safely after restarts.
    /// </summary>
    /// <remarks>
    /// - Persists state in a JSON file  
    /// - Ensures idempotent, incremental processing across executions  
    /// - Automatically initializes new state entries when files are seen for the first time    
    /// </remarks>
    public class JsonOffsetStore : IOffsetStore
    {
        private readonly string _file;
        private Dictionary<string, FileBackupState> _state; 
        private readonly ILogger<JsonOffsetStore> _logger;

        /// <summary>
        /// Attempts to load previously persisted state from disk.
        /// </summary>
        /// <param name="file">Path to the JSON state file.</param>
        /// <param name="logger">Logger used for logging.</param>
        /// <remarks>
        /// - If the state file does not exist, a new empty state is created  
        /// - If the file exists but cannot be deserialized, state is reset and error logged  
        /// </remarks>
        public JsonOffsetStore(string file, ILogger<JsonOffsetStore> logger)
        {
            _file = file;
            _logger = logger;
            try
            {
                _state = File.Exists(file)
                ? JsonSerializer.Deserialize<Dictionary<string, FileBackupState>>(File.ReadAllText(file))!
                : new(); //{}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading state file {File}", file);
                _state = new();
            }
        }

        /// <summary>
        /// Retrieves persisted offset and backup file information for the specified log file.
        /// </summary>
        /// <param name="fileName">The input log file name.</param>
        /// <returns>
        /// A tuple containing the last processed byte offset and the associated backup file name.
        /// </returns>
        /// <remarks>
        /// - If the file has never been processed before, a new entry is created  
        /// - A timestamp-based backup file name is generated for new entries  
        /// - Persists newly created state immediately  
        /// </remarks>
        public (long Offset, string BackupFile) GetState(string fileName)
        {
            if (_state.TryGetValue(fileName, out var entry))
                return (entry.Offset, entry.BackupFile);

            // first time → read file -> set offset to 0 and generate new file
            var newEntry = new FileBackupState
            {
                Offset = 0,
                BackupFile = $"{DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss-fff")}-{fileName}"
            };

            _state[fileName] = newEntry;
            Persist();

            return (0, newEntry.BackupFile);
        }

        /// <summary>
        /// Updates the stored offset for the specified log file  and persists the updated state to disk.
        /// </summary>
        /// <param name="fileName">The input log file name.</param>
        /// <param name="offset">The latest processed byte offset.</param>
        /// <remarks>
        /// - Ensures next execution resumes from the correct byte position  
        /// </remarks>
        public void SaveState(string fileName, long offset)
        {
            _state[fileName].Offset = offset;
            Persist();
        }

        /// <summary>
        /// Writes the current state dictionary to the JSON state file.
        /// </summary>
        private void Persist()
        {
            try
            {
                File.WriteAllText(
                    _file,
                    JsonSerializer.Serialize(_state, new JsonSerializerOptions { WriteIndented = true })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed writing state file {File}", _file);
            }
        }
    }

}
