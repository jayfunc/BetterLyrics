using System.Globalization;
using System.Reflection;
using BetterLyrics.Sdk.Helpers;
using BetterLyrics.Sdk.Interfaces.Plugins;
using BetterLyrics.Sdk.Models.SettingsSchema;

namespace BetterLyrics.Sdk.Abstractions.Plugins;

public abstract class PluginBase<TConfig> : IPlugin where TConfig : PluginConfigBase, new()
{
    private IPluginContext? _context;
    private bool _isDisposed;

    public TConfig Config { get; } = new();

    protected IPluginContext Context => _context ??
                                        throw new InvalidOperationException(
                                            "Plugin is not initialized yet! Do not access Context in the constructor.");

    public abstract string Title { get; set; }

    public string Description
    {
        get
        {
            var assembly = GetType().Assembly;
            var metadata = assembly.GetCustomAttributes<AssemblyDescriptionAttribute>().FirstOrDefault();
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Description)) return metadata.Description;
            return string.Empty;
        }
    }

    public string Author
    {
        get
        {
            var assembly = GetType().Assembly;

            var companyAttr = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            if (!string.IsNullOrWhiteSpace(companyAttr?.Company)) return companyAttr.Company;

            var copyrightAttr = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            if (!string.IsNullOrWhiteSpace(copyrightAttr?.Copyright)) return copyrightAttr.Copyright;

            return "Unknown Author";
        }
    }

    public string Id => GetType().Assembly.GetName().Name ?? "UnknownPlugin";

    public DateTime LastUpdated
    {
        get
        {
            var assembly = GetType().Assembly;

            var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "BuildDate");

            if (metadata != null && DateTime.TryParse(metadata.Value, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var date)) return date;

            if (!string.IsNullOrEmpty(assembly.Location)) return File.GetLastWriteTime(assembly.Location);

            return DateTime.MinValue;
        }
    }

    public string Version
    {
        get
        {
            var assembly = GetType().Assembly;

            var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var versionStr = attr?.InformationalVersion;

            if (string.IsNullOrWhiteSpace(versionStr)) return assembly.GetName().Version?.ToString() ?? "0.0.0";

            var plusIndex = versionStr.IndexOf('+');
            if (plusIndex > 0) return versionStr.Substring(0, plusIndex);

            return versionStr;
        }
    }

    public string RepositoryUrl
    {
        get
        {
            var assembly = GetType().Assembly;
            var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "RepositoryUrl");
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.Value)) return metadata.Value;
            return string.Empty;
        }
    }

    public async Task InitializeAsync(IPluginContext context)
    {
        _context = context;
        Config.BindConfigurator(_context.Configurator);
        await OnInitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        await OnShutdownAsync();
        _context = null;
        _isDisposed = true;

        GC.SuppressFinalize(this);
    }

    public Dictionary<string, SettingDef> GetSettingDefDict()
    {
        var dict = new Dictionary<string, SettingDef>();

        var props = typeof(TConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            var value = prop.GetValue(Config) ?? Context.Configurator.Get(prop.Name, default);

            SettingDef? settingDef = null;

            switch (value)
            {
                case string:
                    settingDef = SettingBuilder.Text(prop, Context.Localizer, (string)value);
                    break;
                case bool:
                    settingDef = SettingBuilder.Bool(prop, Context.Localizer, (bool)value);
                    break;
                case double:
                    settingDef = SettingBuilder.Number(prop, Context.Localizer, (double)value);
                    break;
                case float:
                    settingDef = SettingBuilder.Number(prop, Context.Localizer, (float)value);
                    break;
                case int:
                    settingDef = SettingBuilder.Number(prop, Context.Localizer, (int)value);
                    break;
                case Array:
                    settingDef = SettingBuilder.Choice(prop, Context.Localizer, ((Array)value).Cast<string>().ToList(),
                        ((Array)value).GetValue(0)?.ToString() ?? string.Empty);
                    break;
            }

            if (settingDef == null) continue;

            dict.Add(prop.Name, settingDef);
        }

        return dict;
    }

    protected virtual Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnShutdownAsync()
    {
        return Task.CompletedTask;
    }
}