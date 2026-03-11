using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Input.AimAssist;

namespace Eraflo.Catalyst.Editor
{
    /// <summary>
    /// Custom editor for <see cref="AimAssistConfig"/>.
    /// Draws all serialized properties with the default inspector and adds a
    /// "Curve Preview" section showing AnimationCurve previews built from the LUT data.
    /// </summary>
    [CustomEditor(typeof(AimAssistConfig))]
    public class AimAssistConfigEditor : UnityEditor.Editor
    {
        private AnimationCurve _frictionCurve;
        private AnimationCurve _magnetismCurve;
        private string         _lastBakeError;

        private GUIStyle _headerStyle;

        // -------------------------------------------------------------------------
        // Unity callbacks
        // -------------------------------------------------------------------------

        private void OnEnable()
        {
            RebuildCurves();
        }

        public override void OnInspectorGUI()
        {
            // -- Default inspector -------------------------------------------------
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            if (changed || _frictionCurve == null || _magnetismCurve == null)
                RebuildCurves();

            // -- Curve Preview section ---------------------------------------------
            EditorGUILayout.Space(8);
            DrawSectionHeader("Curve Preview");

            // Bake & Preview button
            if (GUILayout.Button("Bake & Preview"))
                RebuildCurves();

            // Error output (placed immediately after the button)
            if (!string.IsNullOrEmpty(_lastBakeError))
            {
                EditorGUILayout.HelpBox(
                    "Could not preview — BakeCurves() failed: " + _lastBakeError,
                    MessageType.Warning);
            }

            if (_frictionCurve != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Friction LUT", EditorStyles.miniBoldLabel);

                var frictionRect = GUILayoutUtility.GetRect(1f, 60f);
                GUI.enabled = false;
                EditorGUI.CurveField(frictionRect, _frictionCurve);
                GUI.enabled = true;
            }

            if (_magnetismCurve != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Magnetism LUT", EditorStyles.miniBoldLabel);

                var magnetismRect = GUILayoutUtility.GetRect(1f, 60f);
                GUI.enabled = false;
                EditorGUI.CurveField(magnetismRect, _magnetismCurve);
                GUI.enabled = true;
            }
        }

        // -------------------------------------------------------------------------
        // Curve building
        // -------------------------------------------------------------------------

        private void RebuildCurves()
        {
            _lastBakeError  = null;
            _frictionCurve  = null;
            _magnetismCurve = null;

            var config = (AimAssistConfig)target;
            if (config == null) return;

            try
            {
                config.BakeCurves(out NativeArray<float> frictionLUT,
                                   out NativeArray<float> magnetismLUT,
                                   Allocator.Temp);

                _frictionCurve  = BuildAnimationCurve(frictionLUT);
                _magnetismCurve = BuildAnimationCurve(magnetismLUT);

                frictionLUT.Dispose();
                magnetismLUT.Dispose();
            }
            catch (System.Exception e)
            {
                _lastBakeError = e.Message;
            }
        }

        /// <summary>
        /// Converts a flat NativeArray LUT into an AnimationCurve by sampling every 8th index.
        /// The last index is always included to ensure the curve ends at t=1.
        /// </summary>
        private static AnimationCurve BuildAnimationCurve(NativeArray<float> lut)
        {
            var curve = new AnimationCurve();

            const int stride = 8;
            for (int i = 0; i < lut.Length - 1; i += stride)
            {
                float t = i / (float)(AimAssistConfig.LUTSize - 1);
                curve.AddKey(new Keyframe(t, lut[i]));
            }

            // Always include the final sample so the curve reaches t = 1.
            curve.AddKey(new Keyframe(1f, lut[lut.Length - 1]));

            return curve;
        }

        // -------------------------------------------------------------------------
        // UI helpers
        // -------------------------------------------------------------------------

        private void DrawSectionHeader(string title)
        {
            if (_headerStyle == null)
                _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(title, _headerStyle);

            Rect separatorRect = GUILayoutUtility.GetRect(1f, 1f);
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(3);
        }
    }
}
