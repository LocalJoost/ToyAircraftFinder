using HoloToolkitExtensions.Utilities;
using UnityEngine;

namespace HoloToolkitExtensions.Utilities
{
    /// <summary>
    /// Very simple behavior to keep something dead ahead of the user
    /// </summary>
    public class StickInView : MonoBehaviour
    {
        public float Distance = 1.0f;

        void Update()
        {
            var position = LookingDirectionHelpers.CalculatePositionDeadAhead(Distance);
            transform.position = position;
        }
    }
}
