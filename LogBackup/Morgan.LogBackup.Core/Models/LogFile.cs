using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morgan.LogBackup.Core.Models
{
    /// <summary>
    /// Represents a log file available for processing.
    /// </summary>
    /// <remarks>
    /// - Used by FileLogSource to describe discovered input files   
    /// </remarks>
    public class LogFile
    {
        /// <summary>
        /// File name of the log (without directory path).
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// Current size of the file in bytes.
        /// Used to determine whether new data exists and to detect truncation or rotation.
        /// </summary>
        public long Length { get; init; }
    }
}
