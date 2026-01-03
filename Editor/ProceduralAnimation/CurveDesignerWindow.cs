#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
using Eraflo.Catalyst.ProceduralAnimation.Studio;

namespace Eraflo.Catalyst.ProceduralAnimation.Editor
{
    /// <summary>
    /// Visual editor window for designing 3D spatial curves.
    /// </summary>
    public class CurveDesignerWindow : EditorWindow
    {
        private SpatialCurve _curve;
        private SpatialCurveAsset _asset;
        private Vector2 _scrollPosition;
        
        // View settings
        private float _previewTime;
        private bool _animatePreview;
        private float _animationSpeed = 1f;
        private bool _showTangents = true;
        private bool _showFrames = true;
        private int _previewSamples = 50;
        
        // Selection
        private int _selectedPointIndex = -1;
        private Tool _currentTool = Tool.Select;
        
        // Preview mesh
        private GameObject _previewObject;
        
        [MenuItem("Window/Catalyst/Curve Designer")]
        public static void ShowWindow()
        {
            var window = GetWindow<CurveDesignerWindow>();
            window.titleContent = new GUIContent("Curve Designer", EditorGUIUtility.IconContent("EditCollider").image);
            window.minSize = new Vector2(350, 400);
        }
        
        private void OnEnable()
        {
            _curve = new SpatialCurve(CurveType.CatmullRom);
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            CleanupPreview();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);
            
            DrawHeader();
            EditorGUILayout.Space();
            DrawAssetSection();
            EditorGUILayout.Space();
            DrawCurveSettings();
            EditorGUILayout.Space();
            DrawPointList();
            EditorGUILayout.Space();
            DrawPreviewSection();
            EditorGUILayout.Space();
            DrawPresetSection();
            
            EditorGUILayout.EndVertical();
            
            // Animate preview
            if (_animatePreview)
            {
                _previewTime += Time.deltaTime * _animationSpeed;
                if (_previewTime > 1f) _previewTime = 0f;
                Repaint();
                SceneView.RepaintAll();
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Curve Designer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Design 3D trajectories for procedural animations. " +
                "Click in Scene View to add points.", MessageType.Info);
        }
        
        private void DrawAssetSection()
        {
            EditorGUILayout.LabelField("Asset", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            _asset = (SpatialCurveAsset)EditorGUILayout.ObjectField("Curve Asset", _asset, 
                typeof(SpatialCurveAsset), false);
            if (EditorGUI.EndChangeCheck() && _asset != null)
            {
                _curve = _asset.Curve ?? new SpatialCurve();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New"))
            {
                _curve = new SpatialCurve(CurveType.CatmullRom);
                _asset = null;
            }
            
            if (GUILayout.Button("Save As..."))
            {
                SaveCurveAsAsset();
            }
            
            GUI.enabled = _asset != null;
            if (GUILayout.Button("Save"))
            {
                SaveToAsset();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawCurveSettings()
        {
            EditorGUILayout.LabelField("Curve Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            var newType = (CurveType)EditorGUILayout.EnumPopup("Interpolation", _curve.Type);
            if (newType != _curve.Type)
            {
                _curve.Type = newType;
            }
            
            bool closed = EditorGUILayout.Toggle("Closed Loop", _curve.IsClosed);
            _curve.SetClosed(closed);
            
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
            
            // Info
            EditorGUILayout.LabelField($"Points: {_curve.PointCount}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Length: {_curve.CalculateLength():F2}m", EditorStyles.miniLabel);
        }
        
        private void DrawPointList()
        {
            EditorGUILayout.LabelField("Control Points", EditorStyles.boldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.MaxHeight(150));
            
            for (int i = 0; i < _curve.PointCount; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool isSelected = _selectedPointIndex == i;
                GUI.backgroundColor = isSelected ? Color.cyan : Color.white;
                
                if (GUILayout.Button($"P{i}", GUILayout.Width(30)))
                {
                    _selectedPointIndex = i;
                    SceneView.RepaintAll();
                }
                
                GUI.backgroundColor = Color.white;
                
                EditorGUI.BeginChangeCheck();
                float3 pos = _curve.GetPoint(i);
                Vector3 newPos = EditorGUILayout.Vector3Field("", pos);
                if (EditorGUI.EndChangeCheck())
                {
                    _curve.SetPoint(i, newPos);
                    SceneView.RepaintAll();
                }
                
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    _curve.RemovePoint(i);
                    _selectedPointIndex = -1;
                    SceneView.RepaintAll();
                    GUIUtility.ExitGUI();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("+ Add Point"))
            {
                float3 lastPos = _curve.PointCount > 0 
                    ? _curve.GetPoint(_curve.PointCount - 1) + new float3(1, 0, 0)
                    : float3.zero;
                _curve.AddPoint(lastPos);
                _selectedPointIndex = _curve.PointCount - 1;
                SceneView.RepaintAll();
            }
        }
        
        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            
            _showTangents = EditorGUILayout.Toggle("Show Tangents", _showTangents);
            _showFrames = EditorGUILayout.Toggle("Show Frames", _showFrames);
            _previewSamples = EditorGUILayout.IntSlider("Samples", _previewSamples, 10, 100);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            _animatePreview = GUILayout.Toggle(_animatePreview, "Animate", EditorStyles.miniButton);
            _animationSpeed = EditorGUILayout.Slider(_animationSpeed, 0.1f, 3f);
            EditorGUILayout.EndHorizontal();
            
            _previewTime = EditorGUILayout.Slider("Time", _previewTime, 0f, 1f);
            
            // Show current position info
            if (_curve.PointCount >= 2)
            {
                float3 pos = _curve.Evaluate(_previewTime);
                EditorGUILayout.LabelField($"Position: ({pos.x:F2}, {pos.y:F2}, {pos.z:F2})", 
                    EditorStyles.miniLabel);
            }
        }
        
        private void DrawPresetSection()
        {
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Circle"))
            {
                CreateCirclePreset();
            }
            if (GUILayout.Button("Spiral"))
            {
                CreateSpiralPreset();
            }
            if (GUILayout.Button("Figure 8"))
            {
                CreateFigure8Preset();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (_curve == null) return;
            
            // Draw the curve
            DrawCurveInScene();
            
            // Handle point editing
            HandlePointEditing();
            
            // Handle adding new points
            HandleAddingPoints();
        }
        
        private void DrawCurveInScene()
        {
            if (_curve.PointCount < 2) return;
            
            // Draw curve segments
            Handles.color = Color.cyan;
            float3 prevPos = _curve.Evaluate(0);
            for (int i = 1; i <= _previewSamples; i++)
            {
                float t = (float)i / _previewSamples;
                float3 pos = _curve.Evaluate(t);
                Handles.DrawLine((Vector3)prevPos, (Vector3)pos, 2f);
                prevPos = pos;
            }
            
            // Draw control points
            for (int i = 0; i < _curve.PointCount; i++)
            {
                float3 point = _curve.GetPoint(i);
                bool isSelected = i == _selectedPointIndex;
                
                Handles.color = isSelected ? Color.yellow : Color.white;
                float size = HandleUtility.GetHandleSize((Vector3)point) * 0.08f;
                
                if (Handles.Button((Vector3)point, Quaternion.identity, size, size * 1.2f, Handles.SphereHandleCap))
                {
                    _selectedPointIndex = i;
                    Repaint();
                }
                
                // Label
                Handles.Label((Vector3)point + Vector3.up * size * 2, $"P{i}");
            }
            
            // Draw preview position
            if (_curve.PointCount >= 2)
            {
                _curve.EvaluateFrame(_previewTime, out float3 pos, out float3 tangent, out float3 up);
                
                Handles.color = Color.green;
                float size = HandleUtility.GetHandleSize((Vector3)pos) * 0.1f;
                Handles.SphereHandleCap(0, (Vector3)pos, Quaternion.identity, size, EventType.Repaint);
                
                if (_showTangents)
                {
                    Handles.color = Color.blue;
                    Handles.DrawLine((Vector3)pos, (Vector3)(pos + tangent * 0.3f), 3f);
                }
                
                if (_showFrames)
                {
                    float3 right = math.cross(up, tangent);
                    Handles.color = Color.red;
                    Handles.DrawLine((Vector3)pos, (Vector3)(pos + right * 0.15f));
                    Handles.color = Color.green;
                    Handles.DrawLine((Vector3)pos, (Vector3)(pos + up * 0.15f));
                }
            }
        }
        
        private void HandlePointEditing()
        {
            if (_selectedPointIndex < 0 || _selectedPointIndex >= _curve.PointCount) return;
            
            float3 point = _curve.GetPoint(_selectedPointIndex);
            
            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.PositionHandle((Vector3)point, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Move Curve Point");
                _curve.SetPoint(_selectedPointIndex, newPoint);
            }
        }
        
        private void HandleAddingPoints()
        {
            Event e = Event.current;
            
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.A && e.control)
            {
                // Add point at mouse position
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    _curve.AddPoint(point);
                    _selectedPointIndex = _curve.PointCount - 1;
                    e.Use();
                    Repaint();
                }
            }
        }
        
        private void SaveCurveAsAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Curve Asset", "NewCurve", "asset", "Save spatial curve as asset");
            
            if (!string.IsNullOrEmpty(path))
            {
                var asset = CreateInstance<SpatialCurveAsset>();
                asset.Curve = _curve;
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                _asset = asset;
            }
        }
        
        private void SaveToAsset()
        {
            if (_asset != null)
            {
                _asset.Curve = _curve;
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();
            }
        }
        
        private void CreateCirclePreset()
        {
            _curve = SpatialCurve.Arc(float3.zero, 2f, 0f, 360f, new float3(0, 1, 0));
            _curve.SetClosed(true);
            _selectedPointIndex = -1;
            SceneView.RepaintAll();
        }
        
        private void CreateSpiralPreset()
        {
            _curve = SpatialCurve.Spiral(float3.zero, 1f, 2f, 3f, 3f);
            _selectedPointIndex = -1;
            SceneView.RepaintAll();
        }
        
        private void CreateFigure8Preset()
        {
            _curve = SpatialCurve.Figure8(float3.zero, 2f, 1f);
            _selectedPointIndex = -1;
            SceneView.RepaintAll();
        }
        
        private void CleanupPreview()
        {
            if (_previewObject != null)
            {
                DestroyImmediate(_previewObject);
            }
        }
        
        private enum Tool
        {
            Select,
            Add,
            Delete
        }
    }
    
    /// <summary>
    /// ScriptableObject to store SpatialCurve as an asset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCurve", menuName = "Catalyst/Spatial Curve")]
    public class SpatialCurveAsset : ScriptableObject
    {
        [SerializeField] private SpatialCurve _curve = new SpatialCurve();
        
        public SpatialCurve Curve
        {
            get => _curve;
            set => _curve = value;
        }
    }
}
#endif
