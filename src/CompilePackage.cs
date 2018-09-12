using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Threading;
using Tasks = System.Threading.Tasks;

namespace TypeScriptCompileOnSave
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading =true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [Guid(PackageGuids.guidCompilePackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(PackageGuids.guidAutoLoadString, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideUIContextRule(PackageGuids.guidAutoLoadString,
        name: "Auto load",
        expression: "(DotNetCoreWeb | ProjectK) & (js | jsx)",
        termNames: new[] {
            "DotNetCoreWeb",
            "ProjectK",
            "js",
            "jsx" },
        termValues: new[] {
            "ActiveProjectFlavor:{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}",
            "ActiveProjectCapability:DotNetCoreWeb",
            "HierSingleSelectionName:.js$",
            "HierSingleSelectionName:.jsx$"
    })]
    public sealed class CompilePackage : AsyncPackage
    {
        protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                AddConfigFile.Initialize(this, commandService);
            }
        }
    }
}
