using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Construction;
using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Tasks = System.Threading.Tasks;

namespace TypeScriptCompileOnSave
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid("92479255-4cce-4754-9caa-a0c47a10e055")]
    public sealed class CompilePackage : AsyncPackage
    {
        //protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        //{
        //    var dte = await GetServiceAsync(typeof(DTE)) as DTE2;

        //    var project = dte.Solution.Projects.Item(1);

        //    var root = ProjectRootElement.Open(project.FullName);
        //    var proj = new Microsoft.Build.Evaluation.Project(root);
        //    bool success = proj.Build("CompileTypeScriptWithTSConfig");
        //}
    }
}
