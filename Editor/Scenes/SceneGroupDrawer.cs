using Eraflo.Catalyst;
using UnityEditor;
using UnityEngine;

namespace Eraflo.Catalyst.Editor
{
    /// <summary>
    /// Custom property drawer for <see cref="SceneGroup"/>.
    /// Replaces raw string fields with SceneAsset drag-and-drop pickers and validates
    /// that ActiveScene is present in the Scenes list.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneGroup))]
    public class SceneGroupDrawer : PropertyDrawer
    {
        private const float HelpBoxHeight    = 36f;
        private const float RemoveButtonWidth = 22f;
        private const float IndentOffset      = 14f;

        // -------------------------------------------------------------------------
        // Height calculation
        // -------------------------------------------------------------------------

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineH   = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            var scenesProp = property.FindPropertyRelative("Scenes");
            int  sceneCount = scenesProp != null ? scenesProp.arraySize : 0;

            // Name field (1) + "Scenes" label (1) + each scene row + Add button (1) + ActiveScene (1)
            float height = (lineH + spacing) * (4 + sceneCount);

            // Optional HelpBox when ActiveScene is not empty and not in the Scenes list
            var activeSceneProp = property.FindPropertyRelative("ActiveScene");
            if (activeSceneProp != null && !string.IsNullOrEmpty(activeSceneProp.stringValue))
            {
                if (!IsActiveSceneInList(activeSceneProp.stringValue, scenesProp))
                    height += HelpBoxHeight + spacing;
            }

            return height;
        }

        // -------------------------------------------------------------------------
        // Drawing
        // -------------------------------------------------------------------------

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float lineH   = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float x       = position.x;
            float w       = position.width;
            float y       = position.y;

            var nameProp       = property.FindPropertyRelative("Name");
            var scenesProp     = property.FindPropertyRelative("Scenes");
            var activeSceneProp = property.FindPropertyRelative("ActiveScene");

            // -- Name ----------------------------------------------------------------
            EditorGUI.PropertyField(new Rect(x, y, w, lineH), nameProp, new GUIContent("Name"));
            y += lineH + spacing;

            // -- Scenes list label ---------------------------------------------------
            EditorGUI.LabelField(new Rect(x, y, w, lineH), "Scenes", EditorStyles.boldLabel);
            y += lineH + spacing;

            // -- Scene entries -------------------------------------------------------
            for (int i = 0; i < scenesProp.arraySize; i++)
            {
                var  elementProp = scenesProp.GetArrayElementAtIndex(i);
                var  fieldRect   = new Rect(x + IndentOffset, y, w - IndentOffset - RemoveButtonWidth - 4f, lineH);
                var  removeRect  = new Rect(x + w - RemoveButtonWidth, y, RemoveButtonWidth, lineH);

                SceneAsset current = FindSceneAsset(elementProp.stringValue);

                EditorGUI.BeginChangeCheck();
                var newAsset = (SceneAsset)EditorGUI.ObjectField(
                    fieldRect, GUIContent.none, current, typeof(SceneAsset), false);
                if (EditorGUI.EndChangeCheck())
                    elementProp.stringValue = newAsset != null ? newAsset.name : string.Empty;

                if (GUI.Button(removeRect, "-"))
                {
                    scenesProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                y += lineH + spacing;
            }

            // -- Add button ----------------------------------------------------------
            if (GUI.Button(new Rect(x + IndentOffset, y, w - IndentOffset, lineH), "Add Scene"))
            {
                scenesProp.arraySize++;
                scenesProp.GetArrayElementAtIndex(scenesProp.arraySize - 1).stringValue = string.Empty;
            }
            y += lineH + spacing;

            // -- ActiveScene --------------------------------------------------------
            string     activeSceneName = activeSceneProp.stringValue;
            SceneAsset activeAsset     = FindSceneAsset(activeSceneName);
            bool       isInvalid       = !string.IsNullOrEmpty(activeSceneName)
                                         && !IsActiveSceneInList(activeSceneName, scenesProp);

            Color prevColor = GUI.color;
            if (isInvalid)
                GUI.color = Color.red;

            EditorGUI.BeginChangeCheck();
            var newActiveAsset = (SceneAsset)EditorGUI.ObjectField(
                new Rect(x, y, w, lineH),
                new GUIContent("Active Scene"),
                activeAsset,
                typeof(SceneAsset),
                false);
            if (EditorGUI.EndChangeCheck())
                activeSceneProp.stringValue = newActiveAsset != null ? newActiveAsset.name : string.Empty;

            GUI.color = prevColor;
            y += lineH + spacing;

            // -- Validation help box ------------------------------------------------
            if (isInvalid)
            {
                EditorGUI.HelpBox(
                    new Rect(x, y, w, HelpBoxHeight),
                    "ActiveScene must be in the Scenes list.",
                    MessageType.Warning);
            }

            EditorGUI.EndProperty();
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static bool IsActiveSceneInList(string sceneName, SerializedProperty scenesProp)
        {
            if (scenesProp == null) return false;
            for (int i = 0; i < scenesProp.arraySize; i++)
            {
                if (scenesProp.GetArrayElementAtIndex(i).stringValue == sceneName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Resolves a scene name string to a <see cref="SceneAsset"/> via AssetDatabase search.
        /// Returns null when the name is empty or no matching asset is found.
        /// </summary>
        private static SceneAsset FindSceneAsset(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return null;

            string[] guids = AssetDatabase.FindAssets("t:SceneAsset " + sceneName);
            foreach (string guid in guids)
            {
                string     path  = AssetDatabase.GUIDToAssetPath(guid);
                SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (asset != null && asset.name == sceneName)
                    return asset;
            }
            return null;
        }
    }
}
