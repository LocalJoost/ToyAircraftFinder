using UnityEngine;

namespace CustomVison
{
    public static class PredictionExtensions
    {
        public static Vector2 GetCenter(this Prediction p)
        {
            return new Vector2((float) (p.BoundingBox.Left + (0.5 * p.BoundingBox.Width)),
                (float) (p.BoundingBox.Top + (0.5 * p.BoundingBox.Height)));
        }
    }
}