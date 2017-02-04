using System;

namespace TypeScriptCompileOnSave
{
    public class Constants
    {
        public static string TscLocation = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\TypeScript\");

        public static string[] FileExtensions =
        {
            ".js",
            ".jsx"
        };

        public static string[] ProjectGuids =
        {
            "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}" // ASP.NET Core
        };
    }
}
