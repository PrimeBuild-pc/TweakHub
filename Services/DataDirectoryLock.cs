using System.Diagnostics;
using System.IO;

namespace TweakHub.Services;

internal static class DataDirectoryLock
{
    internal static async Task<FileStream?> AcquireAsync(string directory, TimeSpan wait)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, ".tweakhub.lock");
        var elapsed = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex) when ((ex.HResult & 0xffff) is 32 or 33)
            {
                if (elapsed.Elapsed >= wait) return null;
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}
