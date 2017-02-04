using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace TypeScriptCompileOnSave
{
    public static class Transpiler
    {
        private static bool _isProcessing;

        public static async Task<TranspilerResult> Transpile(string cwd)
        {
            if (_isProcessing)
                return TranspilerResult.AlreadyRunning;

            _isProcessing = true;

            try
            {
                string tscExe = GetTscExe();

                ProcessStartInfo start = new ProcessStartInfo(tscExe)
                {
                    WorkingDirectory = cwd,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = System.Diagnostics.Process.Start(start))
                {
                    await process.WaitForExitAsync(TimeSpan.FromSeconds(Constants.CompileTimeout));
                }

                return TranspilerResult.Success;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return TranspilerResult.Fail;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public static bool CanTranspile(this ProjectItem item, out string cwd)
        {
            cwd = null;

            // Already running
            if (_isProcessing)
                return false;

            string fileName = item.FileNames[1];

            try
            {
                // Not the right file extension
                if (!IsFileSupported(fileName))
                    return false;

                // File not in the right project type
                if (!IsProjectSupported(item.ContainingProject))
                    return false;

                // tsconfig.json doesn't exist
                if (!VsHelpers.FileExistAtOrAbove(fileName, Constants.ConfigFileName, out cwd))
                    return false;

                // compileOnSave is set to false
                string configPath = Path.Combine(cwd, Constants.ConfigFileName);
                if (!IsCompileOnSaveEnabled(configPath))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
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

            //var prop = obj["compileOnSave"];

            //if (prop == null)
            //    return true;

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
