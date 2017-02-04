using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TypeScriptCompileOnSave
{
    public class Transpiler
    {
        private static bool _isProcessing;

        public static async Task Transpile(string cwd)
        {
            IVsStatusbar statusBar = VsHelpers.GetService<SVsStatusbar, IVsStatusbar>();

            statusBar.SetText("Transpiling JavaScript...");
            statusBar.FreezeOutput(1);

            var result = await StartProcess(cwd);

            statusBar.FreezeOutput(0);

            if (result == TranspilerResult.Fail)
            {
                statusBar.SetText("Transpilation failed");
            }
            else
            {
                statusBar.Clear();
            }
        }

        private static async Task<TranspilerResult> StartProcess(string cwd)
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

                await Task.Run(() =>
                {
                    using (System.Diagnostics.Process.Start(start))
                    {
                        // Makes sure the process handle is disposed
                    }
                });

                return TranspilerResult.Success;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                return TranspilerResult.Fail;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public static bool CanCompile(ProjectItem item, out string cwd)
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
                if (!VsHelpers.FileExistInOrAbove(fileName, "tsconfig.json", out cwd))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
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
