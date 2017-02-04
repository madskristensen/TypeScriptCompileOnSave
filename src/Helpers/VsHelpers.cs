using Microsoft.VisualStudio.Shell;
using System.IO;

namespace TypeScriptCompileOnSave
{
    public static class VsHelpers
    {
        public static TReturnType GetService<TServiceType, TReturnType>()
        {
            return (TReturnType)ServiceProvider.GlobalProvider.GetService(typeof(TServiceType));
        }

        public static bool FileExistInOrAbove(string sourceFile, string fileNameToLookFor, out string directory)
        {
            directory = null;
            var currentDir = new DirectoryInfo(Path.GetDirectoryName(sourceFile));

            while (currentDir != null)
            {
                string config = Path.Combine(currentDir.FullName, fileNameToLookFor);

                if (File.Exists(config))
                {
                    directory = currentDir.FullName;
                    return true;
                }

                currentDir = currentDir.Parent;
            }

            return false;
        }
    }
}
