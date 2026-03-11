using UnityEngine;
using UnityEngine.UI;

namespace Eraflo.Catalyst.Command.UI
{
    /// <summary>
    /// Simple helper to bind UI Buttons to CommandManager Undo/Redo functions.
    /// </summary>
    public class UndoRedoUI : MonoBehaviour
    {
        [Header("Buttons")]
        public Button UndoButton;
        public Button RedoButton;

        [Header("Settings")]
        public bool AutoEnableDisableButtons = true;

        [Inject] private CommandManager _manager;

        private void Start()
        {
            if (UndoButton != null)
                UndoButton.onClick.AddListener(OnUndoClick);

            if (RedoButton != null)
                RedoButton.onClick.AddListener(OnRedoClick);
        }

        private void Update()
        {
            if (AutoEnableDisableButtons && _manager != null)
            {
                if (UndoButton != null) UndoButton.interactable = _manager.UndoCount > 0;
                if (RedoButton != null) RedoButton.interactable = _manager.RedoCount > 0;
            }
        }

        private async void OnUndoClick()
        {
            if (_manager != null) await _manager.Undo();
        }

        private async void OnRedoClick()
        {
            if (_manager != null) await _manager.Redo();
        }
    }
}
