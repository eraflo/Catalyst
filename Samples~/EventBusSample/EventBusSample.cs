using UnityEngine;
using Eraflo.Catalyst.Events;

namespace Eraflo.Catalyst.Samples.Events
{
    /// <summary>
    /// Sample demonstrating the EventChannel system.
    /// Attach to any GameObject in the scene.
    /// </summary>
    public class EventBusSample : MonoBehaviour
    {
        [Header("Event Channels")]
        [SerializeField] private EventChannel onDamageChannel;
        [SerializeField] private IntEventChannel onHealChannel;
        [SerializeField] private IntEventChannel onScoreChannel;

        [Header("Controls")]
        [SerializeField] private KeyCode publishDamageKey = KeyCode.D;
        [SerializeField] private KeyCode publishHealKey = KeyCode.H;
        [SerializeField] private KeyCode publishScoreKey = KeyCode.S;

        private void OnEnable()
        {
            // Subscribe to events
            if (onDamageChannel != null)
                onDamageChannel.Subscribe(OnDamage);
            if (onHealChannel != null)
                onHealChannel.Subscribe(OnHeal);
            if (onScoreChannel != null)
                onScoreChannel.Subscribe(OnScore);
            
            Debug.Log("[EventBus Sample] Subscribed to events. Press D/H/S to publish.");
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (onDamageChannel != null)
                onDamageChannel.Unsubscribe(OnDamage);
            if (onHealChannel != null)
                onHealChannel.Unsubscribe(OnHeal);
            if (onScoreChannel != null)
                onScoreChannel.Unsubscribe(OnScore);
        }

        private void Update()
        {
            if (Input.GetKeyDown(publishDamageKey))
            {
                onDamageChannel?.Raise();
                Debug.Log("<color=orange>[PUBLISH]</color> Damage event raised!");
            }

            if (Input.GetKeyDown(publishHealKey))
            {
                int amount = Random.Range(5, 25);
                onHealChannel?.Raise(amount);
                Debug.Log($"<color=orange>[PUBLISH]</color> Heal event raised with {amount}");
            }

            if (Input.GetKeyDown(publishScoreKey))
            {
                int points = Random.Range(100, 1000);
                onScoreChannel?.Raise(points);
                Debug.Log($"<color=orange>[PUBLISH]</color> Score event raised with {points}");
            }
        }

        // Event handlers
        private void OnDamage()
        {
            Debug.Log($"<color=red>[DAMAGE]</color> Received damage event!");
        }

        private void OnHeal(int amount)
        {
            Debug.Log($"<color=green>[HEAL]</color> Healed {amount} HP");
        }

        private void OnScore(int points)
        {
            Debug.Log($"<color=yellow>[SCORE]</color> +{points} points!");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 180));
            GUILayout.Box("EventChannel Sample");
            GUILayout.Label("Press D - Raise Damage Event");
            GUILayout.Label("Press H - Raise Heal Event (with int value)");
            GUILayout.Label("Press S - Raise Score Event (with int value)");
            GUILayout.Space(10);
            GUILayout.Label("Watch Console for output");
            GUILayout.Space(10);
            GUILayout.Label($"Damage subscribers: {onDamageChannel?.SubscriberCount ?? 0}");
            GUILayout.Label($"Heal subscribers: {onHealChannel?.SubscriberCount ?? 0}");
            GUILayout.Label($"Score subscribers: {onScoreChannel?.SubscriberCount ?? 0}");
            GUILayout.EndArea();
        }
    }
}
