using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.InputSystem.Combos
{
    /// <summary>
    /// Defines a sequence of inputs that trigger a specific combo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewComboDefinition", menuName = "Catalyst/Input/Combo Definition")]
    public class ComboDefinition : ScriptableObject
    {
        [Tooltip("Unique ID for this combo.")]
        public string ComboId;

        [Tooltip("The sequence of Action IDs required to trigger this combo.")]
        public List<string> Sequence = new List<string>();

        [Tooltip("Priority of this combo if sequences overlap.")]
        public int Priority = 0;
    }
}
