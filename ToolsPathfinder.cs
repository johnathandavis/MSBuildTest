using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;

namespace MSBuildTest
{
    public static class ToolsPathfinder
    {
        private const string DOTNET_CLI_UI_LANGUAGE = nameof(DOTNET_CLI_UI_LANGUAGE);
        
        public static string GetToolsPath(string project, ProjectType projectType)
        {
            if (projectType == ProjectType.Framework)
            {
                return GetFrameworkToolsPath();
            }
            else
            {
                return GetCoreBasePath(project);
            }
        }
        
        public static string GetFrameworkToolsPath()
        {
            string toolsPath = ToolLocationHelper.GetPathToBuildToolsFile("msbuild.exe", ToolLocationHelper.CurrentToolsVersion);
            if (string.IsNullOrEmpty(toolsPath))
            {
                toolsPath = PollForToolsPath().FirstOrDefault();
            }
            if (string.IsNullOrEmpty(toolsPath))
            {
                throw new Exception("Could not locate the tools (MSBuild) path.");
            }
            return Path.GetDirectoryName(toolsPath);
        }
        
        private static string[] PollForToolsPath()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            return new[]
            {
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\14.0\Bin\MSBuild.exe"),
                Path.Combine(programFilesX86, @"MSBuild\12.0\Bin\MSBuild.exe")
            }.Where(File.Exists).ToArray();
        }
        
        public static string GetCoreBasePath(string projectPath)
        {
            // Ensure that we set the DOTNET_CLI_UI_LANGUAGE environment variable to "en-US" before
            // running 'dotnet --info'. Otherwise, we may get localized results.
            string originalCliLanguage = Environment.GetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE);
            Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, "en-US");

            try
            {
                // Create the process info
                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "--info")
                {
                    // global.json may change the version, so need to set working directory
                    WorkingDirectory = Path.GetDirectoryName(projectPath),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                // Execute the process
                using (Process process = Process.Start(startInfo))
                {
                    List<string> lines = new List<string>();
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data))
                        {
                            lines.Add(e.Data);
                        }
                    };
                    process.BeginOutputReadLine();
                    process.WaitForExit();
                    return ParseCoreBasePath(lines);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, originalCliLanguage);
            }
        }

        private static string ParseCoreBasePath(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
            {
                throw new Exception("Could not get results from `dotnet --info` call");
            }

            foreach (string line in lines)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex >= 0 
                    && line.Substring(0, colonIndex).Trim().Equals("Base Path", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(colonIndex + 1).Trim();
                }
            }

            throw new Exception("Could not locate base path in `dotnet --info` results");
        }
    }
}