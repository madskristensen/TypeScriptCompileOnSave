using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace TypeScriptCompileOnSave
{
    public class Compiler
    {
        private static string _tsDir = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\TypeScript\");
        private static string[] _extensions = { ".js", ".jsx" };
        public static bool _isProcessing;

        public static CompilerResult Compile(string cwd)
        {
            if (_isProcessing)
                return CompilerResult.AlreadyRunning;

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

                using (System.Diagnostics.Process.Start(start))
                {
                    // Makes sure the process handle is disposed
                }

                return CompilerResult.Success;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                return CompilerResult.Fail;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        public static bool CanCompile(string fileName, out string cwd)
        {
            cwd = null;

            try
            {
                string ext = Path.GetExtension(fileName);

                if (!_extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    return false;

                var dte = (DTE2)Package.GetGlobalService(typeof(DTE));
                var item = dte.Solution.FindProjectItem(fileName);

                if (item?.ContainingProject?.Kind != "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") // ASP.NET Core
                    return false;

                if (!HasConfig(fileName, out cwd))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.Write(ex);
                return false;
            }
        }

        private static bool HasConfig(string sourceFile, out string directory)
        {
            directory = null;
            var folder = new DirectoryInfo(Path.GetDirectoryName(sourceFile));

            while (folder != null)
            {
                string config = Path.Combine(folder.FullName, "tsconfig.json");

                if (File.Exists(config))
                {
                    directory = folder.FullName;
                    return true;
                }

                folder = folder.Parent;
            }

            return false;
        }

        private static string GetTscExe()
        {
            if (!Directory.Exists(_tsDir))
                return null;

            var latest = Directory.GetDirectories(_tsDir).LastOrDefault();

            if (string.IsNullOrEmpty(latest))
                return null;

            return Path.Combine(latest, "tsc.exe");
        }
    }
}
