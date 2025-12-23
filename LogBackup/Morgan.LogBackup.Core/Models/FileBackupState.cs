using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morgan.LogBackup.Core.Models
{
    /// <summary>
    /// Represents the persisted processing state for a log file,
    /// including the last processed byte offset and the associated backup file name.
    /// </summary>
    public class FileBackupState
    {
        /// <summary>
        /// The last processed byte offset within the source log file.
        /// Used to continue processing incrementally.
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// The backup file name associated with this source file.
        /// All processed entries from the log are written to this file.
        /// </summary>
        public string BackupFile { get; set; } = "";
    }
}
