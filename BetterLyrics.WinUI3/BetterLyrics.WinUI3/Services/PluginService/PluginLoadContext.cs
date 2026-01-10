using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace BetterLyrics.WinUI3.Services.PluginService
{
    public class PluginLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var sharedAssemblies = new HashSet<string>
            {
                "BetterLyrics.Core",
                "Microsoft.WindowsAppSDK",
                "Microsoft.UI",
                "Microsoft.UI.Xaml",
                "Microsoft.Graphics",
                "System.Runtime",
                "Newtonsoft.Json"
            };

            if (assemblyName.Name == null || sharedAssemblies.Contains(assemblyName.Name))
            {
                return null;
            }

            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            return IntPtr.Zero;
        }

    }
}
