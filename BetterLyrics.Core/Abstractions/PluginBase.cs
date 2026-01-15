using BetterLyrics.Core.Interfaces;
using BetterLyrics.Core.Interfaces.Infrastructure;
using BetterLyrics.Core.Models.SettingsSchema;
using System.Globalization;
using System.Reflection;

namespace BetterLyrics.Core.Abstractions
{
    public abstract class PluginBase<TConfig> : IPlugin, IConfigurable where TConfig : PluginConfigBase, new()
    {
        private bool _isDisposed;

        public TConfig Config { get; } = new TConfig();

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

        private IPluginContext? _context;
        protected IPluginContext Context
        {
            get
            {
                return _context ?? throw new InvalidOperationException("Plugin is not initialized yet! Do not access Context in the constructor.");
            }
        }

        public async Task InitializeAsync(IPluginContext context)
        {
            _context = context;
            Config.Bind(_context.Settings);
            await OnInitializeAsync();
        }
        protected virtual Task OnInitializeAsync() => Task.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed) return;

            await OnShutdownAsync();
            _context = null;
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }
        protected virtual ValueTask OnShutdownAsync()
        {
            return ValueTask.CompletedTask;
        }

        public virtual IEnumerable<SettingDef> GetSettings() => Enumerable.Empty<SettingDef>();
        public virtual void OnConfigChanged(Dictionary<string, object> newConfig) { }
    }
}
