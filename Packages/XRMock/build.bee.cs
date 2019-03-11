using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using Bee;
using Bee.Core;
using Bee.CSharpSupport;
using Bee.DotNet;
using Bee.NativeProgramSupport;
using Bee.NativeProgramSupport.Building.FluentSyntaxHelpers;
using Bee.Toolchain.Lumin;
using Bee.Unity.XRSDK;
using Bee.Unity.XRSDK.AR;
using Unity.BuildSystem.CSharpSupport;
using Unity.BuildSystem.NativeProgramSupport;
using Unity.BuildTools;

namespace Bee.Unity.XRSDK
{
    public static class XRPaths
    {
        public static NPath SourceRoot => ExtraRoot.Combine("Source"); 
        public static NPath ExtraRoot => new NPath("Extra~"); 
    }

    public abstract class NativeProvider
    {
        public NPath SourceDirectory => XRPaths.SourceRoot.Combine(Program.Name);
        public NPath UnityIncludes => XRPaths.SourceRoot.Combine("Common", "Unity");
        public NativeProgram Program { get; }

        static Dictionary<string, string> PlatformMapping = new Dictionary<string, string>()
        {
            { "macosx64", "macOS" },
            { "win64", "Windows" }
        };

        protected virtual NPath GetTargetDirectoryFor(NPath targetPath, ToolChain toolchain)
        {
            return targetPath.Combine(PlatformMapping[toolchain.LegacyPlatformIdentifier]);
        }

        void SetupMetafileCopy(NPath targetPath, ToolChain toolchain)
        {
            CopyTool.Instance().Setup(
                targetPath.Combine(PlatformMapping[toolchain.LegacyPlatformIdentifier] + ".meta"),
                SourceDirectory.Combine(PlatformMapping[toolchain.LegacyPlatformIdentifier] + ".meta"));
        }

        public NativeProvider(string name)
        {
            Program = new NativeProgram(name);
            Program.IncludeDirectories.Add(SourceDirectory);
            Program.IncludeDirectories.Add(UnityIncludes);
            Program.Sources.Add(SourceDirectory);
            Program.Defines.Add(new Func<NativeProgramConfiguration, string>(PlatformDefineFor));
        }

        public IEnumerable<NPath> Setup(NPath targetPath, Func<ToolChain, NativeProgramFormat> formatMapFunc = null)
        {
            formatMapFunc = formatMapFunc ?? new Func<ToolChain, NativeProgramFormat>(t => t.DynamicLibraryFormat);
            foreach (var toolchain in SupportedToolchains)
            {
                var config = new NativeProgramConfiguration(CodeGenFor(toolchain), toolchain, false);
                var binary = Program.SetupSpecificConfiguration(config, formatMapFunc(toolchain));
                var final = Deploy(targetPath, config, binary);
                // Add the binary to the list of package dependencies.
                Backend.Current.AddAliasDependency(Program.Name, final);
                yield return binary;
            }
        }

        protected NPath Deploy(NPath targetPath, NativeProgramConfiguration c, NPath binary)
        {
            var finalPath = GetTargetDirectoryFor(targetPath, c.ToolChain).Combine(binary.FileName);
            if (finalPath.Extension == "dylib")
            {
                finalPath = finalPath.ChangeExtension("bundle");
            }
            if (c.Platform is WindowsPlatform)
            {
                if (c.CodeGen == CodeGen.Debug)
                {
                    var pdb = binary.Parent.Combine($"{binary.FileNameWithoutExtension}_{c.ToolChain.Architecture.DisplayName}.pdb");
                    var finalpdb = GetTargetDirectoryFor(targetPath, c.ToolChain).Combine(pdb.FileName);
                    CopyTool.Instance().Setup(finalpdb, pdb);
                }
            }
            SetupMetafileCopy(targetPath, c.ToolChain);
            CopyTool.Instance().Setup(finalPath + ".meta", SourceDirectory.Combine(finalPath.FileName + ".meta"));
            return CopyTool.Instance().Setup(finalPath, binary);
        }

        public abstract IEnumerable<ToolChain> SupportedToolchains { get; }

        protected virtual CodeGen CodeGenFor(ToolChain tc)
        {
            string @var = Environment.GetEnvironmentVariable("XRSDK_BUILD");
            @var = string.IsNullOrEmpty(@var) ? "debug" : @var.ToLower();
            switch(@var)
            {
                case "master": return CodeGen.Master;
                case "release": return CodeGen.Release;
                case "debug":
                default:
                    return CodeGen.Debug;
            }
        }
        protected virtual string PlatformDefineFor(NativeProgramConfiguration c) => $"PLATFORM_{c.Platform.Name.ToUpper()}=1";
    }
}

namespace Bee.Unity.XRSDK.AR
{
    public class XRMockProvider : NativeProvider
    {
        public static string LibName => "UnityXRMock";
        public static NPath PackagePath => Paths.ProjectRoot.Combine("com.unity.xr.mock", "");
        
        public XRMockProvider()
        : base(LibName)
        { }

        public IEnumerable<NPath> Setup(Func<ToolChain, NativeProgramFormat> formatMapFunc = null)
        {
            return Setup(PackagePath, formatMapFunc);
        }

        public override IEnumerable<ToolChain> SupportedToolchains => new ToolChain[] { ToolChain.Store.Host() };
    }
}

class BuildProgram
{
    static void Main()
    {
        var xrmock = new XRMockProvider();
        xrmock.Setup().ToArray();
    }
}
