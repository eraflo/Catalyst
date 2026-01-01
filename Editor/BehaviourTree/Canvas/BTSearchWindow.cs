using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Callbacks;
using Eraflo.Catalyst.BehaviourTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Eraflo.Catalyst.Editor.BehaviourTree.Canvas
{
    /// <summary>
    /// Custom search window for node creation.
    /// Matches the editor design style.
    /// </summary>
    public class BTSearchWindow : VisualElement
    {
        public Action<Type, Vector2> OnNodeSelected;
        
        private Vector2 _createPosition;
        private TextField _searchField;
        private ScrollView _resultsScrollView;
        private List<NodeTypeInfo> _filteredTypes;
        private Type _baseTypeFilter = typeof(Node);
        
        // OPTIMIZATION: Static cache shared across all instances
        private static List<NodeTypeInfo> _allNodeTypes;
        private static bool _cacheBuilt = false;
        
        private struct NodeTypeInfo
        {
            public Type Type;
            public string DisplayName;
            public string Category;
        }
        
        /// <summary>
        /// Invalidates the static node type cache when scripts are reloaded.
        /// </summary>
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            _cacheBuilt = false;
            _allNodeTypes = null;
        }
        
        public BTSearchWindow()
        {
            name = "bt-search-window";
            AddToClassList("search-window");
            
            style.position = Position.Absolute;
            style.width = 240; // Slightly wider
            style.display = DisplayStyle.None;
            
            // Header
            var header = new VisualElement();
            header.AddToClassList("search-header");
            
            _searchField = new TextField();
            _searchField.AddToClassList("search-field");
            _searchField.RegisterValueChangedCallback(OnSearchChanged);
            _searchField.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
            header.Add(_searchField);
            
            Add(header);
            
            // Results ScrollView
            _resultsScrollView = new ScrollView(ScrollViewMode.Vertical);
            _resultsScrollView.AddToClassList("search-results");
            _resultsScrollView.style.flexGrow = 1;
            Add(_resultsScrollView);
            
            // Build node type cache
            BuildNodeTypeCache();
            
            // Close on click outside - handle focus loss carefully
            this.RegisterCallback<FocusOutEvent>(evt => {
                // Delay hide to allow click events on results to fire first
                if (evt.relatedTarget == null || !Contains((VisualElement)evt.relatedTarget))
                {
                    schedule.Execute(() => Hide()).StartingIn(100);
                }
            });
        }
        
        private void BuildNodeTypeCache()
        {
            // OPTIMIZATION: Skip if cache is already built
            if (_cacheBuilt && _allNodeTypes != null)
            {
                _filteredTypes = new List<NodeTypeInfo>(_allNodeTypes);
                return;
            }
            
            _allNodeTypes = new List<NodeTypeInfo>();
            
            var nodeBaseType = typeof(Node);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && nodeBaseType.IsAssignableFrom(t));
                    
                    foreach (var type in types)
                    {
                        string category = GetCategory(type);
                        string displayName = GetDisplayName(type);
                        
                        _allNodeTypes.Add(new NodeTypeInfo
                        {
                            Type = type,
                            DisplayName = displayName,
                            Category = category
                        });
                    }
                }
                catch { }
            }
            
            _allNodeTypes = _allNodeTypes.OrderBy(n => n.Category).ThenBy(n => n.DisplayName).ToList();
            _filteredTypes = new List<NodeTypeInfo>(_allNodeTypes);
            _cacheBuilt = true;
        }
        
        private string GetCategory(Type type)
        {
            if (typeof(CompositeNode).IsAssignableFrom(type)) return "Composite";
            if (typeof(DecoratorNode).IsAssignableFrom(type)) return "Decorator";
            if (typeof(ServiceNode).IsAssignableFrom(type)) return "Service";
            if (typeof(ActionNode).IsAssignableFrom(type)) return "Action";
            if (typeof(ConditionNode).IsAssignableFrom(type)) return "Condition";
            return "Other";
        }
        
        private string GetDisplayName(Type type)
        {
            // Check for attribute
            var attr = type.GetCustomAttribute<BehaviourTreeNodeAttribute>();
            if (attr != null && !string.IsNullOrEmpty(attr.DisplayName))
                return attr.DisplayName;
            
            return type.Name;
        }
        
        public void Show(Vector2 position, Vector2 createPosition, Type baseTypeFilter = null)
        {
            _createPosition = createPosition;
            _baseTypeFilter = baseTypeFilter ?? typeof(Node);
            
            style.left = position.x;
            style.top = position.y;
            style.display = DisplayStyle.Flex;
            
            _searchField.value = "";
            
            // If filter is null or generic Node, exclude services from regular menu
            bool excludeServices = _baseTypeFilter == null || _baseTypeFilter == typeof(Node);
            
            _filteredTypes = _allNodeTypes
                .Where(n => _baseTypeFilter.IsAssignableFrom(n.Type) && 
                           (!excludeServices || !typeof(ServiceNode).IsAssignableFrom(n.Type)))
                .ToList();
            RefreshResults();
            
            _searchField.Focus();
        }
        
        public void Hide()
        {
            style.display = DisplayStyle.None;
        }
        
        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            string search = evt.newValue.ToLower();
            bool excludeServices = _baseTypeFilter == null || _baseTypeFilter == typeof(Node);
            
            if (string.IsNullOrEmpty(search))
            {
                _filteredTypes = _allNodeTypes
                    .Where(n => _baseTypeFilter.IsAssignableFrom(n.Type) && 
                               (!excludeServices || !typeof(ServiceNode).IsAssignableFrom(n.Type)))
                    .ToList();
            }
            else
            {
                _filteredTypes = _allNodeTypes
                    .Where(n => _baseTypeFilter.IsAssignableFrom(n.Type) && 
                               (!excludeServices || !typeof(ServiceNode).IsAssignableFrom(n.Type)) &&
                               (n.DisplayName.ToLower().Contains(search) || 
                                n.Category.ToLower().Contains(search)))
                    .ToList();
            }
            
            RefreshResults();
        }
        
        private void OnSearchKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                Hide();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Return && _filteredTypes.Count > 0)
            {
                SelectNode(_filteredTypes[0].Type);
                evt.StopPropagation();
            }
        }
        
        private void RefreshResults()
        {
            _resultsScrollView.Clear();
            
            string currentCategory = "";
            
            foreach (var nodeInfo in _filteredTypes)
            {
                // Category header
                if (nodeInfo.Category != currentCategory)
                {
                    currentCategory = nodeInfo.Category;
                    var categoryLabel = new Label(currentCategory);
                    categoryLabel.AddToClassList("category-label");
                    _resultsScrollView.Add(categoryLabel);
                }
                
                // Node button
                var button = new Button(() => {
                    SelectNode(nodeInfo.Type);
                });
                button.text = nodeInfo.DisplayName;
                button.AddToClassList("node-button");
                button.AddToClassList(nodeInfo.Category.ToLower());
                
                // Important for preventing focus loss issues
                button.pickingMode = PickingMode.Position;
                
                _resultsScrollView.Add(button);
            }
            
            if (_filteredTypes.Count == 0)
            {
                var emptyLabel = new Label("No results");
                emptyLabel.AddToClassList("empty-label");
                _resultsScrollView.Add(emptyLabel);
            }
        }
        
        private void SelectNode(Type type)
        {
            OnNodeSelected?.Invoke(type, _createPosition);
            Hide();
        }
    }
}
