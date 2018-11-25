using System.Collections;
using UnityEngine;

namespace HoloToolkitExtensions.Utilities
{
    public static class GameObjectExtensions 
    {
        public static void SetHittableStatus(this GameObject gameObject, bool hittable)
        {
            foreach (var collider in gameObject.GetComponentsInChildren<Collider>())
            {
                collider.enabled = hittable;
            }
        }

        public static Bounds GetEncapsulatingBounds(this GameObject obj)
        {
            Bounds totalBounds = new Bounds();

            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                if (totalBounds.size.magnitude == 0f)
                {
                    totalBounds = renderer.bounds;
                }
                else
                {
                    totalBounds.Encapsulate(renderer.bounds);
                }
            }

            return totalBounds;
        }

        public static string GetObjectText(this GameObject textContainingObject)
        {
            var component = textContainingObject.GetComponent<TextMesh>();
            return component != null ? component.text : null;
        }

        public static bool SetObjectText(this GameObject textContainingObject, string newValue)
        {
            var component = textContainingObject.GetComponent<TextMesh>();
            if (component == null)
            {
                return false;
            }

            component.text = newValue;
            return true;
        }
    }
}
