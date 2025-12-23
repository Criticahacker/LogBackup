using Microsoft.Extensions.Logging;
using Morgan.LogBackup.Core.Contracts;

namespace Morgan.LogBackup.Infrastructure.FileSystem
{
    /// <summary>
    /// Responsible for persisting processed log data to backup files
    /// in the configured output directory.
    /// </summary>
    /// <remarks>
    /// - Ensures backup directory exists before writing  
    /// - Appends content to target file rather than overwriting  
    /// </remarks>
    public class FileLogSink : ILogSink
    {
        private readonly string _path;
        private readonly ILogger<FileLogSink> _logger;


        public FileLogSink(string path, ILogger<FileLogSink> logger)
        {
            _path = path;
            Directory.CreateDirectory(_path);
            _logger = logger;
        }

        /// <summary>
        /// Appends processed log content to the specified backup file.
        /// </summary>
        /// <param name="fileName">Target backup file name.</param>
        /// <param name="content">Log content to append.</param>
        /// <remarks>
        /// - Writes are append-only to preserve existing backup data  
        /// - Any I/O failure is logged and rethrown to ensure visibility  
        /// </remarks>
        public void Write(string fileName, string content)
        {
            try
            {
                File.AppendAllText(Path.Combine(_path, fileName), content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed writing to backup file {BackupFile}", fileName);
                throw;
            }
        }
    }

}
