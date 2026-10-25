using BetterLyrics.Core.Interfaces.Providers;
using Microsoft.Extensions.Logging;
using System;
using Windows.Security.Credentials;

namespace BetterLyrics.WinUI3.Providers;

/// <summary>
/// Singleton Provider
/// </summary>
public class PasswordVaultProvider : IPasswordVaultProvider
{
    private readonly ILogger<PasswordVaultProvider> _logger;

    public PasswordVaultProvider(ILogger<PasswordVaultProvider> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 保存敏感数据到 PasswordVault
    /// </summary>
    /// <param name="resource">资源标识，比如 "MyApp"</param>
    /// <param name="key">键名，比如 "SessionKey"</param>
    /// <param name="value">要保存的值</param>
    public void Save(string resource, string key, string value)
    {
        try
        {
            var vault = new PasswordVault();

            vault.Add(new PasswordCredential(resource, key, value));
            _logger.LogDebug("Saved credential {Resource}/{Key} to PasswordVault.", resource, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save credential {Resource}/{Key} to PasswordVault.", resource, key);
        }
    }

    /// <summary>
    /// 从 PasswordVault 获取敏感数据
    /// </summary>
    /// <param name="resource">资源标识</param>
    /// <param name="key">键名</param>
    /// <returns>存储的值，若不存在则返回 null</returns>
    public string? Get(string resource, string key)
    {
        try
        {
            var vault = new PasswordVault();

            var credential = vault.Retrieve(resource, key);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Credential {Resource}/{Key} was not found in PasswordVault.", resource, key);
            return null;
        }
    }

    /// <summary>
    /// 删除指定的敏感数据
    /// </summary>
    public void Delete(string resource, string key)
    {
        try
        {
            var vault = new PasswordVault();

            var credential = vault.Retrieve(resource, key);
            vault.Remove(credential);
            _logger.LogDebug("Deleted credential {Resource}/{Key} from PasswordVault.", resource, key);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to delete credential {Resource}/{Key} from PasswordVault.", resource, key);
        }
    }
}