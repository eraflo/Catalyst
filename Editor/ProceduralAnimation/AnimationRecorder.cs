#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;

namespace Eraflo.Catalyst.ProceduralAnimation.Editor
{
    /// <summary>
    /// Utility for recording procedural animations and baking them to AnimationClips.
    /// </summary>
    public static class AnimationRecorder
    {
        /// <summary>
        /// Records a procedural move to an AnimationClip.
        /// </summary>
        /// <param name="rootTransform">The root transform to record paths relative to.</param>
        /// <param name="targetTransform">The transform being animated.</param>
        /// <param name="move">The procedural move to record.</param>
        /// <param name="fps">Frames per second for sampling.</param>
        /// <returns>A new AnimationClip with the recorded animation.</returns>
        public static AnimationClip Record(Transform rootTransform, Transform targetTransform, 
            Studio.ProceduralMove move, float fps = 30f)
        {
            if (move == null || move.Duration <= 0f)
            {
                Debug.LogWarning("[AnimationRecorder] Invalid move or zero duration.");
                return null;
            }
            
            var clip = new AnimationClip
            {
                frameRate = fps,
                legacy = false
            };
            
            string relativePath = AnimationUtility.CalculateTransformPath(targetTransform, rootTransform);
            
            int frameCount = Mathf.CeilToInt(move.Duration * fps) + 1;
            
            // Position curves
            var posXCurve = new AnimationCurve();
            var posYCurve = new AnimationCurve();
            var posZCurve = new AnimationCurve();
            
            // Rotation curves
            var rotXCurve = new AnimationCurve();
            var rotYCurve = new AnimationCurve();
            var rotZCurve = new AnimationCurve();
            var rotWCurve = new AnimationCurve();
            
            for (int frame = 0; frame < frameCount; frame++)
            {
                float time = frame / fps;
                var pose = move.Evaluate(time);
                
                posXCurve.AddKey(time, pose.Position.x);
                posYCurve.AddKey(time, pose.Position.y);
                posZCurve.AddKey(time, pose.Position.z);
                
                rotXCurve.AddKey(time, pose.Rotation.value.x);
                rotYCurve.AddKey(time, pose.Rotation.value.y);
                rotZCurve.AddKey(time, pose.Rotation.value.z);
                rotWCurve.AddKey(time, pose.Rotation.value.w);
            }
            
            // Apply curves to clip
            clip.SetCurve(relativePath, typeof(Transform), "localPosition.x", posXCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localPosition.y", posYCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localPosition.z", posZCurve);
            
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.x", rotXCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.y", rotYCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.z", rotZCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.w", rotWCurve);
            
            // Ensure proper quaternion interpolation
            clip.EnsureQuaternionContinuity();
            
            return clip;
        }
        
        /// <summary>
        /// Records multiple transforms using a custom evaluation function.
        /// </summary>
        public static AnimationClip RecordMultiple(
            Transform rootTransform,
            Transform[] targets,
            Func<float, int, AnimationPose> evaluator,
            float duration,
            float fps = 30f)
        {
            if (targets == null || targets.Length == 0 || duration <= 0f)
            {
                return null;
            }
            
            var clip = new AnimationClip
            {
                frameRate = fps,
                legacy = false
            };
            
            int frameCount = Mathf.CeilToInt(duration * fps) + 1;
            
            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                Transform target = targets[targetIndex];
                if (target == null) continue;
                
                string relativePath = AnimationUtility.CalculateTransformPath(target, rootTransform);
                
                var posXCurve = new AnimationCurve();
                var posYCurve = new AnimationCurve();
                var posZCurve = new AnimationCurve();
                var rotXCurve = new AnimationCurve();
                var rotYCurve = new AnimationCurve();
                var rotZCurve = new AnimationCurve();
                var rotWCurve = new AnimationCurve();
                
                for (int frame = 0; frame < frameCount; frame++)
                {
                    float time = frame / fps;
                    var pose = evaluator(time, targetIndex);
                    
                    posXCurve.AddKey(time, pose.Position.x);
                    posYCurve.AddKey(time, pose.Position.y);
                    posZCurve.AddKey(time, pose.Position.z);
                    
                    rotXCurve.AddKey(time, pose.Rotation.value.x);
                    rotYCurve.AddKey(time, pose.Rotation.value.y);
                    rotZCurve.AddKey(time, pose.Rotation.value.z);
                    rotWCurve.AddKey(time, pose.Rotation.value.w);
                }
                
                clip.SetCurve(relativePath, typeof(Transform), "localPosition.x", posXCurve);
                clip.SetCurve(relativePath, typeof(Transform), "localPosition.y", posYCurve);
                clip.SetCurve(relativePath, typeof(Transform), "localPosition.z", posZCurve);
                clip.SetCurve(relativePath, typeof(Transform), "localRotation.x", rotXCurve);
                clip.SetCurve(relativePath, typeof(Transform), "localRotation.y", rotYCurve);
                clip.SetCurve(relativePath, typeof(Transform), "localRotation.z", rotZCurve);
                clip.SetCurve(relativePath, typeof(Transform), "localRotation.w", rotWCurve);
            }
            
            clip.EnsureQuaternionContinuity();
            return clip;
        }
        
        /// <summary>
        /// Records a SpatialCurve as a path animation.
        /// </summary>
        public static AnimationClip RecordCurve(
            Transform rootTransform,
            Transform targetTransform,
            Studio.SpatialCurve curve,
            float duration,
            float fps = 30f,
            bool uniformSpeed = true)
        {
            if (curve == null || curve.PointCount < 2 || duration <= 0f)
            {
                return null;
            }
            
            var clip = new AnimationClip
            {
                frameRate = fps,
                legacy = false
            };
            
            string relativePath = AnimationUtility.CalculateTransformPath(targetTransform, rootTransform);
            
            int frameCount = Mathf.CeilToInt(duration * fps) + 1;
            float curveLength = uniformSpeed ? curve.CalculateLength() : 1f;
            
            var posXCurve = new AnimationCurve();
            var posYCurve = new AnimationCurve();
            var posZCurve = new AnimationCurve();
            var rotXCurve = new AnimationCurve();
            var rotYCurve = new AnimationCurve();
            var rotZCurve = new AnimationCurve();
            var rotWCurve = new AnimationCurve();
            
            for (int frame = 0; frame < frameCount; frame++)
            {
                float time = frame / fps;
                float t = time / duration;
                
                // Apply uniform speed if requested
                if (uniformSpeed)
                {
                    t = curve.GetUniformParameter(t * curveLength);
                }
                
                curve.EvaluateFrame(t, out float3 pos, out float3 tangent, out float3 up);
                quaternion rot = quaternion.LookRotation(tangent, up);
                
                posXCurve.AddKey(time, pos.x);
                posYCurve.AddKey(time, pos.y);
                posZCurve.AddKey(time, pos.z);
                
                rotXCurve.AddKey(time, rot.value.x);
                rotYCurve.AddKey(time, rot.value.y);
                rotZCurve.AddKey(time, rot.value.z);
                rotWCurve.AddKey(time, rot.value.w);
            }
            
            clip.SetCurve(relativePath, typeof(Transform), "localPosition.x", posXCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localPosition.y", posYCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localPosition.z", posZCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.x", rotXCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.y", rotYCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.z", rotZCurve);
            clip.SetCurve(relativePath, typeof(Transform), "localRotation.w", rotWCurve);
            
            clip.EnsureQuaternionContinuity();
            return clip;
        }
        
        /// <summary>
        /// Saves an AnimationClip as an asset.
        /// </summary>
        public static void SaveClipAsset(AnimationClip clip, string path)
        {
            if (clip == null) return;
            
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimationRecorder] Saved clip to: {path}");
        }
        
        /// <summary>
        /// Opens a save dialog and saves the clip.
        /// </summary>
        public static void SaveClipWithDialog(AnimationClip clip, string defaultName = "NewAnimation")
        {
            if (clip == null) return;
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Animation Clip",
                defaultName,
                "anim",
                "Save the recorded animation clip");
            
            if (!string.IsNullOrEmpty(path))
            {
                SaveClipAsset(clip, path);
            }
        }
    }
    
    /// <summary>
    /// Editor window for recording procedural animations.
    /// </summary>
    public class AnimationRecorderWindow : EditorWindow
    {
        private Transform _rootTransform;
        private Transform _targetTransform;
        private float _duration = 1f;
        private float _fps = 30f;
        private int _selectedPreset;
        private string[] _presetNames = new[]
        {
            "Custom Move", "Jab", "Heavy Punch", "Kick", "Wave", "Nod"
        };
        
        [MenuItem("Window/Catalyst/Animation Recorder")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationRecorderWindow>();
            window.titleContent = new GUIContent("Anim Recorder");
            window.minSize = new Vector2(300, 250);
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Animation Recorder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Record procedural moves to AnimationClip assets.", MessageType.Info);
            
            EditorGUILayout.Space();
            
            _rootTransform = (Transform)EditorGUILayout.ObjectField(
                "Root Transform", _rootTransform, typeof(Transform), true);
            _targetTransform = (Transform)EditorGUILayout.ObjectField(
                "Target Transform", _targetTransform, typeof(Transform), true);
            
            EditorGUILayout.Space();
            
            _selectedPreset = EditorGUILayout.Popup("Move Preset", _selectedPreset, _presetNames);
            _duration = EditorGUILayout.FloatField("Duration", _duration);
            _fps = EditorGUILayout.Slider("FPS", _fps, 15f, 60f);
            
            EditorGUILayout.Space();
            
            GUI.enabled = _rootTransform != null && _targetTransform != null;
            
            if (GUILayout.Button("Record & Save", GUILayout.Height(30)))
            {
                RecordAndSave();
            }
            
            if (GUILayout.Button("Preview"))
            {
                Preview();
            }
            
            GUI.enabled = true;
        }
        
        private void RecordAndSave()
        {
            var move = GetSelectedMove();
            if (move == null) return;
            
            var clip = AnimationRecorder.Record(_rootTransform, _targetTransform, move, _fps);
            if (clip != null)
            {
                AnimationRecorder.SaveClipWithDialog(clip, _presetNames[_selectedPreset]);
            }
        }
        
        private void Preview()
        {
            // Simple preview - could be enhanced with scene view visualization
            var move = GetSelectedMove();
            if (move == null) return;
            
            Debug.Log($"[AnimationRecorder] Preview: {_presetNames[_selectedPreset]}, " +
                $"Duration: {move.Duration:F2}s, Phases: {move.PhaseCount}");
        }
        
        private Studio.ProceduralMove GetSelectedMove()
        {
            float3 forward = _targetTransform != null 
                ? (float3)_targetTransform.forward 
                : new float3(0, 0, 1);
            
            return _selectedPreset switch
            {
                1 => Studio.ProceduralMoveLibrary.Jab(forward * 0.5f),
                2 => Studio.ProceduralMoveLibrary.HeavyPunch(forward * 0.7f),
                3 => Studio.ProceduralMoveLibrary.Kick(forward * 0.6f),
                4 => Studio.ProceduralMoveLibrary.Wave(),
                5 => Studio.ProceduralMoveLibrary.Nod(),
                _ => new Studio.ProceduralMove()
                    .Prepare(0.1f, -0.1f)
                    .Strike(forward * 0.5f, Studio.Curves.Smooth, _duration * 0.5f)
                    .Recover(0.7f, _duration * 0.5f)
            };
        }
    }
}
#endif
