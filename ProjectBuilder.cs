using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Build.Framework.ILogger;

namespace MSBuildTest
{
    public class ProjectBuilder
    {
        private const string DOTNET_CLI_UI_LANGUAGE = nameof(DOTNET_CLI_UI_LANGUAGE);

        private readonly string _projectDirectory;
        private readonly Project _project;
        private readonly StringBuilder _stringBuilder;
        private readonly ILogger<ProjectBuilder> _logger;
        
        public ProjectBuilder(string csProj, ILogger<ProjectBuilder> logger)
        {
            _projectDirectory = Path.GetDirectoryName(csProj);
            _logger = logger;
            _stringBuilder = new StringBuilder();
            Type = BuildUtils.GetProjectType(csProj);
            Dictionary<string, string> globalProperties = BuildUtils.GetProjectGlobalProperties(csProj);
            _logger.LogInformation($"Set the following global MSBuild properties:");
            foreach (var kvp in globalProperties)
            {
                _logger.LogInformation($"{kvp.Key}: {kvp.Value}");
            }
            
            string toolsPath = ToolsPathfinder.GetToolsPath(csProj, Type);
            ProjectCollection projectCollection = new ProjectCollection(globalProperties);
            ConsoleLogger stringBuilderLogger = new ConsoleLogger(LoggerVerbosity.Normal, x => _stringBuilder.Append(x), null, null);
            //ConsoleLogger iloggerLogger = new ConsoleLogger(LoggerVerbosity.Diagnostic, x => _logger.Log(LogLevel.Information, x), null, null);
            ConsoleLogger loggerLogger = new ConsoleLogger();
            projectCollection.RegisterLogger(stringBuilderLogger);
            //projectCollection.RegisterLogger(stringBuilderLogger);
            projectCollection.RegisterLogger(loggerLogger);
            projectCollection.AddToolset(new Toolset(ToolLocationHelper.CurrentToolsVersion, toolsPath, projectCollection, string.Empty));
            _project = projectCollection.LoadProject(csProj);
        }
        
        public ProjectType Type { get; }

        public void Build()
        {
            // This forces the referenced msbuild runtime to be loaded into memory,
            // otherwise the on-disk version may be used instead
            new Copy();
            _stringBuilder.Clear();
            
            ProjectInstance projectInstance = _project.CreateProjectInstance();
            RestoreNugetPackages();
            
            if (!projectInstance.Build("Publish", new List<ILogger>(new []{ new ConsoleLogger() })))
            {
                string error = _stringBuilder.ToString();
                _stringBuilder.Clear();
                throw new Exception($"Could not Publish project.\n{error}");
            }
            _stringBuilder.Clear();

        }
        
        
        private void RestoreNugetPackages()
        {
            // Ensure that we set the DOTNET_CLI_UI_LANGUAGE environment variable to "en-US" before
            // running 'dotnet --info'. Otherwise, we may get localized results.
            string originalCliLanguage = Environment.GetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE);
            Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, "en-US");

            try
            {
                // Create the process info
                ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "restore")
                {
                    // global.json may change the version, so need to set working directory
                    WorkingDirectory = _projectDirectory,
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
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(DOTNET_CLI_UI_LANGUAGE, originalCliLanguage);
            }
        }

    }
}