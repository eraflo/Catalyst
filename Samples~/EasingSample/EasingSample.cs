using UnityEngine;
using Eraflo.UnityImportPackage.EasingSystem;

namespace Eraflo.UnityImportPackage.Samples.Easing
{
    /// <summary>
    /// Sample demonstrating the Easing system.
    /// Attach to any GameObject in the scene to see animated cubes with different easings.
    /// </summary>
    public class EasingSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float animationDuration = 2f;
        [SerializeField] private float moveDistance = 5f;

        private float _time;
        private bool _forward = true;
        private GameObject[] _cubes;
        private Vector3[] _startPositions;

        // Easings to demonstrate
        private readonly EasingType[] _easings = new[]
        {
            EasingType.Linear,
            EasingType.QuadInOut,
            EasingType.CubicInOut,
            EasingType.ElasticOut,
            EasingType.BounceOut,
            EasingType.BackOut
        };

        private void Start()
        {
            CreateCubes();
            Debug.Log("[Easing Sample] Started. Watch the cubes animate with different easing functions.");
        }

        private void CreateCubes()
        {
            _cubes = new GameObject[_easings.Length];
            _startPositions = new Vector3[_easings.Length];

            for (int i = 0; i < _easings.Length; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = _easings[i].ToString();
                cube.transform.position = new Vector3(-moveDistance / 2, i * 1.5f, 0);
                cube.transform.localScale = Vector3.one * 0.8f;

                // Color gradient
                var renderer = cube.GetComponent<Renderer>();
                renderer.material.color = Color.HSVToRGB((float)i / _easings.Length, 0.8f, 1f);

                _cubes[i] = cube;
                _startPositions[i] = cube.transform.position;

                // Add label
                CreateLabel(cube, _easings[i].ToString());
            }
        }

        private void CreateLabel(GameObject cube, string text)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(cube.transform);
            labelGo.transform.localPosition = new Vector3(0, 0.8f, 0);
            
            var textMesh = labelGo.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.fontSize = 24;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
        }

        private void Update()
        {
            // Progress time
            _time += Time.deltaTime / animationDuration;

            if (_time >= 1f)
            {
                _time = 0f;
                _forward = !_forward;
            }

            // Animate each cube with its easing
            for (int i = 0; i < _cubes.Length; i++)
            {
                float t = _forward ? _time : 1f - _time;
                float easedT = EasingSystem.Easing.Evaluate(t, _easings[i]);

                var pos = _startPositions[i];
                pos.x = Mathf.Lerp(-moveDistance / 2, moveDistance / 2, easedT);
                _cubes[i].transform.position = pos;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Box("Easing Sample");
            GUILayout.Label($"Progress: {_time:P0}");
            GUILayout.Label($"Direction: {(_forward ? "Forward" : "Backward")}");
            GUILayout.EndArea();
        }
    }
}
