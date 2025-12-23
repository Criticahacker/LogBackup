using Microsoft.Extensions.Logging;
using Morgan.LogBackup.Core.Contracts;
using Morgan.LogBackup.Core.Models;

namespace Morgan.LogBackup.Infrastructure.FileSystem
{
    /// <summary>
    /// Provides access to log files stored in the configured input directory.
    /// Responsible for discovering available files and opening them for reading
    /// from a specified offset.
    /// </summary>
    public class FileLogSource : ILogSource
    {
        private readonly string _path;
        private readonly ILogger<FileLogSource> _logger;

        public FileLogSource(string path, ILogger<FileLogSource> logger)
        {
            _path = path;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all log files from the configured input directory.
        /// </summary>
        /// <returns>
        /// A collection of LogFile objects containing file name and size information.
        /// </returns>
        /// <remarks>
        /// - Returns an empty collection if the directory does not exist  
        /// - Logs warnings if directory is missing  
        /// </remarks>
        public IEnumerable<LogFile> GetFiles()
        {
            try
            {
                if (!Directory.Exists(_path))
                {
                    _logger.LogWarning("Input directory {Path} does not exist", _path);
                    return Enumerable.Empty<LogFile>();
                }

                return Directory.GetFiles(_path)
                    .Select(f => new FileInfo(f))
                    .Select(f => new LogFile { Name = f.Name, Length = f.Length });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files from {Path}", _path);
                return Enumerable.Empty<LogFile>();
            }
        }

        /// <summary>
        /// Opens a log file for reading starting at the specified byte offset.
        /// </summary>
        /// <param name="file">The log file to open.</param>
        /// <param name="offset">The byte offset from which reading should begin.</param>
        /// <returns>
        /// Stream positioned at the requested offset.
        /// </returns>
        /// <remarks>
        /// - Uses shared read access so files can be read while still being written  
        /// - Seeks to the provided offset to support incremental processing
        public Stream OpenRead(LogFile file, long offset)
        {
            try
            {
                var fs = new FileStream(
                Path.Combine(_path, file.Name),
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

                fs.Seek(offset, SeekOrigin.Begin);
                return fs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to open file {File} at offset {Offset}",
                    file.Name, offset);

                throw;
            }
        }
    }

}
