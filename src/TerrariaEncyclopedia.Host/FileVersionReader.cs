using System.Diagnostics;

namespace TerrariaEncyclopedia.Host
{
    public interface IFileVersionReader
    {
        string GetFileVersion(string path);
    }

    public sealed class FileVersionReader : IFileVersionReader
    {
        public string GetFileVersion(string path)
        {
            return FileVersionInfo.GetVersionInfo(path).FileVersion;
        }
    }
}
