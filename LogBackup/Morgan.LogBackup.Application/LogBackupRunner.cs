using Morgan.LogBackup.Core.Contracts;
using Microsoft.Extensions.Logging;
using Morgan.LogBackup.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace Morgan.LogBackup.Application
{
    /// <summary>
    /// Reads log files from the configured source, resumes processing
    /// using the stored offset, applies masking / transformation, and
    /// writes processed logs to the configured backup sink.
    /// </summary>
    /// <remarks>
    /// Offset state is persisted to ensure idempotent incremental processing
    /// Does not open a file if there is no new data to process
    /// </remarks>
    public class LogBackupRunner
    {
        private readonly ILogSource _source;
        private readonly ILogSink _sink;
        private readonly IOffsetStore _offsetStore;
        private readonly ILogProcessor _processor;
        private readonly ILogger<LogBackupRunner> _logger;
        private readonly int _maxParallelFiles;

        public LogBackupRunner(
            ILogSource source,
            ILogSink sink,
            IOffsetStore offsetStore,
            ILogProcessor processor,
            ILogger<LogBackupRunner> logger,
            IConfiguration config)
        {
            _source = source;
            _sink = sink;
            _offsetStore = offsetStore;
            _processor = processor;
            _logger = logger;
            _maxParallelFiles = config.GetValue<int>("Worker:MaxParallelFiles", 4);
        }

        /// <summary>
        /// Process multiple files in parallel to improve performance
        /// try / catch → ensures failures in one file do not stop others; errors are logged and processing continues
        /// </summary>
        public async Task RunAsync(CancellationToken token)
        {
            //Reads all files from the input folder(gets FileName & Length of all files).
            var files = _source.GetFiles();

            //Configures how parallel processing should behave
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxParallelFiles,    //How many files are processed in parallel at the same time.         
                CancellationToken = token   //allows the parallel loop to stop gracefully if the application shuts down
            };

            //processes multiple files concurrently, not one - by - one.
            await Parallel.ForEachAsync(files, options, async (file, _) =>
            {
                try
                {
                    await ProcessFileAsync(file, token);        //asynchronously performs the complete backup logic for each file
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {File}. Skipping file.", file.Name);
                }
            });
        }

        /// <summary>
        /// Processes a single log file incrementally by reading only new content,
        /// applying processing rules, writing masked output to the backup file,
        /// and updating the persisted offset state.
        /// </summary>
        /// <remarks>
        /// • Retrieves previously saved state (offset + backup file name).
        /// • Skips processing if the file has no new data.
        /// • Resets offset if file is truncated or rotated.
        /// • Opens the file in shared-read mode to allow reading while it is being written.
        /// • Reads the file line-by-line and applies transformation / masking rules.
        /// • Appends processed output to the mapped backup file.
        /// • Saves the new offset only after successful completion.
        /// • Uses try/catch to avoid stopping processing due to bad lines or errors.
        /// </remarks>
        /// <param name="file">The log file metadata including name and length.</param>
        /// <param name="token">Cancellation token to support graceful interruption.</param>
        private async Task ProcessFileAsync(LogFile file, CancellationToken token)
        {
            var (offset, backupFile) = _offsetStore.GetState(file.Name);

            // if log file is empty, write empty backup and offset = 0
            if (file.Length == 0)
            {
                _sink.Write(backupFile, string.Empty);
                _offsetStore.SaveState(file.Name, 0);
                return;
            }

            // rotation / truncation
            if (file.Length < offset)
            {
                _logger.LogWarning("File {File} truncated. Resetting offset.", file.Name);
                offset = 0;
            }                    

            // no new data → do not open file
            if (file.Length == offset)
                return;

            using var stream = _source.OpenRead(file, offset);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var processed = _processor.Process(line);
                    if (processed != null)
                        _sink.Write(backupFile, processed + Environment.NewLine);
                }
                catch
                {
                    // Skip bad line quietly
                }
            }

            _offsetStore.SaveState(file.Name, stream.Position);
        }

    }

}
