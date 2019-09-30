using System.IO;
using System;

namespace Tests.Utils
{
    sealed class TemporaryPath: IDisposable
    {
        public TemporaryPath() {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                System.IO.Path.GetRandomFileName());
        }

        public string Path { get; }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}