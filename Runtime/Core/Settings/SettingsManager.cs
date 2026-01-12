using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Core.Settings
{
    [Service(Priority = 8)]
    public class SettingsManager : IGameService
    {
        private string SettingsFileName => PackageSettings.Instance.SettingsFilename;
        
        private SettingsData _data;
        private ISerializer _serializer;
        private IStorageBackend _storage;
        private readonly List<ISettingsPage> _pages = new List<ISettingsPage>();

        public SettingsData Data => _data;
        public IEnumerable<ISettingsPage> Pages => _pages;

        public void Initialize()
        {
            _serializer = _serializer ?? new JsonSerializer();
            _storage = _storage ?? new LocalDiskStorage();
            
            LoadSync();

            // Auto-register default pages
            RegisterPage(new AudioSettingsPage());
            RegisterPage(new VideoSettingsPage());
            
            ApplyAll();
        }

        public void Shutdown()
        {
            SaveSync();
        }

        public void RegisterPage(ISettingsPage page)
        {
            if (!_pages.Any(p => p.Id == page.Id))
            {
                _pages.Add(page);
                // Apply the new page immediately with current data
                page.Apply(_data);
            }
        }

        public ISettingsPage GetPage(string id) => _pages.FirstOrDefault(p => p.Id == id);

        #region Persistence

        public async Task SaveAsync()
        {
            try
            {
                byte[] bytes = _serializer.Serialize(_data);
                await _storage.SaveAsync(SettingsFileName, bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SettingsManager] Failed to save settings: {e.Message}");
            }
        }

        public void SaveSync() => SaveAsync().Wait();

        private void LoadSync()
        {
            if (!_storage.Exists(SettingsFileName))
            {
                _data = new SettingsData();
                return;
            }

            try
            {
                var task = _storage.LoadAsync(SettingsFileName);
                task.Wait();
                byte[] bytes = task.Result;
                _data = bytes != null ? _serializer.Deserialize<SettingsData>(bytes) : new SettingsData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SettingsManager] Failed to load settings: {e.Message}");
                _data = new SettingsData();
            }
        }

        #endregion

        #region Application Logic

        public void ApplyAll()
        {
            foreach (var page in _pages)
            {
                page.Apply(_data);
            }
        }

        public void ApplyPage(string pageId)
        {
            var page = GetPage(pageId);
            page?.Apply(_data);
        }

        #endregion

        #region Generic Accessors

        public T GetSetting<T>(string key, T defaultValue = default)
        {
            // First check strongly typed fields if we want, but for now we look in Custom
            if (_data.CustomSettings.TryGetValue(key, out string value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { return defaultValue; }
            }
            return defaultValue;
        }

        public void SetSetting<T>(string key, T value)
        {
            _data.CustomSettings[key] = value?.ToString() ?? string.Empty;
        }

        #endregion
    }
}
