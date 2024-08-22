// Copyright © Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace Serilog.Sinks.OpenTelemetry.FileFallback
{
    /// <summary>
    /// Represents a fallback mechanism that allows logs to be written to the file system
    /// when the primary logging export fails.
    /// </summary>
    public struct FileSystemFallback
    {
        internal FileSinkOptions FileOptions { get; private set; }

        internal bool IsEnabled { get; private set; }

        internal LogFormat LogFormat { get; private set; } // defaults to json

        /// <summary>
        /// Enables the file system fallback and sets the path where logs will be stored 
        /// in case of failure to export to the primary logging destination. 
        /// </summary>
        /// <param name="configuration">The configuration action to configure
        /// the file sink used as a fallback.</param>
        /// <param name="logFormat">The format in which the fallback logs will be written,
        /// See <see cref="LogFormat"/> for available formats.</param>
        /// <returns>A configured <see cref="FileSystemFallback"/> instance with fallback enabled 
        /// and the specified file path.</returns>
        public static FileSystemFallback Configure(Action<FileSinkOptions> configuration, LogFormat logFormat = LogFormat.NDJson)
        {
            var options = new FileSinkOptions();
            configuration(options);
            return new FileSystemFallback
            {
                FileOptions = options,
                IsEnabled = true,
                LogFormat = logFormat
            };
        }

        /// <summary>
        /// Disables the file system fallback.
        /// </summary>
        public static readonly FileSystemFallback None = default;
    }
}
