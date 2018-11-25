using System.Linq;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using HoloToolkitExtensions.Drawing;
using UnityEngine;

namespace HoloToolkitExtensions.Utilities
{
    public static class LookingDirectionHelpers
    {
        /// <summary>
        /// Get a position patial map right ahead of the camera viewing direction on a maximum distance
        /// and failing that, a position dead ahead
        /// </summary>
        /// <param name="maxDistance"></param>
        /// <param name="stabilizer"></param>
        /// <returns></returns>
        public static Vector3 GetPositionInLookingDirection(float maxDistance = 2,
                                  BaseRayStabilizer stabilizer = null)
        {
            var hitPoint = GetPositionOnSpatialMap(maxDistance, stabilizer);

            return hitPoint ?? CalculatePositionDeadAhead(maxDistance);
        }

        /// <summary>
        /// Get a position on the spatial map right ahead of the camera viewing direction
        /// </summary>
        /// <param name="maxDistance"></param>
        /// <param name="stabilizer"></param>
        /// <returns></returns>
        public static Vector3? GetPositionOnSpatialMap(float maxDistance = 2,
            BaseRayStabilizer stabilizer = null)
        {
            RaycastHit hitInfo;

            var headReady = stabilizer != null
                ? stabilizer.StableRay
                : new Ray(CameraCache.Main.transform.position, CameraCache.Main.transform.forward);

            if (SpatialMappingManager.Instance != null &&
                Physics.Raycast(headReady, out hitInfo, maxDistance,
                SpatialMappingManager.Instance.LayerMask))
            {
                return hitInfo.point;
            }

            return null;
        }

        /// <summary>
        /// Calculate a position right ahead of the camera viewing direction
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="stabilizer"></param>
        /// <returns></returns>
        public static Vector3 CalculatePositionDeadAhead(float distance = 2,
                                                         BaseRayStabilizer stabilizer = null)
        {
            return stabilizer != null
                ? stabilizer.StablePosition + stabilizer.StableRay.direction.normalized * distance
                : CameraCache.Main.transform.position + CameraCache.Main.transform.forward.normalized * distance;
        }

        /// <summary>
        /// Get a camera position from either a stabilizer or the Camera itself
        /// </summary>
        /// <param name="stabilizer"></param>
        /// <returns></returns>
        private static Vector3 GetCameraPosition(BaseRayStabilizer stabilizer)
        {
            return stabilizer != null
                ? stabilizer.StablePosition
                : CameraCache.Main.transform.position;
        }

        /// <summary>
        /// Looks where an object must be placed if it is not to intersect with other objects, or the spatial map
        /// Can be used to place an object on top of another object (or at a distance from it).
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="maxDistance"></param>
        /// <param name="distanceFromObstruction"></param>
        /// <param name="layerMask"></param>
        /// <param name="stabilizer"></param>
        /// <param name="showDebugLines"></param>
        /// <returns></returns>
        public static Vector3 GetObjectBeforeObstruction(GameObject obj, float maxDistance = 2,
            float distanceFromObstruction = 0.02f, int layerMask = Physics.DefaultRaycastLayers,
            BaseRayStabilizer stabilizer = null, bool showDebugLines = false)
        {
            var totalBounds = obj.GetEncapsulatingBounds();

            var headRay = stabilizer != null
                ? stabilizer.StableRay
                : new Ray(CameraCache.Main.transform.position, CameraCache.Main.transform.forward);


            // Project the object forward, get all hits *except those involving child objects of the main object*
            var hits = Physics.BoxCastAll(GetCameraPosition(stabilizer),
                                          totalBounds.extents, headRay.direction,
                                          Quaternion.identity, maxDistance, layerMask)
                                          .Where(h => !h.transform.IsChildOf(obj.transform)).ToList();

            // This factor compensates for the fact object center and bounds center for some reason are not always the same
            var centerCorrection = obj.transform.position - totalBounds.center;

            if (showDebugLines)
            {
                BoxCastHelper.DrawBoxCastBox(GetCameraPosition(stabilizer),
                    totalBounds.extents, headRay.direction,
                    Quaternion.identity, maxDistance, Color.green);
            }

            var orderedHits = hits.OrderBy(p => p.distance).Where(q => q.distance > 0.1f).ToList();

            if (orderedHits.Any())
            {
                var closestHit = orderedHits.First();
                //Find the closest hit - we need to move the object forward to that position

                //We need a vector from the camera to the hit...
                var hitVector = closestHit.point - GetCameraPosition(stabilizer);

                //But the hit needs to be projected on our dead ahead vector, as the first hit may not be right in front of us
                var gazeVector = CalculatePositionDeadAhead(closestHit.distance * 2) - GetCameraPosition(stabilizer);
                var projectedHitVector = Vector3.Project(hitVector, gazeVector);
#if UNITY_EDITOR
                if (showDebugLines)
                {
                    Debug.DrawLine(GetCameraPosition(stabilizer), closestHit.point, Color.yellow);
                    Debug.DrawRay(GetCameraPosition(stabilizer), gazeVector, Color.blue);
                    Debug.DrawRay(GetCameraPosition(stabilizer), projectedHitVector, Color.magenta);
                    BoxCastHelper.DrawBox(totalBounds.center, totalBounds.extents, Quaternion.identity, Color.red);
                }
#endif

                //If we use the projectedHitVector to add to the cameraposition, the CENTER of our object will end up 
                // against the obstruction, so we need to know who much the object is extending from the center in the direction of the hit.
                // So we make a ray from the center that intersects with the object's own bounds.
                var edgeRay = new Ray(totalBounds.center, projectedHitVector);
                float edgeDistance;
                if(totalBounds.IntersectRay(edgeRay,  out edgeDistance))
                {
                    if (showDebugLines)
                    {
                        Debug.DrawRay(totalBounds.center, projectedHitVector.normalized * Mathf.Abs(edgeDistance + distanceFromObstruction),
                            Color.cyan);
                    }
                }

                // The new position is not camera position plus the projected hit vector, minus distance to the edge and a possible extra distance
                // we want to keep.
                return GetCameraPosition(stabilizer) +
                            projectedHitVector - projectedHitVector.normalized * Mathf.Abs(edgeDistance + distanceFromObstruction) +
                            centerCorrection;
            }

            return CalculatePositionDeadAhead(maxDistance, stabilizer) + centerCorrection;
        }
    }
}
