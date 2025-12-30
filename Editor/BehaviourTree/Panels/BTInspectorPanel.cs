using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Eraflo.UnityImportPackage.BehaviourTree;
using BT = Eraflo.UnityImportPackage.BehaviourTree.BehaviourTree;

namespace Eraflo.UnityImportPackage.Editor.BehaviourTree.Panels
{
    /// <summary>
    /// Floating inspector panel for selected node using pure UIElements.
    /// </summary>
    public class BTInspectorPanel : VisualElement
    {
        private const string PosXKey = "BT_Inspector_PosX";
        private const string PosYKey = "BT_Inspector_PosY";
        private const string WidthKey = "BT_Inspector_Width";
        private const string HeightKey = "BT_Inspector_Height";
        
        private Node _node;
        private SerializedObject _serializedObject;
        private VisualElement _contentContainer;
        private Label _placeholder;
        private VisualElement _header;
        
        private bool _isDragging;
        private Vector2 _dragStart;
        private Vector2 _posStart;

        private bool _isResizing;
        private Vector2 _resizeStartPos;
        private Vector2 _panelStartSize;
        
        public BTInspectorPanel()
        {
            name = "inspector-panel";
            AddToClassList("floating-panel");
            
            float x = EditorPrefs.GetFloat(PosXKey, 10);
            float y = EditorPrefs.GetFloat(PosYKey, 250);
            float w = EditorPrefs.GetFloat(WidthKey, 240);
            float h = EditorPrefs.GetFloat(HeightKey, 300);
            
            style.position = Position.Absolute;
            style.left = x;
            style.top = y;
            style.width = w;
            style.height = h;
            pickingMode = PickingMode.Position;
            
            // Header
            _header = new VisualElement { name = "panel-header" };
            _header.AddToClassList("panel-header");
            _header.pickingMode = PickingMode.Position;
            
            var title = new Label("Inspector");
            title.AddToClassList("panel-title");
            _header.Add(title);
            
            Add(_header);
            
            _header.RegisterCallback<MouseDownEvent>(OnHeaderMouseDown);
            _header.RegisterCallback<MouseMoveEvent>(OnHeaderMouseMove);
            _header.RegisterCallback<MouseUpEvent>(OnHeaderMouseUp);
            
            // Content (Directly use ScrollView like Blackboard)
            _contentContainer = new ScrollView(ScrollViewMode.Vertical);
            _contentContainer.name = "panel-content";
            _contentContainer.AddToClassList("panel-content");
            _contentContainer.style.flexGrow = 1;
            Add(_contentContainer);
            
            _placeholder = new Label("Select a node");
            _placeholder.AddToClassList("empty-label");
            _contentContainer.Add(_placeholder);

            // Resize handle
            var resizeHandle = new VisualElement { name = "resize-handle" };
            resizeHandle.AddToClassList("resize-handle");
            resizeHandle.pickingMode = PickingMode.Position;
            Add(resizeHandle);
            resizeHandle.RegisterCallback<MouseDownEvent>(OnResizeMouseDown);
            resizeHandle.RegisterCallback<MouseMoveEvent>(OnResizeMouseMove);
            resizeHandle.RegisterCallback<MouseUpEvent>(OnResizeMouseUp);
            
            // IMPORTANT: Stop ALL mouse events from reaching the canvas underneath
            RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            RegisterCallback<MouseUpEvent>(evt => evt.StopPropagation());
            RegisterCallback<MouseMoveEvent>(evt => evt.StopPropagation());
            RegisterCallback<WheelEvent>(evt => evt.StopPropagation());
        }
        
        public void UpdateSelection(Node node)
        {
            _node = node;
            _contentContainer.Clear();
            _serializedObject = null;
            
            if (node == null)
            {
                _contentContainer.Add(_placeholder);
                return;
            }
            
            _serializedObject = new SerializedObject(node);
            
            // Padding container for the fields
            var fieldContainer = new VisualElement();
            fieldContainer.style.paddingTop = 5;
            fieldContainer.style.paddingLeft = 5;
            fieldContainer.style.paddingRight = 5;
            fieldContainer.style.paddingBottom = 10;
            _contentContainer.Add(fieldContainer);
            
            // Use a specific class to style labels if needed, or set labelWidth
            fieldContainer.RegisterCallback<AttachToPanelEvent>(evt => {
                // Ensure label width is reasonable
                fieldContainer.Query<Label>().ForEach(l => l.style.minWidth = 60);
            });
            
            // Space
            fieldContainer.Add(new VisualElement { style = { height = 4 } });

            // Iterate visible properties
            SerializedProperty prop = _serializedObject.GetIterator();
            bool enterChildren = true;
            
            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                
                string n = prop.name;
                // Skip internal fields (including m_Name as requested)
                if (n == "m_Script" || n == "State" || n == "Started" || 
                    n == "Guid" || n == "Position" || n == "Children" || n == "Child" || n == "m_Name")
                    continue;
                
                var field = new PropertyField(prop);
                field.Bind(_serializedObject);
                field.name = n; // Set name so we can find it via container.Q
                fieldContainer.Add(field);
            }
            
            // Perform initial visibility update for nodes with a "Type" enum
            var typeProp = _serializedObject.FindProperty("Type");
            if (typeProp != null && typeProp.propertyType == SerializedPropertyType.Enum)
            {
                var typeField = fieldContainer.Q<PropertyField>("Type");
                if (typeField != null)
                {
                    typeField.RegisterValueChangeCallback(evt => UpdateBlackboardVisibility(fieldContainer, evt.changedProperty));
                    UpdateBlackboardVisibility(fieldContainer, typeProp);
                }
            }
            
            // Add spacer at bottom
            fieldContainer.Add(new VisualElement { style = { height = 10 } });
        }
        
        public void ClearSelection()
        {
            UpdateSelection(null);
        }

        private void UpdateBlackboardVisibility(VisualElement container, SerializedProperty typeProp)
        {
            if (typeProp == null) return;
            
            // Identify the target value field based on the enum name (e.g., "Bool" -> "BoolValue")
            string typeName = typeProp.enumNames[typeProp.enumValueIndex];
            string targetField = typeName + "Value";
            
            // Known value fields to manage
            string[] valueFields = { "BoolValue", "IntValue", "FloatValue", "StringValue", "Vector3Value" };

            foreach (var fieldName in valueFields)
            {
                var field = container.Q<PropertyField>(fieldName);
                if (field != null)
                {
                    field.style.display = (fieldName == targetField) ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }
        
        private void OnHeaderMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                _isDragging = true;
                _dragStart = evt.mousePosition;
                _posStart = new Vector2(resolvedStyle.left, resolvedStyle.top);
                _header.CaptureMouse();
                evt.StopPropagation();
            }
        }
        
        private void OnHeaderMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging)
            {
                // Safety: if mouse is not pressed, something went wrong (e.g. mouse up outside window)
                if (evt.pressedButtons == 0)
                {
                    _isDragging = false;
                    _header.ReleaseMouse();
                    return;
                }

                Vector2 delta = evt.mousePosition - _dragStart;
                style.left = _posStart.x + delta.x;
                style.top = _posStart.y + delta.y;
                evt.StopPropagation();
            }
        }
        
        private void OnHeaderMouseUp(MouseUpEvent evt)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _header.ReleaseMouse();
                
                EditorPrefs.SetFloat(PosXKey, resolvedStyle.left);
                EditorPrefs.SetFloat(PosYKey, resolvedStyle.top);
                evt.StopPropagation();
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
                if (evt.pressedButtons == 0)
                {
                    _isResizing = false;
                    evt.target.ReleaseMouse();
                    return;
                }

                Vector2 delta = evt.mousePosition - _resizeStartPos;
                style.width = Mathf.Max(150, _panelStartSize.x + delta.x);
                style.height = Mathf.Max(100, _panelStartSize.y + delta.y);
                evt.StopPropagation();
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
                evt.StopPropagation();
            }
        }
    }
}
