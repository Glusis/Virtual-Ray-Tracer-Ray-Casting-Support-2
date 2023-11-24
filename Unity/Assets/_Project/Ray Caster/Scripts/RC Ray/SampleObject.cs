using UnityEngine;

namespace _Project.Ray_Caster.Scripts.RC_Ray
{
    /// <summary>
    /// A Unity object that visually represent a sample of a ray from the ray caster.
    /// </summary>
    [RequireComponent(typeof(SampleRenderer))]
    public class SampleObject : MonoBehaviour
    {
        /// <summary>
        /// used to draw the sample
        /// </summary>
        private SampleRenderer sampleRenderer;
        /// <summary>
        /// contains all the data
        /// </summary>
        public Sample sample;
        
        /// <summary>
        /// Draw this sample
        /// </summary>
        /// <param name="showOpacity"></param>
        public void Draw(bool showOpacity)
        {
            sampleRenderer.Radius = sample.Radius;
            if (!showOpacity)
                sampleRenderer.MyColor = new Color(sample.SampleColor.r, sample.SampleColor.g, sample.SampleColor.b, 1);
            else
                sampleRenderer.MyColor = sample.SampleColor;
            sampleRenderer.Origin = sample.UnityPosition;
        }

        private void Awake()
        {
            sampleRenderer = GetComponent<SampleRenderer>();
        }
    }
}