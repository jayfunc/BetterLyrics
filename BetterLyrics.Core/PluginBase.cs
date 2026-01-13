using BetterLyrics.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace BetterLyrics.Core
{
    public abstract class PluginBase : IPlugin
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        public string Author
        {
            get
            {
                var assembly = this.GetType().Assembly;

                var companyAttr = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
                if (!string.IsNullOrWhiteSpace(companyAttr?.Company))
                {
                    return companyAttr.Company;
                }

                var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
                if (!string.IsNullOrWhiteSpace(copyrightAttr?.Copyright))
                {
                    return copyrightAttr.Copyright;
                }

                return "Unknown Author";
            }
        }
        public string Id
        {
            get
            {
                return this.GetType().Assembly.GetName().Name ?? "UnknownPlugin";
            }
        }
        public DateTime LastUpdated
        {
            get
            {
                var assembly = this.GetType().Assembly;

                var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(a => a.Key == "BuildDate");

                if (metadata != null && DateTime.TryParse(metadata.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    return date;
                }

                if (!string.IsNullOrEmpty(assembly.Location))
                {
                    return File.GetLastWriteTime(assembly.Location);
                }

                return DateTime.MinValue;
            }
        }
        public string Version
        {
            get
            {
                var assembly = this.GetType().Assembly;

                var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var versionStr = attr?.InformationalVersion;

                if (string.IsNullOrWhiteSpace(versionStr))
                {
                    return assembly.GetName().Version?.ToString() ?? "0.0.0";
                }

                int plusIndex = versionStr.IndexOf('+');
                if (plusIndex > 0)
                {
                    return versionStr.Substring(0, plusIndex);
                }

                return versionStr;
            }
        }

        public abstract void OnLoad(IPluginContext context);
        public abstract void OnUnload();
    }
}
