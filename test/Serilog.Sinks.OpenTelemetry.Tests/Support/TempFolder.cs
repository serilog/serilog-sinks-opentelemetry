namespace Serilog.Sinks.OpenTelemetry.Tests.Support
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    class TempFolder : IDisposable
    {
        static readonly Guid Session = Guid.NewGuid();

        readonly string _tempFolder;

        public TempFolder(string? name = null)
        {
            _tempFolder = System.IO.Path.Combine(
                Environment.GetEnvironmentVariable("TMP") ?? Environment.GetEnvironmentVariable("TMPDIR") ?? "/tmp",
                "Serilog.Sinks.OpenTelemetry.Tests",
                Session.ToString("n"),
                name ?? Guid.NewGuid().ToString("n"));

            Directory.CreateDirectory(_tempFolder);
        }

        public string Path => _tempFolder;

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempFolder))
                    Directory.Delete(_tempFolder, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public static TempFolder ForCaller([CallerMemberName] string? caller = null, [CallerFilePath] string sourceFileName = "")
        {
            if (caller == null) throw new ArgumentNullException(nameof(caller));
            if (sourceFileName == null) throw new ArgumentNullException(nameof(sourceFileName));

            var folderName = System.IO.Path.GetFileNameWithoutExtension(sourceFileName) + "_" + caller;

            return new TempFolder(folderName);
        }

        public TempRollingFile AllocateFile(string? ext = null)
        {
            return new TempRollingFile(ext, Path);
        }
    }
}
