using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using _Project.Ray_Tracer.Scripts.RT_Ray;

namespace _Project.Ray_Caster.Scripts.RC_Ray
{
    /// <summary>
    /// Child of <see cref="RTRay"/>
    /// Represents a ray produced by the ray caster. Calculates the transfer function and compositing method step
    /// </summary>
    public class RCRay : RTRay
    {
        /// <summary>
        /// The compositing method used for this ray
        /// </summary>
        public Func<Sample, Sample, int, Color> ColorCompositingMethod;
        /// <summary>
        /// The list of samples taken along this ray
        /// </summary>
        public List<Sample> Samples;
        /// <summary>
        /// Index of the last sample that was drawn by this ray, used for drawing visualizable rays
        /// </summary>
        public int LastSampleDrawn;
        /// <summary>
        /// The length of the ray section before entering the voxel grid
        /// </summary>
        public float DistanceBeforeVoxelgrid;
        /// <summary>
        /// The length of the ray section inside the voxel grid
        /// </summary>
        public float DistanceInVoxelgrid;
        /// <summary>
        /// The length of the ray section after the voxel grid
        /// </summary>
        public float DistanceAfterVoxelgrid;
        /// <summary>
        /// The radius of this ray
        /// </summary>
        public float Radius;
        /// <summary>
        /// The color lookup table used for the transfer function
        /// </summary>
        public ColorTableEntry[] ColorLookupTable;
        /// <summary>
        /// class with linear interpolation algorithm
        /// </summary>
        private LinearInterpolation linearInterpolation;
        /// <summary>
        /// The alpha cutoff value used for the 'accumulate' compositing method
        /// </summary>
        public float alphaCutoffValue;
        /// <summary>
        /// The density matching value used for the 'first' compositing method
        /// </summary>
        public float densityMatchingValue;
        /// <summary>
        /// The ray caster manager
        /// </summary>
        private RayCasterManager rayCasterManager;
        /// <summary>
        /// The sample index at which early ray termination is activated
        /// </summary>
        public int earlyRayActivationIndex = -1;
        /// <summary>
        /// totalDensity of the samples, only used for the average compositing method
        /// </summary>
        public double totalDensity = 0;

        /// <summary>
        /// Used for the <see cref="RCRay.ColorLookupTable">
        /// </summary>
        public struct ColorTableEntry
        {
            public Color ColorAlpha;
            public float Density;

            public ColorTableEntry(Color colorAlpha, float density)
            {
                ColorAlpha = colorAlpha;
                Density = density;
            }
        }
        
        /// <summary>
        /// The available compositing methods
        /// </summary>
        public enum CompositingMethodType
        {
            Accumulate,
            Maximum,
            Average,
            First
        }

        public RCRay()
        {
            initializer();
        }

        public RCRay(Vector3 origin, Vector3 direction, float length, RayType type, float distanceBeforeVoxelgrid, float distanceInVoxelGrid, float distanceAfterVoxelgrid)
        {
            initializer();
            DistanceBeforeVoxelgrid = distanceBeforeVoxelgrid;
            DistanceInVoxelgrid = distanceInVoxelGrid;
            DistanceAfterVoxelgrid = distanceAfterVoxelgrid;
            Origin = origin;
            Direction = direction;
            Length = length;
            Type = type;
            Contribution = type == RayType.NoHit || type == RayType.Shadow ? 0.0f : 1.0f;
        }

        public RCRay(RCRay baseRay, int raySection)
        {
            initializer();
            earlyRayActivationIndex = baseRay.earlyRayActivationIndex;
            DistanceBeforeVoxelgrid = baseRay.DistanceBeforeVoxelgrid;
            DistanceInVoxelgrid = baseRay.DistanceInVoxelgrid;
            DistanceAfterVoxelgrid = baseRay.DistanceAfterVoxelgrid;
            Origin = baseRay.Origin;
            Direction = baseRay.Direction;
            Type = baseRay.Type;
            Contribution = Type == RayType.NoHit || Type == RayType.Shadow ? 0.0f : 1.0f;
            Color = baseRay.Color;
            switch (raySection)
            {
                case 0:
                    Origin = baseRay.Origin;
                    Radius = 0.2f * Radius;
                    Length = baseRay.DistanceBeforeVoxelgrid;
                    break;
                case 1:
                    Origin = baseRay.Origin + baseRay.Direction * DistanceBeforeVoxelgrid;
                    Samples = baseRay.Samples;
                    Length = baseRay.DistanceInVoxelgrid;
                    break;
                case 2:
                    Origin = baseRay.Origin + baseRay.Direction * (DistanceBeforeVoxelgrid+DistanceInVoxelgrid);
                    Radius = 0.2f * Radius;
                    Length = baseRay.DistanceAfterVoxelgrid;
                    break;
            }
        }

        /// <summary>
        /// Standard initialization used by every initializer
        /// </summary>
        private void initializer()
        {
            LastSampleDrawn = 0;
            rayCasterManager = RayCasterManager.Get();
            linearInterpolation = new LinearInterpolation();
            Radius = rayCasterManager.RayRadius;
            ColorLookupTable = rayCasterManager.ColorLookupTable;
            alphaCutoffValue = rayCasterManager.OpacityCutoffValue;
            densityMatchingValue = rayCasterManager.MatchingDensityValue;
            Samples = new List<Sample>();
            SetColorCompositingMethod(rayCasterManager.CompositingMethod);
        }

        /// <summary>
        /// Add a sample to <see cref="Samples"/>
        /// </summary>
        /// <param name="unityPosition">The unity coordinates of the sample</param>
        /// <param name="gridPostion">The voxel grid coordinates of the sample</param>
        /// <param name="density">The density of the sample</param>
        public void AddSample(Vector3 unityPosition, Vector3 gridPostion, double density)
        {
            Sample sample = new Sample(unityPosition, gridPostion, density, transferFunction(density));
            //double previousDensity = Samples.Count > 0 ? Samples[Samples.Count - 1].CompositedDensity : 0;
            //Color previousColor = Samples.Count > 0 ? Samples[^1].CompositedSampleColor : Color.clear;
            Sample previousSample = Samples.Count > 0 ? Samples[^1] : new Sample(0, Color.black);
            //sample.CompositedDensity = CompositingMethod(previousDensity, density, Samples.Count);
            sample.CompositedSampleColor = ColorCompositingMethod(previousSample, sample, Samples.Count);
            Samples.Add(sample);
        }

        /// <summary>
        /// Set the final pixel color
        /// </summary>
        public void LastSampleAdded()
        {
            Color lastColor = Samples.Count > 0 ? Samples[^1].CompositedSampleColor : Color.clear;
            // If the final composited color does not have an alpha value of 1 we "fill it up" with black background color
            Color finalColor = lastColor;
            if (lastColor.a < 1)
            {
                finalColor.r = lastColor.r * lastColor.a;
                finalColor.g = lastColor.g * lastColor.a;
                finalColor.b = lastColor.b * lastColor.a;
            }
            Color = finalColor;
        }
        
        /// <summary>
        /// Change which compositing method should be used
        /// </summary>
        /// <param name="method">The new compositing method that should be used</param>
        public void SetColorCompositingMethod(CompositingMethodType method)
        {
            switch (method)
            {
                case CompositingMethodType.Accumulate:
                {
                    ColorCompositingMethod = Accumulate;
                    break;
                }
                case CompositingMethodType.Maximum:
                {
                    ColorCompositingMethod = Maximum;
                    break;
                }
                case CompositingMethodType.Average:
                {
                    ColorCompositingMethod = Average;
                    break;
                }
                case CompositingMethodType.First:
                {
                    ColorCompositingMethod = First;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Accumulates the colors of the previous sample and the new sample
        /// </summary>
        /// <param name="previousSample">The previous sample taken</param>
        /// <param name="newSample">The new sample taken</param>
        /// <param name="numberOfRecordedSamples">The total number of samples taken</param>
        /// <returns></returns>
        private Color Accumulate(Sample previousSample, Sample newSample, int numberOfRecordedSamples)
        {
            Color previousColor = previousSample.CompositedSampleColor;
            Color newColor = newSample.SampleColor;
            Color result;
            result.a = previousColor.a + (1 - previousColor.a) * newColor.a;
            // If we have reached the maximum alpha value we can see through there is no need to keep going
            if (result.a >= alphaCutoffValue || earlyRayActivationIndex != -1)
            {
                if (earlyRayActivationIndex == -1)
                    earlyRayActivationIndex = numberOfRecordedSamples;
                return previousColor;
            }
            // To prevent division by 0 error we filter out this edge case where both alpha values are 0
            if (previousColor.a == 0 && newColor.a == 0)
                return previousColor;
            result.r = previousColor.r + (1 - previousColor.a) * newColor.r;
            result.r = result.r > 1 ? 1 : result.r;
            result.g = previousColor.g + (1 - previousColor.a) * newColor.g;
            result.g = result.g > 1 ? 1 : result.g;
            result.b = previousColor.b + (1 - previousColor.a) * newColor.b;
            result.b = result.b > 1 ? 1 : result.b;
            return result;
        }

        /// <summary>
        /// Takes the maximum density sample found on the ray and shows its transfer function color
        /// </summary>
        /// <param name="previousSample">The previous sample taken</param>
        /// <param name="newSample">The new sample taken</param>
        /// <param name="numberOfRecordedSamples">The total number of samples taken</param>
        /// <returns></returns>
        private Color Maximum(Sample previousSample, Sample newSample, int numberOfRecordedSamples)
        {
            return newSample.RecordedDensity > previousSample.RecordedDensity ? newSample.SampleColor : previousSample.CompositedSampleColor;
        }

        /// <summary>
        /// Takes the average density of the samples and shows that average densities transfer function color
        /// </summary>
        /// <param name="previousSample">The previous sample taken</param>
        /// <param name="newSample">The new sample taken</param>
        /// <param name="numberOfRecordedSamples">The total number of samples taken</param>
        /// <returns></returns>
        private Color Average(Sample previousSample, Sample newSample, int numberOfRecordedSamples)
        {
            totalDensity += newSample.RecordedDensity;
            return transferFunction(totalDensity / numberOfRecordedSamples);
        }

        /// <summary>
        /// Takes the first sample with a density higher than <see cref="densityMatchingValue"/> with a precision of two decimals,
        /// When found activates <see cref="earlyRayActivationIndex"/>
        /// </summary>
        /// <param name="previousSample">The previous sample taken</param>
        /// <param name="newSample">The new sample taken</param>
        /// <param name="numberOfRecordedSamples">The total number of samples taken</param>
        /// <returns></returns>
        private Color First(Sample previousSample, Sample newSample, int numberOfRecordedSamples)
        {
            // Guard. If we already found a first then there is no need to keep looking
            if (numberOfRecordedSamples > 1 && previousSample.CompositedSampleColor != Samples[0].CompositedSampleColor)
            {
                if (earlyRayActivationIndex == -1)
                    earlyRayActivationIndex = numberOfRecordedSamples;
                return previousSample.CompositedSampleColor;
            }

            return Math.Round(newSample.RecordedDensity, 2) == Math.Round(densityMatchingValue, 2)
                ? transferFunction(densityMatchingValue) : previousSample.CompositedSampleColor;
        }
        
        /// <summary>
        /// Translates a density into a color using the <see cref="ColorLookupTable"/>
        /// </summary>
        /// <param name="density">The density for which to calculate the color</param>
        /// <returns></returns>
        private Color transferFunction(double density)
        {
            return linearInterpolation.MonoLinearInterpolation((float)density, ColorLookupTable);
        }
    }
}