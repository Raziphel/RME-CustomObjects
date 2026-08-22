using UnityEngine;

namespace RazisRealm.RmeCustomObjects
{
    [DisallowMultipleComponent]
    public sealed class RmeCustomObjectRoot : MonoBehaviour
    {
        public string ObjectName = "NewCustomObject";
        [TextArea] public string Description = "Reusable RME custom object";
    }
}
