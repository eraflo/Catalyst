using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Eraflo.Catalyst.Noise;

namespace Eraflo.Catalyst.Editor
{
    /// <summary>
    /// Custom property drawer for <see cref="NoiseField"/>.
    /// Draws all serialized fields normally and shows a 128x32 grayscale preview strip
    /// of the noise at the current parameters, sampled via <see cref="FractalNoise.Sample2D"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(NoiseField))]
    public class NoiseFieldDrawer : PropertyDrawer
    {
        private const int   PreviewTexWidth  = 128;
        private const int   PreviewTexHeight = 32;
        private const float PreviewRectH     = 34f;
        private const float SectionPadding   = 4f;

        // Cached textures keyed by property path to avoid regeneration every frame.
        private static readonly Dictionary<string, Texture2D> PreviewCache =
            new Dictionary<string, Texture2D>();

        // Serialized field names in display order.
        private static readonly string[] FieldNames =
        {
            "Frequency", "TimeScale", "Amplitude", "Octaves", "Offset", "Time"
        };

        // -------------------------------------------------------------------------
        // Height calculation
        // -------------------------------------------------------------------------

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float lineH   = EditorGUIUtility.singleLineHeight;

            // Sum heights of all serialized fields.
            float height = 0f;
            foreach (string fieldName in FieldNames)
            {
                var fieldProp = property.FindPropertyRelative(fieldName);
                if (fieldProp != null)
                    height += EditorGUI.GetPropertyHeight(fieldProp, true) + spacing;
            }

            // Preview section: top padding + label/button row + texture strip + bottom spacing.
            height += SectionPadding          // gap before preview section
                    + lineH + spacing         // "Noise Preview" label + Refresh button row
                    + PreviewRectH + spacing; // texture strip

            return height;
        }

        // -------------------------------------------------------------------------
        // Drawing
        // -------------------------------------------------------------------------

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float lineH   = EditorGUIUtility.singleLineHeight;
            float x       = position.x;
            float w       = position.width;
            float y       = position.y;

            // -- Serialized fields --------------------------------------------------
            EditorGUI.BeginChangeCheck();

            foreach (string fieldName in FieldNames)
            {
                var fieldProp = property.FindPropertyRelative(fieldName);
                if (fieldProp == null) continue;

                float fieldH = EditorGUI.GetPropertyHeight(fieldProp, true);
                EditorGUI.PropertyField(new Rect(x, y, w, fieldH), fieldProp, true);
                y += fieldH + spacing;
            }

            bool fieldsChanged = EditorGUI.EndChangeCheck();

            // -- Preview section header --------------------------------------------
            y += SectionPadding;

            const float refreshButtonW = 60f;
            var labelRect   = new Rect(x, y, w - refreshButtonW - 4f, lineH);
            var refreshRect = new Rect(x + w - refreshButtonW, y, refreshButtonW, lineH);

            EditorGUI.LabelField(labelRect, "Noise Preview", EditorStyles.boldLabel);

            string key         = property.propertyPath;
            bool   forceRefresh = GUI.Button(refreshRect, "Refresh");

            if (forceRefresh && PreviewCache.ContainsKey(key))
                PreviewCache.Remove(key);

            y += lineH + spacing;

            // -- Texture strip ------------------------------------------------------
            var textureRect = new Rect(x, y, w, PreviewRectH);

            if (!PreviewCache.TryGetValue(key, out Texture2D tex) || tex == null || fieldsChanged)
            {
                tex             = BuildPreviewTexture(property);
                PreviewCache[key] = tex;
            }

            if (tex != null)
            {
                EditorGUI.DrawPreviewTexture(textureRect, tex);
            }
            else
            {
                // Gray fallback when sampling is unavailable.
                EditorGUI.DrawRect(textureRect, new Color(0.35f, 0.35f, 0.35f, 1f));
                EditorGUI.LabelField(textureRect, "Preview unavailable", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUI.EndProperty();
        }

        // -------------------------------------------------------------------------
        // Preview texture generation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Builds a 128x32 grayscale texture by sampling <see cref="FractalNoise.Sample2D"/>
        /// along the x-axis at the current Frequency and Octaves settings.
        /// FractalNoise.Sample2D is pure C# (no [BurstCompile]) and can be called at edit time.
        /// </summary>
        private static Texture2D BuildPreviewTexture(SerializedProperty property)
        {
            try
            {
                float frequency = property.FindPropertyRelative("Frequency")?.floatValue ?? 1f;
                int   octaves   = property.FindPropertyRelative("Octaves")?.intValue   ?? 3;

                var settings = new FractalSettings
                {
                    Octaves     = Mathf.Max(1, octaves),
                    Lacunarity  = 2f,
                    Persistence = 0.5f,
                    Amplitude   = 1f,
                    Frequency   = 1f,
                };

                var tex    = new Texture2D(PreviewTexWidth, PreviewTexHeight, TextureFormat.RGB24, false);
                tex.hideFlags = HideFlags.HideAndDontSave;
                var pixels = new Color[PreviewTexWidth * PreviewTexHeight];

                for (int px = 0; px < PreviewTexWidth; px++)
                {
                    // Sample along the x-axis; time dimension kept at 0 for a static preview.
                    float sampleX = (px / (float)PreviewTexWidth) * frequency;
                    float raw     = FractalNoise.Sample2D(new float2(sampleX, 0f), settings);

                    // Remap from approximately -1..1 to 0..1 grayscale.
                    float gray = Mathf.Clamp01(raw * 0.5f + 0.5f);
                    var   col  = new Color(gray, gray, gray, 1f);

                    for (int py = 0; py < PreviewTexHeight; py++)
                        pixels[py * PreviewTexWidth + px] = col;
                }

                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[NoiseFieldDrawer] Preview generation failed: " + e.Message);
                return BuildFallbackTexture();
            }
        }

        private static Texture2D BuildFallbackTexture()
        {
            var tex = new Texture2D(PreviewTexWidth, PreviewTexHeight, TextureFormat.RGB24, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var pixels = new Color[PreviewTexWidth * PreviewTexHeight];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.gray;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
