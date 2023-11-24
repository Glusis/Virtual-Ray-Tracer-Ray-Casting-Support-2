using System;
using _Project.Ray_Tracer.Scripts.Utility;
using System.Collections.Generic;
using _Project.Ray_Tracer.Scripts;
using _Project.Ray_Tracer.Scripts.RT_Ray;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Ray_Caster.Scripts.RC_Ray
{
    /// <summary>
    /// A simple class used to pool <see cref="SampleObject"/>s for drawing by the <see cref="RayCasterManager"/>. For more
    /// information on object pooling in Unity see: https://learn.unity.com/tutorial/introduction-to-object-pooling.
    /// </summary>
    public class SampleObjectPool
    {
        /// <summary>
        /// List of <see cref="sampleObject"/>s
        /// </summary>
        private readonly List<SampleObject> sampleObjects;
        /// <summary>
        /// The prefab used to create visualizable samples
        /// </summary>
        private readonly SampleObject samplePrefab;
        /// <summary>
        /// The parent transform for the samples
        /// </summary>
        private readonly Transform parent;
        /// <summary>
        /// Keeps track of which samples are currently active and visible
        /// </summary>
        private int nextIndex;

        /// <summary>
        /// Construct a new pool of <see cref="SampleObject"/>s. All instantiated objects start inactive.
        /// </summary>
        /// <param name="samplePrefab"> The <see cref="SampleObject"/> prefab to be instantiated by this pool. </param>
        /// <param name="initialAmount"> The initial amount of <see cref="SampleObject"/>s to instantiate. </param>
        /// <param name="parent"> The parent object of all <see cref="SampleObject"/>s instantiated by this pool. </param>
        public SampleObjectPool(SampleObject samplePrefab, int initialAmount, Transform parent)
        {
            this.samplePrefab = samplePrefab;
            this.parent = parent;

            sampleObjects = new List<SampleObject>(initialAmount);
            nextIndex = 0;
        }

        /// <summary>
        /// Deactivate all <see cref="SampleObject"/>s in this pool. This also marks the objects as unused.
        /// </summary>
        public void DeactivateAll()
        {
            sampleObjects.ForEach(sampleObject => sampleObject.gameObject.SetActive(false));
            nextIndex = 0;
        }

        /// <summary>
        /// Destroy all unused <see cref="SampleObject"/>s in this pool.
        /// </summary>
        private void CleanUp()
        {
            for (int i = sampleObjects.Count - 1; i >= nextIndex; --i)
                DestroyObject(sampleObjects[i].gameObject);
            sampleObjects.RemoveRange(nextIndex, sampleObjects.Count - nextIndex);
        }

        private void DestroyObject(GameObject obj)
        {
#if UNITY_EDITOR
            Object.DestroyImmediate(obj);
#else
            Object.Destroy(obj);
#endif
        }

        /// <summary>
        /// Mark all <see cref="SampleObject"/>s in this pool unused. This does not mean they are deactivated, but they
        /// will be returned by <see cref="GetSampleObject"/>. The intended usage is to first call this function, then make
        /// all objects needed using <see cref="MakeSampleObjects"/> and finally deactivate all unused objects left active
        /// by calling <see cref="CleanUp"/>.
        /// </summary>
        private void SetAllUnused()
        {
            nextIndex = 0;
        }
        
        /// <summary>
        /// Make <see cref="SampleObject"/>s for the given list of ray casting ray trees
        /// </summary>
        /// <param name="rcRays"> the rcrays whose samples need sample objects</param>
        public void MakeSampleObjects(List<TreeNode<RTRay>> rcRays)
        {
            DeactivateAll();
            SetAllUnused(); // Mark all rays as unused; Start all over

            foreach (TreeNode<RTRay> treeNode in rcRays)
            foreach (TreeNode<RTRay> rtRay in treeNode.Children) // Skip 0 length base ray
            {
                MakeSampleObjects(rtRay);
            }
            CleanUp();      // Remove any rayobjects that are no longer necessary
        }

        /// <summary>
        /// Make <see cref="SampleObject"/> for the given ray casting ray tree
        /// </summary>
        /// <param name="rayTree"> the ray tree containing the samples that need sample objects </param>
        private void MakeSampleObjects(TreeNode<RTRay> rayTree)
        {
            if (!rayTree.IsLeaf())
                foreach (TreeNode<RTRay> child in rayTree.Children)
                    MakeSampleObjects(child);
            RCRay rcRay = rayTree.Data as RCRay;
            foreach (Sample sample in rcRay.Samples)
            {
                sample.ObjectPoolIndex = MakeSampleObject(sample);
            }
        }

        /// <summary>
        /// Get an unused <see cref="SampleObject"/> from the pool and, if necessary, activate it. If there are no unused
        /// objects in the pool a new one will be instantiated and returned.
        /// </summary>
        /// <returns> An unused activated <see cref="SampleObject"/> from the pool. </returns>
        private int MakeSampleObject(Sample sample)
        {
            MakeSampleObject();
            sampleObjects[nextIndex].sample = sample;
            sampleObjects[nextIndex].gameObject.SetActive(false);
            return nextIndex++;
        }

        /// <summary>
        /// If there are no unused <see cref="SampleObject"/>s, create a new one and add it to the pool
        /// </summary>
        private void MakeSampleObject()
        {
            // First we check if an unused sample object already exists.
            if (nextIndex < sampleObjects.Count) return;
            
            // Else we add a new object to the pool.
            sampleObjects.Add(Object.Instantiate(samplePrefab, parent));
        }

        /// <summary>
        /// Get a <see cref="SampleObject"/> from the object pool
        /// </summary>
        /// <param name="index">Index of sample object to be returned</param>
        /// <returns></returns>
        public SampleObject GetSampleObject(int index)
        {
            sampleObjects[index].gameObject.SetActive(true);
            return sampleObjects[index];
        }
    }
}
