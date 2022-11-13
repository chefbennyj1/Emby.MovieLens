using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;

namespace Emby.MovieLens.ML_Net
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedType.Global
    public class AssemblyManager : IServerEntryPoint
    {
        private static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static bool IsMacOS() => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        private static bool IsLinux() => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        
        private IApplicationPaths ApplicationPaths { get; }
        private ILogger Log { get; }

        public static AssemblyManager Instance { get; set; }
        public AssemblyManager(IApplicationPaths paths, ILogManager logManager)
        {
            ApplicationPaths = paths;
            Log = logManager.GetLogger(Plugin.Instance.Name);
            Instance = this;
        }

        private string GetMaxFactorizationNativeAssemblyName()
        {
            if (IsLinux())   return "libMatrixFactorizationNative.so";
            if (IsWindows()) return "MatrixFactorizationNative.dll";
            if (IsMacOS())   return "libMatrixFactorizationNative.dylib";

            throw new Exception("Unable to find MaxFactorization Library.");
        }

        private static string MatrixFactorizationNativeEmbeddedResourceAssembly()
        {
            var location = string.Empty;
            var architecture = RuntimeInformation.OSArchitecture;
            if (IsLinux())
            {
                switch (architecture)
                {
                    case Architecture.X64   : location += "libMatrixFactorizationNative_Linux64.so";    break;
                    case Architecture.Arm   : location += "libMatrixFactorizationNative_LinuxArm.so";   break;
                    case Architecture.Arm64 : location += "libMatrixFactorizationNative_LinuxArm64.so"; break;
                }
            }
            if (IsWindows())
            {
                switch (architecture)
                {
                    case Architecture.X64 : location += "MatrixFactorizationNative_Win64.dll"; break;
                    case Architecture.X86 : location += "MatrixFactorizationNative_Win86.dll"; break;
                }
            }
            if (IsMacOS())
            {
                switch (architecture)
                {
                    case Architecture.X64   : location += "libMatrixFactorizationNative_OSX64.dylib";    break;
                    case Architecture.Arm64 : location += "libMatrixFactorizationNative_OSXARM64.dylib"; break;
                }
            }

            return location;

        }
        
        public void Dispose()
        {
            
        }

        public void Run()
        {
            // MaxFactorizationNative is a dependency for the ML.Net library.
            // It needs to live the Application Root (System folder).
            // Copy over the appropriate version for the appropriate OS.
            var maxFactorizationNativeLibraryName = GetMaxFactorizationNativeAssemblyName();
            Log.Info($"ML.Net loading dependency {maxFactorizationNativeLibraryName}");
            var matrixFactorizationNativeLibraryEmbeddedResourceStream = GetEmbeddedResourceStream(MatrixFactorizationNativeEmbeddedResourceAssembly());
            
            //Copy the resource into the system root.
            using (var fileStream = new FileStream(Path.Combine(ApplicationPaths.ProgramSystemPath, maxFactorizationNativeLibraryName), FileMode.Create, FileAccess.Write))
            {
                matrixFactorizationNativeLibraryEmbeddedResourceStream?.CopyTo(fileStream);
            }

            //This event will take care of loading the rest of the library we we need to run ML.Net
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            //Don't try and load items that are not in the Microsoft.ML namespace
            if (!args.Name.Contains(".ML") && !args.Name.Contains("Newtonsoft")) return null;
           
            //Don't load the assembly twice
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null) return assembly;

            Log.Info($"ML.Net loading assembly {args.Name} {args.RequestingAssembly}");

            var r1 = Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(s => s.Contains(args.Name.Split(',')[0]));
            if (r1 is null) return null;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(r1))
            {
                byte[] assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                return Assembly.Load(assemblyData);
            }
        }


        public Stream GetEmbeddedResourceStream(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var name = assembly.GetManifestResourceNames().FirstOrDefault(s => s.EndsWith(resourceName));

            return GetType().Assembly.GetManifestResourceStream(name);
        }

        public async Task SaveEmbeddedResourceToFileAsync(Stream embeddedResourceStream, string output)
        {
            using (var fileStream = new FileStream(output, FileMode.Create, FileAccess.Write))
            {
                await embeddedResourceStream.CopyToAsync(fileStream);
            }
        }
       
    }
}
