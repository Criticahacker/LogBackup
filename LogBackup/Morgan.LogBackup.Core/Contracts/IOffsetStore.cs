using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Morgan.LogBackup.Core.Contracts
{
    public interface IOffsetStore
    {
        (long Offset, string BackupFile) GetState(string fileName);
        void SaveState(string fileName, long offset);
    }
}
