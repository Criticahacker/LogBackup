using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Morgan.LogBackup.Core.Models;

namespace Morgan.LogBackup.Core.Contracts
{
    public interface ILogSource
    {
        IEnumerable<LogFile> GetFiles();
        Stream OpenRead(LogFile file, long offset);
    }
}
