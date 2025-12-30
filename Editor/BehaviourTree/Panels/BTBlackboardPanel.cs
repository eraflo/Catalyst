using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Eraflo.UnityImportPackage.BehaviourTree;
using BT = Eraflo.UnityImportPackage.BehaviourTree.BehaviourTree;

namespace Eraflo.UnityImportPackage.Editor.BehaviourTree.Panels
{
    /// <summary>
    /// Floating blackboard panel with editable values.
    /// </summary>
    public class BTBlackboardPanel : VisualElement
    {
        private const string PosXKey = "BT_Blackboard_PosX";
        private const string PosYKey = "BT_Blackboard_PosY";
        private const string WidthKey = "BT_Blackboard_Width";
        private const string HeightKey = "BT_Blackboard_Height";
        
        private BT _tree;
        private VisualElement _contentContainer;
        private Label _emptyLabel;
        
        private bool _isDragging;
        private Vector2 _dragStart;
        private Vector2 _posStart;

        private bool _isResizing;
        private Vector2 _resizeStartPos;
        private Vector2 _panelStartSize;
        
        public BTBlackboardPanel()
        {
            name = "blackboard-panel";
            AddToClassList("floating-panel");
            
            float x = EditorPrefs.GetFloat(PosXKey, 10);
            float y = EditorPrefs.GetFloat(PosYKey, 10);
            float w = EditorPrefs.GetFloat(WidthKey, 220);
            float h = EditorPrefs.GetFloat(HeightKey, 200);
            
            style.position = Position.Absolute;
            style.left = x;
            style.top = y;
            style.width = w;
            style.height = h;
            
            // Header
            var header = new VisualElement { name = "panel-header" };
            header.AddToClassList("panel-header");
            
            var title = new Label("Blackboard");
            title.AddToClassList("panel-title");
            header.Add(title);
            
            var addBtn = new Button(() => ShowAddMenu()) { text = "+" };
            addBtn.AddToClassList("panel-button");
            header.Add(addBtn);
            
            Add(header);
            
            header.RegisterCallback<MouseDownEvent>(OnHeaderMouseDown);
            header.RegisterCallback<MouseMoveEvent>(OnHeaderMouseMove);
            header.RegisterCallback<MouseUpEvent>(OnHeaderMouseUp);
            
            // Content
            _contentContainer = new ScrollView(ScrollViewMode.Vertical);
            _contentContainer.name = "panel-content";
            _contentContainer.AddToClassList("panel-content");
            _contentContainer.style.flexGrow = 1;
            Add(_contentContainer);
            
            _emptyLabel = new Label("(Empty)");
            _emptyLabel.AddToClassList("empty-label");
            _contentContainer.Add(_emptyLabel);

            // Resize handle
            var resizeHandle = new VisualElement { name = "resize-handle" };
            resizeHandle.AddToClassList("resize-handle");
            Add(resizeHandle);
            resizeHandle.RegisterCallback<MouseDownEvent>(OnResizeMouseDown);
            resizeHandle.RegisterCallback<MouseMoveEvent>(OnResizeMouseMove);
            resizeHandle.RegisterCallback<MouseUpEvent>(OnResizeMouseUp);
        }
        
        public void UpdateView(BT tree)
        {
            _tree = tree;
            RefreshKeys();
        }
        
        private void RefreshKeys()
        {
            _contentContainer.Clear();
            
            if (_tree?.Blackboard == null)
            {
                _contentContainer.Add(_emptyLabel);
                return;
            }
            
            var keys = _tree.Blackboard.GetAllKeys();
            if (keys.Length == 0)
            {
                _contentContainer.Add(_emptyLabel);
                return;
            }
            
            foreach (var key in keys)
            {
                var row = CreateEditableRow(key);
                _contentContainer.Add(row);
            }
        }
        
        private VisualElement CreateEditableRow(string key)
        {
            var row = new VisualElement();
            row.AddToClassList("blackboard-row");
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            
            // Key name (Editable)
            var keyField = new TextField { value = key };
            keyField.AddToClassList("key-field");
            keyField.style.width = 70;
            keyField.style.flexShrink = 0;
            keyField.RegisterValueChangedCallback(evt => {
                if (string.IsNullOrEmpty(evt.newValue) || _tree.Blackboard.Contains(evt.newValue))
                {
                    keyField.SetValueWithoutNotify(evt.previousValue);
                    return;
                }
                Undo.RecordObject(_tree, "Rename Blackboard Key");
                _tree.Blackboard.Rename(evt.previousValue, evt.newValue);
                EditorUtility.SetDirty(_tree);
                // No need to RefreshKeys here as we only changed one row's key identity
            });
            row.Add(keyField);
            
            // Value field - type-specific
            var valueField = CreateValueField(key);
            if (valueField != null)
            {
                valueField.style.flexGrow = 1;
                row.Add(valueField);
            }
            
            // Delete button
            var deleteBtn = new Button(() => DeleteKey(key)) { text = "×" };
            deleteBtn.AddToClassList("delete-button");
            row.Add(deleteBtn);
            
            return row;
        }
        
        private VisualElement CreateValueField(string key)
        {
            if (_tree.Blackboard.TryGet<int>(key, out int intVal))
            {
                var field = new IntegerField { label = "" };
                field.labelElement.style.display = DisplayStyle.None;
                field.value = intVal;
                field.RegisterValueChangedCallback(evt => {
                    _tree.Blackboard.Set(key, evt.newValue);
                    EditorUtility.SetDirty(_tree);
                });
                return field;
            }
            
            if (_tree.Blackboard.TryGet<float>(key, out float floatVal))
            {
                var field = new FloatField { label = "" };
                field.labelElement.style.display = DisplayStyle.None;
                field.value = floatVal;
                field.RegisterValueChangedCallback(evt => {
                    _tree.Blackboard.Set(key, evt.newValue);
                    EditorUtility.SetDirty(_tree);
                });
                return field;
            }
            
            if (_tree.Blackboard.TryGet<bool>(key, out bool boolVal))
            {
                var field = new Toggle();
                field.value = boolVal;
                field.RegisterValueChangedCallback(evt => {
                    _tree.Blackboard.Set(key, evt.newValue);
                    EditorUtility.SetDirty(_tree);
                });
                return field;
            }
            
            if (_tree.Blackboard.TryGet<string>(key, out string strVal))
            {
                var field = new TextField { label = "" };
                field.labelElement.style.display = DisplayStyle.None;
                field.value = strVal ?? "";
                field.RegisterValueChangedCallback(evt => {
                    _tree.Blackboard.Set(key, evt.newValue);
                    EditorUtility.SetDirty(_tree);
                });
                return field;
            }
            
            return new Label("?");
        }
        
        private void DeleteKey(string key)
        {
            _tree.Blackboard.Remove(key);
            EditorUtility.SetDirty(_tree);
            RefreshKeys();
        }
        
        private void ShowAddMenu()
        {
            if (_tree == null) return;
            
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Bool"), false, () => AddKey<bool>("newBool", false));
            menu.AddItem(new GUIContent("Int"), false, () => AddKey<int>("newInt", 0));
            menu.AddItem(new GUIContent("Float"), false, () => AddKey<float>("newFloat", 0f));
            menu.AddItem(new GUIContent("String"), false, () => AddKey<string>("newString", ""));
            menu.ShowAsContext();
        }
        
        private void AddKey<T>(string key, T value)
        {
            int c = 1;
            string k = key;
            while (_tree.Blackboard.Contains(k)) k = $"{key}{c++}";
            _tree.Blackboard.Set(k, value);
            EditorUtility.SetDirty(_tree);
            RefreshKeys();
        }
        
        private void OnHeaderMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                _isDragging = true;
                _dragStart = evt.mousePosition;
                _posStart = new Vector2(resolvedStyle.left, resolvedStyle.top);
                evt.target.CaptureMouse();
                evt.StopPropagation();
            }
        }
        
        private void OnHeaderMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging)
            {
                Vector2 delta = evt.mousePosition - _dragStart;
                style.left = _posStart.x + delta.x;
                style.top = _posStart.y + delta.y;
            }
        }
        
        private void OnHeaderMouseUp(MouseUpEvent evt)
        {
            if (_isDragging)
            {
                _isDragging = false;
                evt.target.ReleaseMouse();
                
                EditorPrefs.SetFloat(PosXKey, resolvedStyle.left);
                EditorPrefs.SetFloat(PosYKey, resolvedStyle.top);
            }
        }

        private void OnResizeMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                _isResizing = true;
                _resizeStartPos = evt.mousePosition;
                _panelStartSize = new Vector2(resolvedStyle.width, resolvedStyle.height);
                evt.target.CaptureMouse();
                evt.StopPropagation();
            }
        }

        private void OnResizeMouseMove(MouseMoveEvent evt)
        {
            if (_isResizing)
            {
                Vector2 delta = evt.mousePosition - _resizeStartPos;
                style.width = Mathf.Max(150, _panelStartSize.x + delta.x);
                style.height = Mathf.Max(100, _panelStartSize.y + delta.y);
            }
        }

        private void OnResizeMouseUp(MouseUpEvent evt)
        {
            if (_isResizing)
            {
                _isResizing = false;
                evt.target.ReleaseMouse();
                EditorPrefs.SetFloat(WidthKey, resolvedStyle.width);
                EditorPrefs.SetFloat(HeightKey, resolvedStyle.height);
            }
        }
    }
}
