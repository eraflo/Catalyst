using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Eraflo.Catalyst.BehaviourTree;
using Eraflo.Catalyst.Editor.BehaviourTree.Canvas;
using Eraflo.Catalyst.Editor.BehaviourTree.Panels;
using BT = Eraflo.Catalyst.BehaviourTree.BehaviourTree;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Window
{
    /// <summary>
    /// Main editor window for the Behaviour Tree.
    /// Uses custom canvas instead of GraphView.
    /// </summary>
    public class BehaviourTreeEditorWindow : EditorWindow
    {
        private BTCanvas _canvas;
        private BTBlackboardPanel _blackboardPanel;
        private BTInspectorPanel _inspectorPanel;
        private BTSearchWindow _searchWindow;
        
        [SerializeField] private BT _tree;
        private Label _treeNameLabel;
        
        [MenuItem("Tools/Eraflo Catalyst/Behaviour Tree Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<BehaviourTreeEditorWindow>();
            window.titleContent = new GUIContent("Behaviour Tree");
            window.minSize = new Vector2(800, 600);
        }
        
        public static void OpenWindow(BT tree)
        {
            var window = GetWindow<BehaviourTreeEditorWindow>();
            window.titleContent = new GUIContent("Behaviour Tree");
            window.SelectTree(tree);
        }
        
        private void CreateGUI()
        {
            var root = rootVisualElement;
            
            // Load stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.Eraflo.Catalyst/Editor/BehaviourTree/Styles/BehaviourTree.uss"
            );
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            
            root.style.flexDirection = FlexDirection.Column;
            
            // Toolbar
            var toolbar = CreateToolbar();
            root.Add(toolbar);
            
            // Canvas container
            var canvasContainer = new VisualElement();
            canvasContainer.style.flexGrow = 1;
            canvasContainer.style.overflow = Overflow.Hidden;
            root.Add(canvasContainer);
            
            // Canvas
            _canvas = new BTCanvas();
            _canvas.OnNodeSelected += OnNodeSelected;
            _canvas.OnSelectionCleared += () => _inspectorPanel?.ClearSelection();
            _canvas.OnShowSearchWindow += (localPos, canvasPos, baseType) => {
                var worldPos = _canvas.LocalToWorld(localPos);
                var rootPos = root.WorldToLocal(worldPos);
                _searchWindow?.Show(rootPos, canvasPos, baseType);
            };
            canvasContainer.Add(_canvas);
            
            // Floating panels
            _blackboardPanel = new BTBlackboardPanel();
            canvasContainer.Add(_blackboardPanel);
            
            _inspectorPanel = new BTInspectorPanel();
            _inspectorPanel.OnServiceRemoved += (node) => _canvas.UpdateBadgeForNode(node);
            canvasContainer.Add(_inspectorPanel);
            
            // Search window (last = on top)
            _searchWindow = new BTSearchWindow();
            _searchWindow.OnNodeSelected += (type, pos) => {
                // Services are handled by BTCanvas.AddService, not CreateNode
                if (typeof(ServiceNode).IsAssignableFrom(type))
                {
                    _canvas.HandleServiceSelection(type);
                }
                else
                {
                    _canvas.CreateNode(type, pos);
                }
            };
            root.Add(_searchWindow);
            
            // Load current selection or restore from previous
            if (_tree != null)
            {
                SelectTree(_tree);
            }
            else
            {
                OnSelectionChange();
            }
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                _canvas?.UpdateDebugStates();
                _blackboardPanel?.UpdateView(_tree);
            }
        }
        
        private VisualElement CreateToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("bt-toolbar");
            
            // Tree selector
            var selectBtn = new Button(() => ShowTreeMenu()) { text = "Select Tree ▼" };
            selectBtn.AddToClassList("toolbar-button");
            toolbar.Add(selectBtn);
            
            // Tree name
            _treeNameLabel = new Label("No tree selected");
            _treeNameLabel.AddToClassList("toolbar-label");
            toolbar.Add(_treeNameLabel);
            
            // Spacer
            var spacer = new VisualElement();
            spacer.AddToClassList("toolbar-spacer");
            toolbar.Add(spacer);
            
            // Panel toggle buttons
            var blackboardToggle = new Button(() => ToggleBlackboardPanel()) { text = "📋" };
            blackboardToggle.AddToClassList("toolbar-button");
            blackboardToggle.AddToClassList("toolbar-toggle");
            blackboardToggle.tooltip = "Toggle Blackboard Panel";
            toolbar.Add(blackboardToggle);
            
            var inspectorToggle = new Button(() => ToggleInspectorPanel()) { text = "🔍" };
            inspectorToggle.AddToClassList("toolbar-button");
            inspectorToggle.AddToClassList("toolbar-toggle");
            inspectorToggle.tooltip = "Toggle Inspector Panel";
            toolbar.Add(inspectorToggle);
            
            // Separator
            var sep = new VisualElement();
            sep.AddToClassList("toolbar-separator");
            toolbar.Add(sep);
            
            // New tree
            var newBtn = new Button(() => CreateNewTree()) { text = "New Tree" };
            newBtn.AddToClassList("toolbar-button");
            toolbar.Add(newBtn);
            
            // Save
            var saveBtn = new Button(() => SaveTree()) { text = "Save" };
            saveBtn.AddToClassList("toolbar-button");
            toolbar.Add(saveBtn);
            
            return toolbar;
        }
        
        private void ToggleBlackboardPanel()
        {
            if (_blackboardPanel == null) return;
            bool visible = _blackboardPanel.style.display == DisplayStyle.Flex;
            _blackboardPanel.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            EditorPrefs.SetBool("BT_Blackboard_Visible", !visible);
        }
        
        private void ToggleInspectorPanel()
        {
            if (_inspectorPanel == null) return;
            bool visible = _inspectorPanel.style.display == DisplayStyle.Flex;
            _inspectorPanel.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            EditorPrefs.SetBool("BT_Inspector_Visible", !visible);
        }
        
        private void ShowTreeMenu()
        {
            var menu = new GenericMenu();
            
            var guids = AssetDatabase.FindAssets("t:BehaviourTree");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<BT>(path);
                if (tree != null)
                {
                    menu.AddItem(new GUIContent(tree.name), false, () => SelectTree(tree));
                }
            }
            
            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("(No trees found)"));
            }
            
            menu.ShowAsContext();
        }
        
        public void SelectTree(BT tree)
        {
            _tree = tree;
            _treeNameLabel.text = tree != null ? tree.name : "No tree selected";
            
            _canvas?.LoadTree(tree);
            _blackboardPanel?.UpdateView(tree);
            _inspectorPanel?.ClearSelection();
        }
        
        private void OnSelectionChange()
        {
            var tree = Selection.activeObject as BT;
            if (tree != null && tree != _tree)
            {
                SelectTree(tree);
                return;
            }

            var runner = Selection.activeGameObject?.GetComponent<BehaviourTreeRunner>();
            if (runner != null && runner.RuntimeTree != null && runner.RuntimeTree != _tree)
            {
                SelectTree(runner.RuntimeTree);
            }
        }
        
        private void OnNodeSelected(BTNodeElement element)
        {
            _inspectorPanel?.UpdateSelection(element?.Node);
        }
        
        private void CreateNewTree()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Behaviour Tree", "NewBehaviourTree", "asset",
                "Choose a location for the new Behaviour Tree"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                var tree = ScriptableObject.CreateInstance<BT>();
                AssetDatabase.CreateAsset(tree, path);
                AssetDatabase.SaveAssets();
                SelectTree(tree);
            }
        }
        
        private void SaveTree()
        {
            if (_tree != null)
            {
                EditorUtility.SetDirty(_tree);
                AssetDatabase.SaveAssets();
            }
        }
        
        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && _tree != null)
            {
                _blackboardPanel?.UpdateView(_tree);
                _canvas?.UpdateDebugStates();
                Repaint(); // Force window repaint for live visual updates
            }
        }
    }
}
