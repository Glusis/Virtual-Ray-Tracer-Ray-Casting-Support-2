using _Project.Ray_Tracer.Scripts;
using System.Collections.Generic;
using UnityEngine;
using _Project.Ray_Caster.Scripts.Voxel_Grid;
using _Project.Ray_Caster.Scripts.RC_Ray;
using _Project.Ray_Tracer.Scripts.RT_Ray;
using _Project.Ray_Tracer.Scripts.Utility;

namespace _Project.Ray_Caster.Scripts
{
    /// <summary>
    /// Implementation of ray casting algorithm
    /// </summary>
    public class UnityRayCaster : UnityRayTracer
    {
        private static UnityRayCaster instance = null;
        private LinearInterpolation linearInterpolation;
        
        /// <summary>
        /// Whether to perform early ray termination
        /// </summary>
        [SerializeField] private bool doRayTermination;
        public bool DoRayTermination
        {
            get { return doRayTermination; }
            set
            {
                if (value == doRayTermination) return;
                doRayTermination = value;
                callRayTracerChanged();
            }
        }
        
        /// <summary>
        /// The distance between the samples on a ray in unity space
        /// </summary>
        [SerializeField] 
        private float distanceBetweenSamples = 0.1f;
        /// <summary>
        /// The maximum depth of any ray tree produced by this ray tracer.
        /// </summary>
        public float DistanceBetweenSamples
        {
            get { return distanceBetweenSamples; }
            set
            {
                if (value == distanceBetweenSamples) return;
                distanceBetweenSamples = value;
                callRayTracerChanged();
            }
        }
        
        /// <summary>
        /// Hit information used for ray casting
        /// </summary>
        private readonly struct CasterHitInfo
        {
            public readonly Vector3 Entry;
            public readonly Vector3 Exit;
            public readonly VoxelGrid EncounteredVoxelGrid;
            public readonly float DistanceBeforeVoxelGrid;
            public readonly float DistanceInVoxelGrid;
            public readonly float DistanceAfterVoxelGrid;
            public readonly bool didHit;
        
            public CasterHitInfo(ref RaycastHit hit1, ref RaycastHit hit2, bool didHit)
            {
                if (didHit)
                {
                    Entry = hit1.point;
                    Exit = hit2.point;
                    EncounteredVoxelGrid = hit1.transform.GetComponent<VoxelGrid>();
                    DistanceInVoxelGrid = hit2.distance + 0.001f;
                    DistanceBeforeVoxelGrid = hit1.distance;
                    DistanceAfterVoxelGrid = Mathf.Infinity;
                    this.didHit = didHit;
                }
                else
                {
                    Entry = new Vector3();
                    Exit = new Vector3();
                    EncounteredVoxelGrid = null;
                    DistanceInVoxelGrid = 0;
                    DistanceBeforeVoxelGrid = Mathf.Infinity;
                    DistanceAfterVoxelGrid = 0;
                    this.didHit = didHit;
                }
            }
        }
        
        /// <summary>
        /// Render visualizable rays
        /// </summary>
        /// <returns>List of treenodes with RCRays</returns>
        public new List<TreeNode<RTRay>> Render()
        {
            AccelerationPrep();
            List<TreeNode<RTRay>> rayTrees = new List<TreeNode<RTRay>>();
            rtSceneManager = RTSceneManager.Get();
            scene = rtSceneManager.Scene;
            camera = scene.Camera;
        
            int width = camera.ScreenWidth;
            int height = camera.ScreenHeight;
            float aspectRatio = (float)width / height;
            float halfScreenHeight = camera.ScreenDistance * Mathf.Tan(Mathf.Deg2Rad * camera.FieldOfView / 2.0f);
            float halfScreenWidth = aspectRatio * halfScreenHeight;
            float pixelWidth = halfScreenWidth * 2.0f / width;
            float pixelHeight = halfScreenHeight * 2.0f / height;
            int ssFactor = superSamplingVisual ? SuperSamplingFactor : 1;
            int ssSquared = ssFactor * ssFactor;
            Vector3 origin = camera.transform.position;
            float step = 1f / ssFactor;
        
            // Trace a ray for each pixel. 
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Color color = Color.black;
        
                    // Set a base Ray with a zero-distance as the main ray of the pixel
                    float centerPixelX = -halfScreenWidth + pixelWidth * (x + 0.5f);
                    float centerPixelY = -halfScreenHeight + pixelHeight * (y + 0.5f);
                    Vector3 centerPixel = new Vector3(centerPixelX, centerPixelY, camera.ScreenDistance);
                    TreeNode<RTRay> rayTree = new TreeNode<RTRay>(new RCRay());
                    rayTree.Data = new RCRay(origin, centerPixel / centerPixel.magnitude, 0f, RTRay.RayType.Normal, 0, 0, 0);
        
                    for (int supY = 0; supY < ssFactor; supY++)
                    {
                        float pixelY = centerPixelY + pixelHeight * (step * (0.5f + supY) - 0.5f);
        
                        for (int supX = 0; supX < ssFactor; supX++)
                        {
                            float pixelX = centerPixelX + pixelWidth * (step * (0.5f + supX) - 0.5f);
        
                            // Create and rotate the pixel location. Note that the camera looks along the positive z-axis.
                            Vector3 pixel = new Vector3(pixelX, pixelY, camera.ScreenDistance);
                            pixel = camera.transform.rotation * pixel;
        
                            // This is the distance between the pixel on the screen and the origin. We need this to compensate
                            // for the length of the returned RTRay. Since we have this factor we also use it to normalize this
                            // vector to make the code more efficient.
                            float pixelDistance = pixel.magnitude;
        
                            // perform the ray casting
                            TreeNode<RTRay> subRayTree = CastVisualizableRay(origin + pixel, pixel / pixelDistance);
                            rayTree.AddChild(subRayTree);
                            color += subRayTree.Data.Color;
                        }
                    }
        
                    // Divide by superSamplingFactorSquared and set alpha levels back to 1. It should always be 1!
                    color /= ssSquared;
                    color.a = 1.0f;
                    
                    rayTree.Data.Color = color;
                    rayTrees.Add(rayTree);
                }
            }
            AccelerationCleanupTree();
            return rayTrees;
        }
        
        /// <summary>
        /// Cast a ray for a single picture in a high resolution image
        /// </summary>
        /// <param name="origin">origin of the ray</param>
        /// <param name="direction">direction of the ray</param>
        /// <param name="depth">not used but needed to be able to override</param>
        /// <returns>color of ray for pixel</returns>
        protected override Color TraceImage(Vector3 origin, Vector3 direction, int depth)
        {
            RCRay ray = CastRay(origin, direction);
            return ClampColor(ray.Color);
        }
        
        public new static UnityRayCaster Get()
        {
            return instance;
        }

        /// <summary>
        /// Calculate the amount of samples needed over a set ray distance
        /// </summary>
        /// <param name="distance">The distance for all the samples</param>
        /// <returns>The amount of samples that will fit in the given distance</returns>
        private int CalculateAmountOfSamples(float distance)
        {
           return (int) (distance / distanceBetweenSamples);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="amountOfSamples"></param>
        /// <param name="distance"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private Vector3 CalculateDeltaPerSample(int amountOfSamples, float distance, Vector3 direction)
        {
            switch (amountOfSamples)
            {
                case 0: 
                case 1: 
                    return new Vector3(); // delta per sample will not be used
                default: 
                    return (direction * distance) / (amountOfSamples-1); // We take amountOfSamples-1 so the first and last point are on the edge of the volume
            }
        }

        /// <summary>
        /// Cast a new ray
        /// </summary>
        /// <param name="origin">Origin of ray</param>
        /// <param name="direction">Direction of ray</param>
        /// <returns><see cref="RCRay"/> of ray</returns>
        private RCRay CastRay(Vector3 origin, Vector3 direction)
        {
            CasterHitInfo hitInfo = GetCasterHitInfo(origin, direction);
            return CastRay(origin, direction, hitInfo);
        }
        
        /// <summary>
        /// Cast a new ray
        /// </summary>
        /// <param name="origin">Origin of ray</param>
        /// <param name="direction">Direction of ray</param>
        /// <param name="hitinfo">hit information of ray</param>
        /// <returns><see cref="RCRay"/> of ray</returns>
        private RCRay CastRay(Vector3 origin, Vector3 direction, CasterHitInfo hitInfo)
        {
            int amountOfSamples = CalculateAmountOfSamples(hitInfo.DistanceInVoxelGrid);
            return CastRay(origin, direction, hitInfo, amountOfSamples);
        }
        
        /// <summary>
        /// Cast a new ray
        /// </summary>
        /// <param name="origin">Origin of ray</param>
        /// <param name="direction">Direction of ray</param>
        /// <param name="hitinfo">hitinfo of ray</param>
        /// <param name="amountOfSamples">the amount of samples to be taken along the ray</param>
        /// <returns><see cref="RCRay"/> of ray</returns>
        private RCRay CastRay(Vector3 origin, Vector3 direction, CasterHitInfo hitInfo, int amountOfSamples)
        {
            RCRay rcRay;
            if (!hitInfo.didHit)
            {
                rcRay = new RCRay(origin, direction, Mathf.Infinity, RCRay.RayType.NoHit, 0, 0, Mathf.Infinity);
                rcRay.LastSampleAdded();
                return rcRay;
            }
            rcRay = new RCRay(origin, direction, Mathf.Infinity, RCRay.RayType.Normal, hitInfo.DistanceBeforeVoxelGrid, hitInfo.DistanceInVoxelGrid, hitInfo.DistanceAfterVoxelGrid);
            Vector3 deltaPerSample = CalculateDeltaPerSample(amountOfSamples, hitInfo.DistanceInVoxelGrid, direction);
            VoxelGrid voxelGrid = hitInfo.EncounteredVoxelGrid;
            for (int i = 0; i < amountOfSamples; i++)
            {
                Vector3 unityCoords = hitInfo.Entry + (deltaPerSample * i);
                Vector3 gridCoords = unityCoordsToGridCoords(unityCoords, voxelGrid);
                double density = linearInterpolation.TriLinearInterpolation(gridCoords, voxelGrid);
                rcRay.AddSample(unityCoords, gridCoords, density);
                if (doRayTermination && rcRay.earlyRayActivationIndex != -1)
                {
                    rcRay.DistanceInVoxelgrid = distanceBetweenSamples * (i+1);
                    rcRay.DistanceAfterVoxelgrid = 0;
                    break;
                }
            }
            rcRay.LastSampleAdded();
            return rcRay;
        }

        /// <summary>
        /// Calculates the grid coordinates related to the given unity coordinates
        /// </summary>
        /// <param name="unityCoords">The coordinates to calculate the gridposition for</param>
        /// <param name="voxelGrid">The voxelgrid that the unityCoords come from</param>
        /// <returns></returns>
        private Vector3 unityCoordsToGridCoords(Vector3 unityCoords, VoxelGrid voxelGrid)
        {
            Vector3 gridCoords = unityCoords;
            // We first translate so the unity cube is at (0, 0, 0). We of course do not actually translate the whole 
            // cube, we simply translate the given unityPosition to where it would be if the whole cube had been translated
            gridCoords = gridCoords - voxelGrid.Position;
            // Now that our position has its origin at (0, 0, 0) we can rotate it (since rotation is always around the origin)
            gridCoords = Quaternion.Inverse(voxelGrid.transform.rotation) * gridCoords;
            // Now that the cube and grid are both rotated the same way, we can set their bottom left corners to the same position
            // The middle of the cube is currently at (0, 0, 0). We must use the cubes scale to set its bottom left corner to (0,0,0)
            gridCoords += voxelGrid.Scale / 2;
            // Now that they both have the same rotation and their bottom left corners are at (0,0,0) we can scale them
            // so they are the same size
            gridCoords.x = gridCoords.x / voxelGrid.Scale.x * (voxelGrid.SizeX-1); // First divide by the scale of the unity volume, then multiply by the scale of the grid
            gridCoords.y = gridCoords.y / voxelGrid.Scale.y * (voxelGrid.SizeY-1);
            gridCoords.z = gridCoords.z / voxelGrid.Scale.z * (voxelGrid.SizeZ-1);
            // Now we have the grid coordinates equivalent to the given unity coordinates
            return gridCoords;
        }

        /// <summary>
        /// Cast a new ray that can be visualized
        /// </summary>
        /// <param name="origin">Origin of the ray</param>
        /// <param name="direction">Direction of the ray</param>
        /// <returns>Treenode with RCRays consisting of 3 parts, the first part contains the ray section before entering
        /// the voxel grid, the second part is the ray section in the voxel grid, the last section is after the
        /// voxel grid</returns>
        private TreeNode<RTRay> CastVisualizableRay(Vector3 origin, Vector3 direction)
        {
            RCRay baseRay = CastRay(origin, direction);
            
            TreeNode<RTRay> nodeOne = new TreeNode<RTRay>(new RCRay(baseRay, 0 ));
            TreeNode<RTRay> nodeTwo = new TreeNode<RTRay>(new RCRay(baseRay, 1));
            TreeNode<RTRay> nodeThree = new TreeNode<RTRay>(new RCRay(baseRay, 2));
            nodeTwo.AddChild(nodeThree);
            nodeOne.AddChild(nodeTwo);
            return nodeOne;
        }

        /// <summary>
        /// Get the hit info for a ray
        /// </summary>
        /// <param name="origin">Origin of ray</param>
        /// <param name="direction">Direction of ray</param>
        /// <returns>The hitinfo of the ray</returns>
        private CasterHitInfo GetCasterHitInfo(Vector3 origin, Vector3 direction)
        {
            if (!Physics.Raycast(origin, direction, out RaycastHit hit1, Mathf.Infinity, rayTracerLayer))
            {
                return new CasterHitInfo(ref hit1, ref hit1, false);
            }
            // To get the entry and exit point we cast a second physics ray. hit1 will contain the entry point, and hit2 will contain the exit point
            Physics.Raycast(hit1.point + direction * 0.001f, direction, out RaycastHit hit2, Mathf.Infinity, rayTracerLayer);
            return new CasterHitInfo(ref hit1, ref hit2, true);
        }
        
        protected override void Awake()
        {
            base.Awake();
            instance = this;
            linearInterpolation = new LinearInterpolation();
        }
    }
}