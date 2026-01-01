using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Eraflo.Catalyst.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    public class BTStickyNoteElement : VisualElement
    {
        public StickyNote Note { get; private set; }
        public System.Action<BTStickyNoteElement> OnSelected;
        public System.Action<BTStickyNoteElement> OnPositionChanged;
        
        private Label _titleLabel;
        private TextField _contentField;
        private VisualElement _header;
        
        public bool IsSelected { get; private set; }

        public BTStickyNoteElement(StickyNote note)
        {
            Note = note;
            name = "sticky-note";
            AddToClassList("sticky-note");
            
            // Layout from serialized data
            style.position = Position.Absolute;
            style.left = note.Position.x;
            style.top = note.Position.y;
            style.width = note.Position.width;
            style.height = note.Position.height;
            style.backgroundColor = note.Color;
            
            // Header (Draggable area)
            _header = new VisualElement { name = "sticky-note-header" };
            _header.AddToClassList("sticky-note-header");
            Add(_header);
            
            // Title
            _titleLabel = new Label(note.Title);
            _titleLabel.AddToClassList("sticky-note-title");
            _header.Add(_titleLabel);
            
            // Content (editable)
            _contentField = new TextField();
            _contentField.value = note.Content;
            _contentField.multiline = true;
            _contentField.AddToClassList("sticky-note-content");
            _contentField.RegisterValueChangedCallback(evt => {
                Note.Content = evt.newValue;
                EditorUtility.SetDirty(Note);
            });
            Add(_contentField);
            
            // Capabilities
            this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));
            
            // Drag logic on Header ONLY
            _header.RegisterCallback<MouseDownEvent>(OnMouseDown);
            _header.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            _header.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        private bool _isDragging;
        private Vector2 _dragStart;
        private Vector2 _elementStart;

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                OnSelected?.Invoke(this);
                
                _isDragging = true;
                _dragStart = evt.localMousePosition;
                _elementStart = new Vector2(style.left.value.value, style.top.value.value);
                
                // Capture mouse on the HEADER to ensure we get Up/Move events
                _header.CaptureMouse();
                evt.StopPropagation();
            }
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_isDragging)
            {
                Vector2 delta = evt.localMousePosition - _dragStart;
                Vector2 newPos = _elementStart + delta; // Logic handles local delta correctly because we capture mouse
                
                // Since we are capturing mouse, localMousePosition is relative to us.
                // It's simpler to use visual element layout or world space if needed, 
                // but for simple drag:
                
                style.left = style.left.value.value + delta.x;
                style.top = style.top.value.value + delta.y;
                
                Note.Position.x = style.left.value.value;
                Note.Position.y = style.top.value.value;
            }
        }
        
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _header.ReleaseMouse();
                
                // Snap to grid (20px)
                float gridSize = 20f;
                float x = Mathf.Round(Note.Position.x / gridSize) * gridSize;
                float y = Mathf.Round(Note.Position.y / gridSize) * gridSize;
                
                Note.Position.x = x;
                Note.Position.y = y;
                style.left = x;
                style.top = y;
                
                EditorUtility.SetDirty(Note);
                evt.StopPropagation();
            }
        }
        
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            if (selected) AddToClassList("selected");
            else RemoveFromClassList("selected");
        }
        
        private void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Yellow", a => SetColor(new Color(1f, 0.9f, 0.4f)));
            evt.menu.AppendAction("Blue", a => SetColor(new Color(0.6f, 0.8f, 1f)));
            evt.menu.AppendAction("Green", a => SetColor(new Color(0.6f, 1f, 0.6f)));
            evt.menu.AppendAction("Pink", a => SetColor(new Color(1f, 0.7f, 0.8f))); 
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", a => DeleteNote());
        }
        
        private void SetColor(Color c)
        {
            Note.Color = c;
            style.backgroundColor = c;
            EditorUtility.SetDirty(Note);
        }
        
        public System.Action<BTStickyNoteElement> OnDelete;
        
        private void DeleteNote()
        {
            OnDelete?.Invoke(this);
        }
    }
}
