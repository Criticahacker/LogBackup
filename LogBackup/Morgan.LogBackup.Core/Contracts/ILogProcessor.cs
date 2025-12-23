using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morgan.LogBackup.Core.Contracts
{
    public interface ILogProcessor
    {
        string? Process(string rawLine);
    }
}
