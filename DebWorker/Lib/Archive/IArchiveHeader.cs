using DebWorker.Lib.Models;

namespace DebWorker.Lib.Archive
{
    public interface IArchiveHeader
    {
        LinuxFileMode FileMode { get; }
        DateTimeOffset LastModified { get; }
        uint FileSize { get; }
    }
}
