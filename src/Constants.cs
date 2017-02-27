using System;

namespace TypeScriptCompileOnSave
{
    public class Constants
    {
        public const int CompileTimeout = 10; // seconds
        public const string ConfigFileName = "tsconfig.json";
        public static string TscLocation = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\TypeScript\");

        public static string[] FileExtensions =
        {
            ".js",
            ".jsx"
        };

        public static string[] ProjectGuids =
        {
            "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}", // ASP.NET Core
            "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}", // Project K
        };

        public const string DefaultTsConfig = @"{{
  ""compileOnSave"": true,
  ""compilerOptions"": {{
    ""allowJs"": true,
    ""sourceMap"": true,
    ""jsx"": ""react"",
    ""outFile"": ""wwwroot/js/bundle.js""
  }},
  ""files"": [
    ""{0}""
  ]
}}";
    }
}
