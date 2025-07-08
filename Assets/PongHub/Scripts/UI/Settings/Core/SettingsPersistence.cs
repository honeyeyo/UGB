using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace PongHub.UI.Settings.Core
{
    /// <summary>
    /// 设置持久化管理器 - 负责设置数据的保存和加载
    /// Settings Persistence Manager - Handles saving and loading of settings data
    /// </summary>
    public class SettingsPersistence : MonoBehaviour
    {
        [Header("持久化配置")]
        [SerializeField]
        [Tooltip("使用PlayerPrefs作为备用存储")]
        private bool usePlayerPrefsBackup = true;

        [SerializeField]
        [Tooltip("启用数据加密")]
        private bool enableEncryption = false;

        [SerializeField]
        [Tooltip("备份文件数量")]
        private int backupCount = 3;

        // 私有字段
        private string settingsFilePath;
        private string settingsDirectory;
        private string encryptionKey = "PongHub_Settings_Key_2025";

        /// <summary>
        /// 初始化持久化管理器
        /// </summary>
        public void Initialize(string fileName)
        {
            // 设置文件路径
            settingsDirectory = Path.Combine(Application.persistentDataPath, "Settings");
            settingsFilePath = Path.Combine(settingsDirectory, fileName);

            // 确保目录存在
            if (!Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }

            Debug.Log($"Settings path: {settingsFilePath}");
        }

        #region 异步保存和加载

        /// <summary>
        /// 异步保存数据
        /// </summary>
        public async Task SaveAsync<T>(T data) where T : class
        {
            try
            {
                // 序列化数据
                string jsonData = JsonUtility.ToJson(data, true);

                // 加密数据（如果启用）
                if (enableEncryption)
                {
                    jsonData = EncryptData(jsonData);
                }

                // 创建备份
                await CreateBackupAsync();

                // 写入文件
                await WriteToFileAsync(settingsFilePath, jsonData);

                // 保存到PlayerPrefs作为备用（如果启用）
                if (usePlayerPrefsBackup)
                {
                    SaveToPlayerPrefs(data);
                }

                Debug.Log("Settings saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save settings: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 异步加载数据
        /// </summary>
        public async Task<T> LoadAsync<T>() where T : class, new()
        {
            try
            {
                // 首先尝试从文件加载
                if (File.Exists(settingsFilePath))
                {
                    string jsonData = await ReadFromFileAsync(settingsFilePath);

                    // 解密数据（如果启用）
                    if (enableEncryption)
                    {
                        jsonData = DecryptData(jsonData);
                    }

                    // 反序列化
                    T data = JsonUtility.FromJson<T>(jsonData);
                    if (data != null)
                    {
                        Debug.Log("Settings loaded from file");
                        return data;
                    }
                }

                // 如果文件加载失败，尝试从PlayerPrefs加载
                if (usePlayerPrefsBackup)
                {
                    T backupData = LoadFromPlayerPrefs<T>();
                    if (backupData != null)
                    {
                        Debug.Log("Settings loaded from PlayerPrefs backup");
                        return backupData;
                    }
                }

                // 如果都失败了，返回默认值
                Debug.LogWarning("No settings found, creating default");
                return new T();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");

                // 尝试加载备份
                T backupData = await LoadFromBackupAsync<T>();
                if (backupData != null)
                {
                    Debug.Log("Settings loaded from backup");
                    return backupData;
                }

                return new T();
            }
        }

        #endregion

        #region 导入导出

        /// <summary>
        /// 导出数据到指定文件
        /// </summary>
        public async Task ExportToFileAsync<T>(T data, string filePath) where T : class
        {
            try
            {
                string jsonData = JsonUtility.ToJson(data, true);
                await WriteToFileAsync(filePath, jsonData);
                Debug.Log($"Data exported to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export data: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 从指定文件导入数据
        /// </summary>
        public async Task<T> ImportFromFileAsync<T>(string filePath) where T : class
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                string jsonData = await ReadFromFileAsync(filePath);
                T data = JsonUtility.FromJson<T>(jsonData);

                Debug.Log($"Data imported from: {filePath}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import data: {e.Message}");
                throw;
            }
        }

        #endregion

        #region 备份管理

        /// <summary>
        /// 创建备份文件
        /// </summary>
        private async Task CreateBackupAsync()
        {
            try
            {
                if (!File.Exists(settingsFilePath))
                    return;

                // 生成备份文件名（带时间戳）
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"settings_backup_{timestamp}.json";
                string backupPath = Path.Combine(settingsDirectory, "Backups");

                // 确保备份目录存在
                if (!Directory.Exists(backupPath))
                {
                    Directory.CreateDirectory(backupPath);
                }

                string backupFilePath = Path.Combine(backupPath, backupFileName);

                // 复制当前设置文件
                string currentData = await ReadFromFileAsync(settingsFilePath);
                await WriteToFileAsync(backupFilePath, currentData);

                // 清理旧备份
                await CleanupOldBackupsAsync(backupPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to create backup: {e.Message}");
            }
        }

        /// <summary>
        /// 从备份加载数据
        /// </summary>
        private async Task<T> LoadFromBackupAsync<T>() where T : class, new()
        {
            try
            {
                string backupPath = Path.Combine(settingsDirectory, "Backups");
                if (!Directory.Exists(backupPath))
                    return null;

                string[] backupFiles = Directory.GetFiles(backupPath, "settings_backup_*.json");
                if (backupFiles.Length == 0)
                    return null;

                // 按修改时间排序，获取最新的备份
                Array.Sort(backupFiles, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));

                foreach (string backupFile in backupFiles)
                {
                    try
                    {
                        string jsonData = await ReadFromFileAsync(backupFile);

                        // 解密数据（如果启用）
                        if (enableEncryption)
                        {
                            jsonData = DecryptData(jsonData);
                        }

                        T data = JsonUtility.FromJson<T>(jsonData);
                        if (data != null)
                        {
                            return data;
                        }
                    }
                    catch
                    {
                        // 继续尝试下一个备份文件
                        continue;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load from backup: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清理旧备份文件
        /// </summary>
        private async Task CleanupOldBackupsAsync(string backupPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    string[] backupFiles = Directory.GetFiles(backupPath, "settings_backup_*.json");
                    if (backupFiles.Length <= backupCount)
                        return;

                    // 按修改时间排序
                    Array.Sort(backupFiles, (x, y) => File.GetLastWriteTime(y).CompareTo(File.GetLastWriteTime(x)));

                    // 删除超出数量的旧备份
                    for (int i = backupCount; i < backupFiles.Length; i++)
                    {
                        File.Delete(backupFiles[i]);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to cleanup old backups: {e.Message}");
                }
            });
        }

        #endregion

        #region PlayerPrefs 备用存储

        /// <summary>
        /// 保存到PlayerPrefs
        /// </summary>
        private void SaveToPlayerPrefs<T>(T data) where T : class
        {
            try
            {
                string jsonData = JsonUtility.ToJson(data);
                PlayerPrefs.SetString("GameSettings_Backup", jsonData);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to save to PlayerPrefs: {e.Message}");
            }
        }

        /// <summary>
        /// 从PlayerPrefs加载
        /// </summary>
        private T LoadFromPlayerPrefs<T>() where T : class
        {
            try
            {
                if (PlayerPrefs.HasKey("GameSettings_Backup"))
                {
                    string jsonData = PlayerPrefs.GetString("GameSettings_Backup");
                    return JsonUtility.FromJson<T>(jsonData);
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load from PlayerPrefs: {e.Message}");
                return null;
            }
        }

        #endregion

        #region 文件操作辅助方法

        /// <summary>
        /// 异步写入文件
        /// </summary>
        private async Task WriteToFileAsync(string filePath, string data)
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                await writer.WriteAsync(data);
            }
        }

        /// <summary>
        /// 异步读取文件
        /// </summary>
        private async Task<string> ReadFromFileAsync(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                return await reader.ReadToEndAsync();
            }
        }

        #endregion

        #region 数据加密（简单实现）

        /// <summary>
        /// 加密数据（简单XOR加密）
        /// </summary>
        private string EncryptData(string data)
        {
            try
            {
                char[] chars = data.ToCharArray();
                char[] keyChars = encryptionKey.ToCharArray();

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)(chars[i] ^ keyChars[i % keyChars.Length]);
                }

                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(chars));
            }
            catch (Exception e)
            {
                Debug.LogError($"Encryption failed: {e.Message}");
                return data; // 返回原始数据
            }
        }

        /// <summary>
        /// 解密数据
        /// </summary>
        private string DecryptData(string encryptedData)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(encryptedData);
                char[] chars = System.Text.Encoding.UTF8.GetString(bytes).ToCharArray();
                char[] keyChars = encryptionKey.ToCharArray();

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)(chars[i] ^ keyChars[i % keyChars.Length]);
                }

                return new string(chars);
            }
            catch (Exception e)
            {
                Debug.LogError($"Decryption failed: {e.Message}");
                return encryptedData; // 返回原始数据
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 删除所有设置数据
        /// </summary>
        public void ClearAllSettings()
        {
            try
            {
                // 删除主设置文件
                if (File.Exists(settingsFilePath))
                {
                    File.Delete(settingsFilePath);
                }

                // 清除PlayerPrefs备份
                if (PlayerPrefs.HasKey("GameSettings_Backup"))
                {
                    PlayerPrefs.DeleteKey("GameSettings_Backup");
                    PlayerPrefs.Save();
                }

                // 清除备份目录
                string backupPath = Path.Combine(settingsDirectory, "Backups");
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                }

                Debug.Log("All settings cleared");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to clear settings: {e.Message}");
            }
        }

        #endregion
    }
}