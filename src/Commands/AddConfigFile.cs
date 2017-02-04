using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.IO;

namespace TypeScriptCompileOnSave
{
    internal sealed class AddConfigFile
    {
        private readonly Package _package;
        private ProjectItem _item;

        private AddConfigFile(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidCompilePackageCmdSet, PackageIds.AddConfigFileId);
            var cmd = new OleMenuCommand(Execute, cmdId);
            cmd.BeforeQueryStatus += BeforeQueryStatus;
            commandService.AddCommand(cmd);
        }

        public static AddConfigFile Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new AddConfigFile(package, commandService);
        }

        private void BeforeQueryStatus(object sender, EventArgs e)
        {
            var button = (OleMenuCommand)sender;
            button.Visible = button.Enabled = false;

            DTE2 dte = VsHelpers.GetService<DTE, DTE2>();
            _item = dte.SelectedItems.Item(1).ProjectItem;

            if (dte.SelectedItems.MultiSelect || !Transpiler.IsProjectSupported(_item.ContainingProject))
                return;

            string fileName = _item.FileNames[1];

            if (!Transpiler.IsFileSupported(fileName))
                return;

            button.Visible = true;

            if (VsHelpers.FileExistInOrAbove(fileName, "tsconfig.json", out string cwd))
            {
                button.Text = "Transpile to JavaScript (tsconfig.json found)";
                button.Enabled = false;
            }
            else
            {
                button.Text = "Transpile to JavaScript";
                button.Enabled = true;
            }
        }

        private async void Execute(object sender, EventArgs e)
        {
            if (_item == null || _item.ContainingProject == null)
                return;

            string projectRoot = _item.ContainingProject.Properties.Item("FullPath").Value.ToString();

            if (Directory.Exists(projectRoot))
            {
                string sourceDir = Path.GetDirectoryName(_item.FileNames[1]);
                string sourceExt = Path.GetExtension(_item.FileNames[1]);
                string glob = sourceDir.Substring(projectRoot.Length + 1).Replace("\\", "/") + $"/**/*{sourceExt}";
                string configPath = Path.Combine(projectRoot, Constants.ConfigFileName);
                string content = string.Format(Constants.DefaultTsConfig, glob);

                File.WriteAllText(configPath, content);

                _item.DTE.ItemOperations.OpenFile(configPath);
                _item.DTE.ExecuteCommand("SolutionExplorer.SyncWithActiveDocument");

                if (Transpiler.CanCompile(_item, out string cwd))
                {
                    await Transpiler.Transpile(cwd);
                }
            }
        }
    }
}
