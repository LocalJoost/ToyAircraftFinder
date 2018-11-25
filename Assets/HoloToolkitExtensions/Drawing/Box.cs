using UnityEngine;

namespace HoloToolkitExtensions.Drawing
{
    /// <summary>
    /// Nicked from https://answers.unity.com/questions/1156087/how-can-you-visualize-a-boxcast-boxcheck-etc.html
    /// </summary>
    public class Box
    {
        public Vector3 localFrontTopLeft { get; private set; }
        public Vector3 localFrontTopRight { get; private set; }
        public Vector3 localFrontBottomLeft { get; private set; }
        public Vector3 localFrontBottomRight { get; private set; }

        public Vector3 localBackTopLeft
        {
            get { return -localFrontBottomRight; }
        }

        public Vector3 localBackTopRight
        {
            get { return -localFrontBottomLeft; }
        }

        public Vector3 localBackBottomLeft
        {
            get { return -localFrontTopRight; }
        }

        public Vector3 localBackBottomRight
        {
            get { return -localFrontTopLeft; }
        }

        public Vector3 frontTopLeft
        {
            get { return localFrontTopLeft + origin; }
        }

        public Vector3 frontTopRight
        {
            get { return localFrontTopRight + origin; }
        }

        public Vector3 frontBottomLeft
        {
            get { return localFrontBottomLeft + origin; }
        }

        public Vector3 frontBottomRight
        {
            get { return localFrontBottomRight + origin; }
        }

        public Vector3 backTopLeft
        {
            get { return localBackTopLeft + origin; }
        }

        public Vector3 backTopRight
        {
            get { return localBackTopRight + origin; }
        }

        public Vector3 backBottomLeft
        {
            get { return localBackBottomLeft + origin; }
        }

        public Vector3 backBottomRight
        {
            get { return localBackBottomRight + origin; }
        }

        public Vector3 origin { get; private set; }

        public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
        {
            Rotate(orientation);
        }

        public Box(Vector3 origin, Vector3 halfExtents)
        {
            this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
            this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
            this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

            this.origin = origin;
        }


        public void Rotate(Quaternion orientation)
        {
            localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
            localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
            localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
            localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
        }

        //This should work for all cast types
        static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
        {
            return origin + (direction.normalized * hitInfoDistance);
        }

        static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            Vector3 direction = point - pivot;
            return pivot + rotation * direction;
        }
    }
}
