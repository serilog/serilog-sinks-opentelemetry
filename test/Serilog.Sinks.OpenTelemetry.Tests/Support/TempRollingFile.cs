namespace Serilog.Sinks.OpenTelemetry.Tests.Support
{
    internal class TempRollingFile
    {
        private string _ext;
        private string _path;
        private string _guid;

        public TempRollingFile(string? ext, string path)
        {
            _guid = Guid.NewGuid().ToString("n");
            _ext = ext ?? "tmp";
            _path = path;
        }

        public string FileConfigurationPath => Path.Combine(_path, _guid + "." + _ext);

        public string RollingFilePath => Path.Combine(_path, _guid + DateTime.Now.ToString("yyyyMMdd") + "." + _ext);
        
    }
}
