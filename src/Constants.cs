using System;

namespace TypeScriptCompileOnSave
{
    public class Constants
    {
        public const string ConfigFileName = "tsconfig.json";
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

        public const string DefaultTsConfig = @"{{
  ""include"": [ ""{0}"" ],
  ""compilerOptions"": {{
    ""allowJs"": true,
    ""sourceMap"": true,
    ""outFile"": ""wwwroot/js/bundle.js""
  }}
}}";
    }
}
