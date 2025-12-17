using System;
using Windows.Security.Credentials;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PasswordVaultHelper
    {
        /// <summary>
        /// 保存敏感数据到 PasswordVault
        /// </summary>
        /// <param name="resource">资源标识，比如 "MyApp"</param>
        /// <param name="key">键名，比如 "SessionKey"</param>
        /// <param name="value">要保存的值</param>
        public static void Save(string resource, string key, string value)
        {
            try
            {
                var vault = new PasswordVault();

                vault.Add(new PasswordCredential(resource, key, value));
            }
            catch (Exception) { }
        }

        /// <summary>
        /// 从 PasswordVault 获取敏感数据
        /// </summary>
        /// <param name="resource">资源标识</param>
        /// <param name="key">键名</param>
        /// <returns>存储的值，若不存在则返回 null</returns>
        public static string? Get(string resource, string key)
        {
            try
            {
                var vault = new PasswordVault();

                var credential = vault.Retrieve(resource, key);
                credential.RetrievePassword();
                return credential.Password;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 删除指定的敏感数据
        /// </summary>
        public static void Delete(string resource, string key)
        {
            try
            {
                var vault = new PasswordVault();

                var credential = vault.Retrieve(resource, key);
                vault.Remove(credential);
            }
            catch (Exception) { }
        }
    }
}