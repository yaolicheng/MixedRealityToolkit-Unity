﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Internal.Definitions.Physics;
using Microsoft.MixedReality.Toolkit.Internal.Extensions;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Internal.Utilities.Physics
{
    public static class MixedRealityRaycaster
    {
        public static bool DebugEnabled = false;

        /// <summary>
        /// Simple raycasts each physics <see cref="RayStep"/>
        /// </summary>
        /// <param name="step"></param>
        /// <param name="prioritizedLayerMasks"></param>
        /// <param name="physicsHit"></param>
        /// <returns></returns>
        public static bool RaycastSimplePhysicsStep(RayStep step, LayerMask[] prioritizedLayerMasks, out RaycastHit physicsHit)
        {
            return prioritizedLayerMasks.Length == 1
                // If there is only one priority, don't prioritize
                ? UnityEngine.Physics.Raycast(step.Origin, step.Direction, out physicsHit, step.Length, prioritizedLayerMasks[0])
                // Raycast across all layers and prioritize
                : TryGetPrioritizedPhysicsHit(UnityEngine.Physics.RaycastAll(step.Origin, step.Direction, step.Length, UnityEngine.Physics.AllLayers), prioritizedLayerMasks, out physicsHit);
        }

        /// <summary>
        /// Box raycasts each physics <see cref="RayStep"/>
        /// </summary>
        /// <returns></returns>
        public static bool RaycastBoxPhysicsStep(RayStep step, Vector3 extents, Vector3 targetPosition, Matrix4x4 matrix, float maxDistance, LayerMask[] prioritizedLayerMasks, int raysPerEdge, bool isOrthographic, out Vector3[] points, out Vector3[] normals, out bool[] hits)
        {
            if (Application.isEditor && DebugEnabled)
            {
                Debug.DrawLine(step.Origin, step.Origin + step.Direction * 10.0f, Color.green);
            }

            extents /= (raysPerEdge - 1);

            int halfRaysPerEdge = (int)((raysPerEdge - 1) * 0.5f);
            int numRays = raysPerEdge * raysPerEdge;
            bool hitSomething = false;

            points = new Vector3[numRays];
            normals = new Vector3[numRays];
            hits = new bool[numRays];

            int index = 0;

            for (int x = -halfRaysPerEdge; x <= halfRaysPerEdge; x += 1)
            {
                for (int y = -halfRaysPerEdge; y <= halfRaysPerEdge; y += 1)
                {
                    Vector3 offset = matrix.MultiplyVector(new Vector3(x * extents.x, y * extents.y, 0));

                    Vector3 origin = step.Origin;
                    Vector3 direction = (targetPosition + offset) - step.Origin;

                    if (isOrthographic)
                    {
                        origin += offset;
                        direction = step.Direction;
                    }

                    RaycastHit rayHit;
                    hits[index] = RaycastSimplePhysicsStep(new RayStep(origin, direction * maxDistance), prioritizedLayerMasks, out rayHit);

                    if (hits[index])
                    {
                        hitSomething = true;
                        points[index] = rayHit.point;
                        normals[index] = rayHit.normal;

                        if (Application.isEditor && DebugEnabled)
                        {
                            Debug.DrawLine(origin, points[index], Color.yellow);
                        }
                    }
                    else
                    {
                        if (Application.isEditor && DebugEnabled)
                        {
                            Debug.DrawLine(origin, origin + direction * 3.0f, Color.gray);
                        }
                    }

                    index++;
                }
            }

            return hitSomething;
        }

        /// <summary>
        /// Sphere raycasts each physics <see cref="RayStep"/>
        /// </summary>
        /// <param name="step"></param>
        /// <param name="radius"></param>
        /// <param name="prioritizedLayerMasks"></param>
        /// <param name="physicsHit"></param>
        /// <returns></returns>
        public static bool RaycastSpherePhysicsStep(RayStep step, float radius, LayerMask[] prioritizedLayerMasks, out RaycastHit physicsHit)
        {
            return prioritizedLayerMasks.Length == 1
                // If there is only one priority, don't prioritize
                ? UnityEngine.Physics.SphereCast(step.Origin, radius, step.Direction, out physicsHit, step.Length, prioritizedLayerMasks[0])
                // Raycast across all layers and prioritize
                : TryGetPrioritizedPhysicsHit(UnityEngine.Physics.SphereCastAll(step.Origin, radius, step.Direction, step.Length, UnityEngine.Physics.AllLayers), prioritizedLayerMasks, out physicsHit);
        }


        /// <summary>
        /// Tries to ge the prioritized physics raycast hit based on the prioritized layer masks.
        /// <para><remarks>Sorts all hit objects first by layerMask, then by distance.</remarks></para>
        /// </summary>
        /// <param name="hits"></param>
        /// <param name="priorityLayers"></param>
        /// <param name="raycastHit"></param>
        /// <returns>The minimum distance hit within the first layer that has hits</returns>
        public static bool TryGetPrioritizedPhysicsHit(RaycastHit[] hits, LayerMask[] priorityLayers, out RaycastHit raycastHit)
        {
            raycastHit = default(RaycastHit);

            if (hits.Length == 0)
            {
                return false;
            }

            for (int layerMaskIdx = 0; layerMaskIdx < priorityLayers.Length; layerMaskIdx++)
            {
                RaycastHit? minHit = null;

                for (int hitIdx = 0; hitIdx < hits.Length; hitIdx++)
                {
                    RaycastHit hit = hits[hitIdx];
                    if (hit.transform.gameObject.layer.IsInLayerMask(priorityLayers[layerMaskIdx]) &&
                        (minHit == null || hit.distance < minHit.Value.distance))
                    {
                        minHit = hit;
                    }
                }

                if (minHit != null)
                {
                    raycastHit = minHit.Value;
                    return true;
                }
            }

            return false;
        }
    }
}
