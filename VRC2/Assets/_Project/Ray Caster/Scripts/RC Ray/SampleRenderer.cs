using UnityEngine;

namespace _Project.Ray_Caster.Scripts.RC_Ray
{
    /// <summary>
    /// Renders a sample from a ray casted ray as a sphere
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SampleRenderer : MonoBehaviour
    {
        private Vector3 origin;
        /// <summary>
        /// The origin for the sphere
        /// </summary>
        public Vector3 Origin
        {
            get { return origin; }
            set
            {
                // Because we often reset the origin to the same value this check improves performance
                if (origin == value)
                    return;

                origin = value;
                transform.position = origin;
            }
        }

        private float radius;
        /// <summary>
        /// The radius of the drawn sphere
        /// </summary>
        public float Radius
        {
            get { return radius; }
            set
            {
                // Because we often reset the radius to the same value this check improves performance
                if (radius == value)
                    return;

                radius = value;
                transform.localScale = new Vector3(radius, radius, radius);
            }
        }

        private Material material;
        /// <summary>
        /// The material used
        /// </summary>
        public Material Material
        {
            get { return material; }
            set
            {
                // Because we often reset the material to the same value this check improves performance
                if (material == value)
                    return;

                material = value;
                meshRenderer.material = material;
            }
        }
        
        private Color color;
        /// <summary>
        /// The color of the sphere
        /// </summary>
        public Color MyColor
        {
            get { return color; }
            set
            {
                // Because we often reset the material to the same value this check improves performance.
                if (color == value)
                    return;

                color = value;
                meshRenderer.material.color = color;
            }
        }

        private MeshRenderer meshRenderer;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }
    }
}
