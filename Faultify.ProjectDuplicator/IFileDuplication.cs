using System.IO;

namespace Faultify.ProjectDuplicator
{
    public interface IFileDuplication: IDisposable
    {
        string Name { get; set; }
        string Directory { get; set; }

        string FullFilePath();
        bool IsWriteModeEnabled();
        bool IsReadModeEnabled();
        Stream OpenReadWriteStream();
        Stream OpenReadStream();
        void EnableReadWriteOnly();
        void EnableReadOnly();
    }
}