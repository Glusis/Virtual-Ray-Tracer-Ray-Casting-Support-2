using System;
using UnityEngine;

namespace _Project.Ray_Caster.Scripts.RC_Ray
{
    /// <summary>
    /// A sample taken by a ray in the ray caster
    /// </summary>
    public class Sample
    {
        /// <summary>
        /// Unity coordinates of the sample
        /// </summary>
        public Vector3 UnityPosition;
        /// <summary>
        /// Voxel grid coordinates of the sample
        /// </summary>
        public Vector3 GridPosition;
        /// <summary>
        /// Radius of the visualized sample sphere
        /// </summary>
        public float Radius;
        /// <summary>
        /// Index of the sample in the object pool
        /// </summary>
        public int ObjectPoolIndex;
        /// <summary>
        /// The sample color given by the transfer function
        /// </summary>
        public Color SampleColor;
        /// <summary>
        /// The composited sample color given by the compositing method
        /// </summary>
        public Color CompositedSampleColor;
        /// <summary>
        /// The density of this sample
        /// </summary>
        public double RecordedDensity;

        public Sample(Vector3 unityPosition, Vector3 gridPosition, double density, Color color)
        {
            UnityPosition = unityPosition;
            GridPosition = gridPosition;
            RecordedDensity = density;
            // set default values:
            Radius = 0.1f;
            SampleColor = color;
        }
        
        public Sample(double density, Color color)
        {
            RecordedDensity = density;
            // set default values:
            Radius = 0.1f;
            SampleColor = color;
        }
    }
}