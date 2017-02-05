using EnvDTE;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TypeScriptCompileOnSave
{
    public static class Transpiler
    {
        private static bool _isProcessing;

        public static async Task<TranspilerStatus> Transpile(this ProjectItem item)
        {
            var status = CanTranspile(item);

            if (status != TranspilerStatus.Ok)
                return status;

            _isProcessing = true;

            try
            {
                string tscExe = GetTscExe();

                ProcessStartInfo start = new ProcessStartInfo(tscExe)
                {
                    WorkingDirectory = Path.GetDirectoryName(item.FileNames[1]),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = System.Diagnostics.Process.Start(start))
                {
                    await process.WaitForExitAsync(TimeSpan.FromSeconds(Constants.CompileTimeout));
                }

                return TranspilerStatus.Ok;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return TranspilerStatus.Exception;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private static TranspilerStatus CanTranspile(this ProjectItem item)
        {
            // Already running
            if (_isProcessing)
                return TranspilerStatus.AlreadyRunning;

            string fileName = item.FileNames[1];

            try
            {
                // Not the right file extension
                if (!IsFileSupported(fileName))
                    return TranspilerStatus.NotSupported;

                // File not in the right project type
                if (!IsProjectSupported(item.ContainingProject))
                    return TranspilerStatus.NotSupported;

                // tsconfig.json doesn't exist
                if (!VsHelpers.FileExistAtOrAbove(fileName, Constants.ConfigFileName, out string cwd))
                    return TranspilerStatus.NotSupported;

                // compileOnSave is set to false
                string configPath = Path.Combine(cwd, Constants.ConfigFileName);
                if (!IsCompileOnSaveEnabled(configPath))
                    return TranspilerStatus.NotSupported;

                return TranspilerStatus.Ok;
            }
            catch (JsonException)
            {
                return TranspilerStatus.ConfigError;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return TranspilerStatus.Exception;
            }
        }

        public static bool IsFileSupported(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            return Constants.FileExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsProjectSupported(Project project)
        {
            return Constants.ProjectGuids.Contains(project?.Kind, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsCompileOnSaveEnabled(string tsconfigFile)
        {
            if (!File.Exists(tsconfigFile))
                return false;

            string json = File.ReadAllText(tsconfigFile);
            var obj = JObject.Parse(json);

            return obj["compileOnSave"].Value<bool>() && obj["compilerOptions"]["allowJs"].Value<bool>();
        }

        private static string GetTscExe()
        {
            if (!Directory.Exists(Constants.TscLocation))
                return null;

            var latest = Directory.GetDirectories(Constants.TscLocation).LastOrDefault();

            if (string.IsNullOrEmpty(latest))
                return null;

            return Path.Combine(latest, "tsc.exe");
        }
    }
}
