using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morgan.LogBackup.Infrastructure.Processing
{
    /// <summary>
    /// Represents a masking rule used for partially masking sensitive field values.
    /// Determines how many characters remain visible at the start and end of a value,
    /// with all characters in between replaced by asterisks.
    /// </summary>
    /// <remarks>
    /// Example:
    /// If value = "1234567890", VisibleStart = 2, VisibleEnd = 2  
    /// Result => "12******90"
    /// - Keeps data partially readable for diagnostics  
    /// - Protects sensitive information from exposure  
    /// - Used by JsonLogProcessor during log sanitization  
    /// </remarks>
    public class MaskRule
    {
        /// <summary>
        /// Number of characters to remain visible from the beginning of the value.
        /// </summary>
        public int VisibleStart { get; set; }

        /// <summary>
        /// Number of characters to remain visible from the end of the value.
        /// </summary>
        public int VisibleEnd { get; set; }
    }

}
