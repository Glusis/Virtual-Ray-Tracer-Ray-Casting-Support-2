using _Project.Ray_Tracer.Scripts;
using UnityEngine;
using _Project.Ray_Caster.Scripts.RC_Ray;
using _Project.Ray_Caster.Scripts.Voxel_Grid;
using _Project.Ray_Tracer.Scripts.RT_Ray;
using _Project.Ray_Tracer.Scripts.Utility;

namespace _Project.Ray_Caster.Scripts
{
    /// <summary>
    /// Child of <see cref="RayManager"/>, adapted for ray casting
    /// </summary>
    class RayCasterManager : RayManager
    {
        /// <summary>
        /// Material for ray casting visuals, this had to be changed so rays and samples could be transparent
        /// </summary>
        [SerializeField] private Material rayCastingMaterial;
        /// <summary>
        /// The prefab for a sample
        /// </summary>
        [SerializeField] private SampleObject samplePrefab;
        private SampleObjectPool sampleObjectPool;
        /// <summary>
        /// The initial amount of sample objects in the sample object pool
        /// </summary>
        private int samplePoolSize = 128;
        private UnityRayCaster unityRayCaster;
        private static RayCasterManager instance;
        /// <summary>
        /// Whether or not to display opacity of ray and samples
        /// </summary>
        [SerializeField] private bool showOpacity;
        public bool ShowOpacity
        {
            get { return showOpacity; }
            set
            {
                if (value == showOpacity) return;
                showOpacity = value;
                UpdateRays();
            }
        }

        /// <summary>
        /// The opacity cutoff value used for the accumulate compositing method
        /// </summary>
        [SerializeField] private float opacityCutoffValue;
        public float OpacityCutoffValue
        {
            get { return opacityCutoffValue;}
            set
            {
                if (value == opacityCutoffValue) return;
                opacityCutoffValue = value;
                foreach (TreeNode<RTRay> treeNode in rays)
                {
                    (treeNode.Data as RCRay).alphaCutoffValue = value;
                    foreach (TreeNode<RTRay> rtRay in treeNode.Children) // Skip 0 length base ray
                        (rtRay.Data as RCRay).alphaCutoffValue = value;
                }
                UpdateRays();
            }
        }
        
        /// <summary>
        /// The density value used for matching in the first compositing method
        /// </summary>
        [SerializeField] private float matchingDensityValue;
        public float MatchingDensityValue
        {
            get { return matchingDensityValue;}
            set
            {
                if (value == matchingDensityValue) return;
                matchingDensityValue = value;
                foreach (TreeNode<RTRay> treeNode in rays)
                {
                    (treeNode.Data as RCRay).densityMatchingValue = value;
                    foreach (TreeNode<RTRay> rtRay in treeNode.Children) // Skip 0 length base ray
                        (rtRay.Data as RCRay).densityMatchingValue = value;
                }
                UpdateRays();
            }
        }
        
        /// <summary>
        /// The compositing method to use
        /// </summary>
        [SerializeField]
        private RCRay.CompositingMethodType compositingMethod;
        public RCRay.CompositingMethodType CompositingMethod
        {
            get { return compositingMethod;}
            set {
                if (value == compositingMethod) return;
                compositingMethod = value;
                foreach (TreeNode<RTRay> treeNode in rays)
                {
                    (treeNode.Data as RCRay).SetColorCompositingMethod(compositingMethod);
                    foreach (TreeNode<RTRay> rtRay in treeNode.Children) // Skip 0 length base ray
                        (rtRay.Data as RCRay).SetColorCompositingMethod(compositingMethod);
                }
                UpdateRays();
            }
        }
        
        /// <summary>
        /// The color lookup table used for the transfer function
        /// </summary>
        private RCRay.ColorTableEntry[] colorLookupTable;
        public RCRay.ColorTableEntry[] ColorLookupTable
        {
            get { return colorLookupTable;}
            set
            {
                if (value == colorLookupTable) return;
                colorLookupTable = value;
                foreach (TreeNode<RTRay> treeNode in rays)
                foreach (TreeNode<RTRay> rtRay in treeNode.Children) // Skip 0 length base ray
                    (rtRay.Data as RCRay).ColorLookupTable = value;
                UpdateRays();
            }
        }

        public new static RayCasterManager Get()
        {
            return instance;
        }
        
        protected override void Start()
        {
            VoxelGrid voxelGrid = VoxelGrid.Get();
            colorLookupTable = voxelGrid.RecommendedColorLookupTable;
            sampleObjectPool = new SampleObjectPool(samplePrefab, samplePoolSize, transform);
            unityRayCaster = UnityRayCaster.Get();
            base.Start();
        }

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }
        
        /// <summary>
        /// Update the rays and their samples
        /// </summary>
        public override void UpdateRays()
        {
            if (!VoxelGrid.Get().SelectedVoxelGridDoneLoading())
                return;
            rays = unityRayCaster.Render();
            rayObjectPool.MakeRayObjects(rays);
            sampleObjectPool.MakeSampleObjects(rays);
            //sampleObjectPool.MakeSampleObjects(rays);
            rtSceneManager.UpdateImage(GetRayColors());
            redraw = true;
        }

        /// <summary>
        /// Same as <see cref="RayManager.Redraw()"/>, with the additions for samples
        /// </summary>
        protected override void Redraw()
        {
            if (hasSelectedRay)
                sampleObjectPool.DeactivateAll();
            base.Redraw();
        }
        
        /// <summary>
        /// Same as <see cref="RayManager.DrawRays()"/>, with the additions for samples
        /// </summary>
        protected override void DrawRays()
        {
            base.DrawRays();
            DrawSamplesInstant();
        }
        
        /// <summary>
        /// Draw all samples in instantly
        /// </summary>
        private void DrawSamplesInstant()
        {
            if (hasSelectedRay)
                DrawSamplesInstant(selectedRay);
            else
            {
                foreach (TreeNode<RTRay> ray in rays)
                    DrawSamplesInstant(ray);
            }
            
        }
        
        /// <summary>
        /// Draw all samples in the given ray instantly
        /// </summary>
        /// <param name="rtRay">The ray for which to draw the samples</param>
        private void DrawSamplesInstant(TreeNode<RTRay> rtRay)
        {
            foreach (TreeNode<RTRay> child in rtRay.Children) // Skip 0 length base ray
                DrawSamplesInstant(child);
            RCRay rcRay = rtRay.Data as RCRay;
            foreach (Sample sample in rcRay.Samples)
                DrawSample(sample);
        }
        
        /// <summary>
        /// Draw the given sample
        /// </summary>
        /// <param name="sample">The sample to draw</param>
        private void DrawSample(Sample sample)
        {
            SampleObject sampleObject = sampleObjectPool.GetSampleObject(sample.ObjectPoolIndex);
            sampleObject.Draw(ShowOpacity);
        }
        
        /// <summary>
        /// The same functionalty <see cref="RayManager.DrawRaysAnimated()"/>, but as the samples need to be drawn
        /// with the rays, this functions implementation is more elaborate
        /// </summary>
        protected override void DrawRaysAnimated()
        {
            // Reset the animation if we are looping or if a reset was requested.
            if ((animationDone && Loop) || Reset)
            {
                rayObjectPool.DeactivateAll();
                sampleObjectPool.DeactivateAll();
                distanceToDraw = 0.0f;
                rayTreeToDraw = 0;
                animationDone = false;
                Reset = false;
            }

            // Animate all ray trees if we are not done animating already.
            if (!animationDone)
            {
                distanceToDraw += Speed * Time.deltaTime;
                animationDone = true; // Will be reset to false if one tree is not finished animating.
                // If we have selected a ray we only draw its ray tree.
                if (hasSelectedRay)
                {
                    int width = rtSceneManager.Scene.Camera.ScreenWidth;
                    int index = selectedRayCoordinates.x + width * selectedRayCoordinates.y;
                    selectedRay = rays[index];
                    foreach (var ray in selectedRay.Children) // Skip the zero-length base-ray 
                        animationDone &= DrawRayTreeAnimated(ray, distanceToDraw);
                }
                // If specified we animate the ray trees sequentially (pixel by pixel).
                else if (animateSequentially)
                {
                    // Draw all previous ray trees in full.
                    for (int i = 0; i < rayTreeToDraw; ++i)
                        foreach (TreeNode<RTRay> ray in rays[i].Children) // Skip the zero-length base-ray 
                        {
                            DrawRayTree(ray);
                            DrawSamplesInstant(ray);
                        }
                            

                    // Animate the current ray tree. If it is now fully drawn we move on to the next one.
                    bool treeDone = true;
                    foreach(var ray in rays[rayTreeToDraw].Children) // Skip the zero-length base-ray 
                        treeDone &= DrawRayTreeAnimated(ray, distanceToDraw);

                    if (treeDone)
                    {
                        distanceToDraw = 0.0f;
                        ++rayTreeToDraw;
                    }

                    animationDone = treeDone && rayTreeToDraw >= rays.Count;
                }
                else
                {
                    // we animate all ray trees.
                    foreach (var pixel in rays)
                    foreach (var rayTree in pixel.Children) // Skip the zero-length base-ray 
                        animationDone &= DrawRayTreeAnimated(rayTree, distanceToDraw);
                }
            }
            // Otherwise we can just draw all rays in full.
            else
                DrawRays();
        }

        /// <summary>
        /// The same functionalty <see cref="RayManager.DrawRayTreeAnimated()"/>, but as the samples need to be drawn
        /// with the rays, this functions implementation is more elaborate
        /// </summary>
        /// <param name="rayTree">The ray tree to draw</param>
        /// <param name="distance">The maximum distance the ray tree will be drawn</param>
        /// <returns></returns>
        protected override bool DrawRayTreeAnimated(TreeNode<RTRay> rayTree, float distance)
        {
            RCRay rcRay = rayTree.Data as RCRay;
            if ((HideNoHitRays && rayTree.Data.Type == RTRay.RayType.NoHit) ||
                (HideNegligibleRays && rayTree.Data.Contribution < rayHideThreshold))
            {
                HideRays(rayTree);
                return true;
            }

            RayObject rayObject = rayObjectPool.GetRayObject(rayTree.Data.ObjectPoolIndex, rayTree.Data.AreaRay);
            rayObject.Draw(GetRayRadius(rayTree), distance);

            float leftover = distance - rayObject.DrawLength;
            // If this is the ray section in the voxel grid we need to draw samples
            if (rcRay.Samples.Count > 0)
            {
                // We check which of the samples need to be drawn
                int lastSampleToDraw = (int)(distance / unityRayCaster.DistanceBetweenSamples);
                // last sample to draw cannot be less than 0:
                lastSampleToDraw = lastSampleToDraw < 0 ? 0 : lastSampleToDraw;
                // last sample cannot be more than the amount of samples
                lastSampleToDraw = lastSampleToDraw <= rcRay.Samples.Count ? lastSampleToDraw : rcRay.Samples.Count; for (int i = rcRay.LastSampleDrawn; i < lastSampleToDraw; i++)
                    DrawSample(rcRay.Samples[i]);
            }
            

            // If this ray is not at its full length we are not done animating.
            if (leftover <= 0.0f)
                return false;
            // If this ray is at its full length and has no children we are done animating.
            if (rayTree.IsLeaf())
                return true;

            // Otherwise we start animating the children.
            bool done = true;
            foreach (var child in rayTree.Children)
            {
                done &= DrawRayTreeAnimated(child, leftover);
            }
            return done;
        }

        /// <summary>
        /// Get the ray radius of a RCRay hiding as an RTRay
        /// </summary>
        /// <param name="rayTree"></param>
        /// <returns></returns>
        protected override float GetRayRadius(TreeNode<RTRay> rayTree)
        {
            return (rayTree.Data as RCRay).Radius;
        }
        
        public override Material GetRayMaterial(float contribution, RTRay.RayType type, Color color, bool areaLight)
        {
            if (!showOpacity)
                color.a = 1;
            return new Material(rayCastingMaterial) { color = color };
        }
    }
}
