using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Eraflo.Catalyst.Core.Save;

namespace Eraflo.Catalyst.Editor.Core
{
    /// <summary>
    /// Editor window to monitor and debug the save system.
    /// Open via Tools > Catalyst > Save Debugger.
    /// </summary>
    public class SaveDebuggerWindow : EditorWindow
    {
        private Vector2 _entityScrollPos;
        private Vector2 _filesScrollPos;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.5; // 500ms

        private SaveableEntity[] _cachedEntities = new SaveableEntity[0];
        private string[] _cachedFiles = new string[0];
        private string _currentSaveName = "autosave";

        [MenuItem("Tools/Catalyst/Save Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<SaveDebuggerWindow>("Save Debugger");
            window.minSize = new Vector2(400, 380);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!_autoRefresh || !Application.isPlaying) return;

            if (EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                RefreshData();
                Repaint();
            }
        }

        private void RefreshData()
        {
            _cachedEntities = FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None);
            RefreshFiles();
        }

        private void RefreshFiles()
        {
            string persistentPath = Application.persistentDataPath;
            var files = new List<string>();
            if (Directory.Exists(persistentPath))
            {
                files.AddRange(Directory.GetFiles(persistentPath, "*.dat"));
                files.AddRange(Directory.GetFiles(persistentPath, "*.sav"));
            }
            _cachedFiles = files.ToArray();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to debug the save system.", MessageType.Info);
                return;
            }

            var saveManager = App.Get<SaveManager>();
            DrawSummary(saveManager);
            EditorGUILayout.Space(4);
            DrawEntitiesSection();
            EditorGUILayout.Space(4);
            DrawFilesSection();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);

            GUILayout.FlexibleSpace();

            if (Application.isPlaying)
            {
                _currentSaveName = EditorGUILayout.TextField(
                    _currentSaveName,
                    EditorStyles.toolbarTextField,
                    GUILayout.Width(80));

                if (GUILayout.Button("Save All", EditorStyles.toolbarButton))
                {
                    _ = App.Get<SaveManager>()?.SaveGame(_currentSaveName);
                }

                if (GUILayout.Button("Load All", EditorStyles.toolbarButton))
                {
                    _ = App.Get<SaveManager>()?.LoadGame(_currentSaveName);
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                {
                    RefreshData();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSummary(SaveManager saveManager)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            int registeredCount = saveManager?.RegisteredEntities?.Count ?? 0;
            EditorGUILayout.LabelField($"Registered Entities: {registeredCount}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Save Files on Disk: {_cachedFiles.Length}");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEntitiesSection()
        {
            EditorGUILayout.LabelField($"Registered Entities ({_cachedEntities.Length})", EditorStyles.boldLabel);

            _entityScrollPos = EditorGUILayout.BeginScrollView(_entityScrollPos, GUILayout.MaxHeight(150));

            if (_cachedEntities.Length == 0)
            {
                EditorGUILayout.HelpBox("No SaveableEntity instances found in the scene.", MessageType.Info);
            }
            else
            {
                foreach (var entity in _cachedEntities)
                {
                    if (entity == null) continue;
                    DrawEntityEntry(entity);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntityEntry(SaveableEntity entity)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(entity.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField(entity.Guid, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilesSection()
        {
            EditorGUILayout.LabelField($"Save Files on Disk ({_cachedFiles.Length})", EditorStyles.boldLabel);

            _filesScrollPos = EditorGUILayout.BeginScrollView(_filesScrollPos, GUILayout.MaxHeight(200));

            if (_cachedFiles.Length == 0)
            {
                EditorGUILayout.HelpBox("No save files found in persistentDataPath.", MessageType.Info);
            }
            else
            {
                foreach (var filePath in _cachedFiles)
                {
                    DrawFileEntry(filePath);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFileEntry(string filePath)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            string fileName = Path.GetFileName(filePath);
            long fileSize = new FileInfo(filePath).Length;
            string sizeLabel = fileSize >= 1024 ? $"{fileSize / 1024} KB" : $"{fileSize} B";

            EditorGUILayout.LabelField(fileName, EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField(sizeLabel, GUILayout.Width(60));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Load", EditorStyles.miniButton, GUILayout.Width(44)))
            {
                string saveName = Path.GetFileNameWithoutExtension(filePath);
                _ = App.Get<SaveManager>()?.LoadGame(saveName);
            }

            if (GUILayout.Button("Delete", EditorStyles.miniButton, GUILayout.Width(52)))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete Save File",
                    $"Are you sure you want to delete '{fileName}'?",
                    "Delete", "Cancel"))
                {
                    File.Delete(filePath);
                    RefreshFiles();
                    Repaint();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
    }
}
