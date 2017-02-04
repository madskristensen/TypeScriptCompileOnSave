using Microsoft.VisualStudio.Shell;

namespace TypeScriptCompileOnSave
{
    public static class VsHelpers
    {
        public static TReturnType GetService<TServiceType, TReturnType>()
        {
            return (TReturnType)ServiceProvider.GlobalProvider.GetService(typeof(TServiceType));
        }
    }
}
