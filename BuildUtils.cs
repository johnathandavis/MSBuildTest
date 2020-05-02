using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace MSBuildTest
{
    public class BuildUtils
    {
        public static ProjectType GetProjectType(string csProj)
        {
            using (XmlReader reader = XmlReader.Create(csProj))
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.HasAttributes)
                {
                    if (reader.MoveToAttribute("Sdk"))
                    {
                        return ProjectType.Core;
                    }
                }    
            }

            return ProjectType.Framework;
        }

        public static Dictionary<string, string> GetProjectGlobalProperties(string projectPath)
        {
            ProjectType pj = GetProjectType(projectPath);
            string toolsPath = ToolsPathfinder.GetToolsPath(projectPath, pj);
            if (pj == ProjectType.Core)
            {
                return GetCoreGlobalProperties(projectPath, toolsPath);
            }
            else
            {
                return GetFrameworkGlobalProperties(projectPath, toolsPath);
            }
        }
        
        public static Dictionary<string, string> GetCoreGlobalProperties(string projectPath, string toolsPath)
        {
            string solutionDir = Path.GetDirectoryName(projectPath);
            string extensionsPath = toolsPath;
            string sdksPath = Path.Combine(toolsPath, "Sdks");
            string roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            Environment.SetEnvironmentVariable(
                "MSBuildExtensionsPath",
                extensionsPath);
            Environment.SetEnvironmentVariable(
                "MSBuildSDKsPath",
                sdksPath);
            
            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir },
                { "MSBuildExtensionsPath", extensionsPath },
                { "MSBuildSDKsPath", sdksPath },
                { "RoslynTargetsPath", roslynTargetsPath }
            };
        }

        public static Dictionary<string, string> GetFrameworkGlobalProperties(string projectPath, string toolsPath)
        {
            string solutionDir = Path.GetDirectoryName(projectPath);
            string extensionsPath = Path.GetFullPath(Path.Combine(toolsPath, @"..\..\"));
            string sdksPath = Path.Combine(extensionsPath, "Sdks");
            string roslynTargetsPath = Path.Combine(toolsPath, "Roslyn");

            return new Dictionary<string, string>
            {
                { "SolutionDir", solutionDir },
                { "MSBuildExtensionsPath", extensionsPath },
                { "MSBuildSDKsPath", sdksPath },
                { "RoslynTargetsPath", roslynTargetsPath }
            };
        }
    }
}