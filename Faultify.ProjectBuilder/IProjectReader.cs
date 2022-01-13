using System;
using System.Threading.Tasks;

namespace Faultify.ProjectBuilder
{
    /// <summary>
    ///     Reading the project in an Asyncronous manner
    /// </summary>
    public interface IProjectReader
    {
        Task<IProjectInfo> ReadProjectAsync(string path, IProgress<string> progress);
    }
}
