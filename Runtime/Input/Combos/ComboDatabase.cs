using System.Collections.Generic;
using UnityEngine;

namespace Eraflo.Catalyst.InputSystem.Combos
{
    /// <summary>
    /// A collection of ComboDefinitions.
    /// </summary>
    [CreateAssetMenu(fileName = "NewComboDatabase", menuName = "Catalyst/Input/Combo Database")]
    public class ComboDatabase : ScriptableObject
    {
        public List<ComboDefinition> Combos = new List<ComboDefinition>();
    }
}
