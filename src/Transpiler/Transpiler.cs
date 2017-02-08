using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Minimatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MSbuildProject = Microsoft.Build.Evaluation.Project;
using shell = Microsoft.VisualStudio.Shell.VsShellUtilities;

namespace TypeScriptCompileOnSave
{
    public static class Transpiler
    {
        private static bool _isProcessing;
        private static Dictionary<string, MSbuildProject> _buildProjects = new Dictionary<string, MSbuildProject>();

        public static async Task<TranspilerStatus> Transpile(this ProjectItem item)
        {
            TranspilerStatus status = CanTranspile(item);

            if (status != TranspilerStatus.Ok)
                return status;

            _isProcessing = true;

            try
            {
                return await BuildProject(item.ContainingProject);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private static async Task<TranspilerStatus> BuildProject(Project project)
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    if (!_buildProjects.ContainsKey(project.UniqueName))
                    {
                        var root = Microsoft.Build.Construction.ProjectRootElement.Open(project.FullName);
                        var proj = new MSbuildProject(root);
                        proj.SetGlobalProperty("BuildingProject", "true");
                        _buildProjects[project.UniqueName] = proj;
                    }

                    bool success = _buildProjects[project.UniqueName].Build(new[] { "FindConfigFiles", "CompileTypeScriptWithTsConfig" });

                    return success ? TranspilerStatus.Ok : TranspilerStatus.BuildFailed;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    return TranspilerStatus.Exception;
                }
            });
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

                // Don't run while building
                if (IsBuildingOrDebugging(item.DTE))
                    return TranspilerStatus.NotSupported;

                // tsconfig.json doesn't exist
                if (!VsHelpers.FileExistAtOrAbove(fileName, Constants.ConfigFileName, out string cwd))
                    return TranspilerStatus.NotSupported;

                // tsconfig.json is invalid
                if (!TryGetJsonConfig(cwd, out JObject obj))
                    return TranspilerStatus.ConfigError;

                // compileOnSave is set to false
                if (!IsCompileOnSaveEnabled(obj))
                    return TranspilerStatus.NotSupported;

                // Current file not a source file
                if (!IsSourceFile(obj, cwd, fileName))
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

        private static bool TryGetJsonConfig(string cwd, out JObject obj)
        {
            obj = null;
            try
            {
                string configPath = Path.Combine(cwd, Constants.ConfigFileName);
                string json = File.ReadAllText(configPath);
                obj = JObject.Parse(json);

                return true;
            }
            catch (Exception)
            {
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

        public static bool IsBuildingOrDebugging(_DTE dte)
        {
            var serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            return shell.IsSolutionBuilding(serviceProvider) || dte.Debugger.CurrentMode != dbgDebugMode.dbgDesignMode;
        }

        private static bool IsCompileOnSaveEnabled(JObject tsconfig)
        {
            return tsconfig["compileOnSave"].Value<bool>() && tsconfig["compilerOptions"]["allowJs"].Value<bool>();
        }

        private static bool IsSourceFile(JObject obj, string cwd, string fileName)
        {
            IEnumerable<string> files = obj["files"]?.Values<string>() ?? Enumerable.Empty<string>();
            IEnumerable<string> includes = obj["include"]?.Values<string>() ?? Enumerable.Empty<string>();

            var options = new Options { AllowWindowsPaths = true };
            string relative = fileName.Substring(cwd.Length).Trim('\\');

            foreach (string file in files)
            {
                bool isMatch = Minimatcher.Check(relative, file, options);
                if (isMatch) return true;
            }

            foreach (string pattern in includes)
            {
                bool isMatch = Minimatcher.Check(relative, pattern, options);
                if (isMatch) return true;
            }

            return false;
        }
    }
}
