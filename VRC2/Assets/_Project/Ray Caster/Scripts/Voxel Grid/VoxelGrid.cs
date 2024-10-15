using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using _Project.Ray_Caster.Scripts.RC_Ray;
using TMPro;
using UnityEngine;

namespace _Project.Ray_Caster.Scripts.Voxel_Grid
{
    /// <summary>
    /// Class tied to the voxel grid prefab. It hold the voxel grid data and performs actions related to the voxel grid
    /// </summary>
    public class VoxelGrid : MonoBehaviour
    {
        private static VoxelGrid instance = null;
        private RayCasterManager rayCasterManager = null;
        
        /// <summary>
        /// The grid containing the selected voxelgrids scalar data
        /// </summary>
        public double[,,] Grid;
        /// <summary>
        /// Size of the voxel grid
        /// </summary>
        public int SizeX = 100;
        public int SizeY = 100;
        public int SizeZ = 100;
        
        /// <summary>
        /// The recommended transfer function for the selected voxel grid
        /// </summary>
        public RCRay.ColorTableEntry[] RecommendedColorLookupTable;

        /// <summary>
        /// UI elements used to display the progress of loading a new voxel grid
        /// </summary>
        [SerializeField] private GameObject loadVoxelGridProgressFill;
        [SerializeField] private TextMeshProUGUI loadVoxelGridtaskProgress;
        [SerializeField] private VoxelGridType selectedVoxelGrid;
        [SerializeField] private GameObject progressBar;

        /// <summary>
        /// The selected voxel grid type
        /// </summary>
        public VoxelGridType SelectedVoxelGrid
        {
            get { return selectedVoxelGrid; }
            set
            {
                if (value == selectedVoxelGrid) return;
                selectedVoxelGrid = value;
            }
        }
        
        // we remember previously generated voxel grids to speed up the game when the user is switching between grids
        private double[,,] buckyGrid = null;
        private double[,,] bunnyGrid = null;
        private double[,,] engineGrid = null;
        private double[,,] hazelnutGrid = null;
        
        /// <summary>
        /// The available voxel grid types
        /// </summary>
        public enum VoxelGridType
        {
            Bucky,
            Bunny,
            Engine,
            Hazelnut
        }

        /// <summary>
        /// Get the recommended color lookup table for the selected voxel grid
        /// </summary>
        /// <param name="type">The voxel grid type to get the recommended color lookup table for</param>
        /// <returns>The recommended color lookup table</returns>
        private RCRay.ColorTableEntry[] getReccomendedColorLookupTable(VoxelGridType type)
        {
            RecommendedColorLookupTable = new RCRay.ColorTableEntry[5];
            switch (type)
            {
                case VoxelGridType.Bucky:
                    RecommendedColorLookupTable[0].Density = 0f;
                    RecommendedColorLookupTable[1].Density = 0f;
                    RecommendedColorLookupTable[2].Density = 0f;
                    RecommendedColorLookupTable[3].Density = 0f;
                    RecommendedColorLookupTable[4].Density = 1.0f;
                    RecommendedColorLookupTable[0].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[1].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[2].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[3].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[4].ColorAlpha = new Color(1, 1, 1, 0.3f);
                    return RecommendedColorLookupTable;
                case VoxelGridType.Bunny:
                    RecommendedColorLookupTable[0].Density = 0f;
                    RecommendedColorLookupTable[1].Density = 0f;
                    RecommendedColorLookupTable[2].Density = 0f;
                    RecommendedColorLookupTable[3].Density = 0.568f;
                    RecommendedColorLookupTable[4].Density = 1.0f;
                    RecommendedColorLookupTable[0].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[1].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[2].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[3].ColorAlpha = new Color(0, 1, 0, 0.3f);
                    RecommendedColorLookupTable[4].ColorAlpha = Color.clear;
                    return RecommendedColorLookupTable;
                case VoxelGridType.Hazelnut:
                    RecommendedColorLookupTable[0].Density = 0f;
                    RecommendedColorLookupTable[1].Density = 0f;
                    RecommendedColorLookupTable[2].Density = 0.353f;
                    RecommendedColorLookupTable[3].Density = 0.435f;
                    RecommendedColorLookupTable[4].Density = 1.0f;
                    RecommendedColorLookupTable[0].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[1].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[2].ColorAlpha = new Color(0, 1, 0, 0.5f);
                    RecommendedColorLookupTable[3].ColorAlpha = new Color(1, 1, 0, 0.7f);
                    RecommendedColorLookupTable[4].ColorAlpha = Color.clear;
                    return RecommendedColorLookupTable;
                case VoxelGridType.Engine:
                    RecommendedColorLookupTable[0].Density = 0f;
                    RecommendedColorLookupTable[1].Density = 0f;
                    RecommendedColorLookupTable[2].Density = 0f;
                    RecommendedColorLookupTable[3].Density = 0.556f;
                    RecommendedColorLookupTable[4].Density = 1.0f;
                    RecommendedColorLookupTable[0].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[1].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[2].ColorAlpha = Color.clear;
                    RecommendedColorLookupTable[3].ColorAlpha = new Color(1, 0, 0, 0.3f);
                    RecommendedColorLookupTable[4].ColorAlpha = new Color(0, 0, 1, 0.7f);
                    return RecommendedColorLookupTable;
            }
            return null;
        }

        /// <summary>
        /// Set the rotation of the voxel grid to the recommended rotation
        /// </summary>
        /// <param name="type">The voxel grid type to set the rotation for</param>
        private void setReccommendedRotation(VoxelGridType type)
        {
            switch (type)
            {
                case VoxelGridType.Bucky:
                    Rotation = new Vector3(0, 0, 0);
                    break;
                case VoxelGridType.Bunny:
                    Rotation = new Vector3(90, 180, 180); // the bunny is upside down by default. so we set it upright
                    break;
                case VoxelGridType.Engine:
                    Rotation = new Vector3(0, 0, 0);
                    break;
                case VoxelGridType.Hazelnut:
                    Rotation = new Vector3(0, 0, 0);
                    break;
            }
        }
        
        /// <summary>
        /// Whether the selected voxel grid is fully loaded
        /// </summary>
        /// <returns>true if fully loaded, otherwise false</returns>
        public bool SelectedVoxelGridDoneLoading()
        {
            switch (selectedVoxelGrid)
            {
                case VoxelGridType.Bucky:
                    return buckyGrid != null;
                case VoxelGridType.Bunny:
                    return bunnyGrid != null;
                case VoxelGridType.Engine:
                    return engineGrid != null;
                case VoxelGridType.Hazelnut:
                    return hazelnutGrid != null;
            }
            return false;
        }
        
        /// <summary>
        /// Set the dimensions for the selected voxel grid
        /// </summary>
        /// <param name="type">Voxel grid type to set dimensions for</param>
        private void setGridSizes(VoxelGridType type)
        {
            switch (type)
            {
                case VoxelGridType.Bucky:
                    SizeX = 32;
                    SizeY = 32;
                    SizeZ = 32;
                    return;
                case VoxelGridType.Bunny:
                    SizeX = 512;
                    SizeY = 512;
                    SizeZ = 361;
                    return;
                case VoxelGridType.Hazelnut:
                    SizeX = 256;
                    SizeY = 256;
                    SizeZ = 256;
                    return;
                case VoxelGridType.Engine:
                    SizeX = 256;
                    SizeY = 256;
                    SizeZ = 256;
                    return;
            }
        }

        /// <summary>
        /// Try to load a voxel grid using the saved data
        /// </summary>
        /// <param name="type">The voxel grid type to load the grid for</param>
        /// <returns>true if successfully loaded with saved data, otherwise false</returns>
        private bool loadKnownGrid(VoxelGridType type)
        {
            // If we have loaded this grid before we used the remembered grid
            switch (type)
            {
                case VoxelGridType.Bucky:
                    if (buckyGrid != null)
                    {
                        Grid = buckyGrid;
                        if (rayCasterManager != null)
                            rayCasterManager.ColorLookupTable = RecommendedColorLookupTable;
                        return true;
                    }
                    break;
                case VoxelGridType.Bunny:
                    if (bunnyGrid != null)
                    {
                        Grid = bunnyGrid;
                        if (rayCasterManager != null)
                            rayCasterManager.ColorLookupTable = RecommendedColorLookupTable;
                        return true;
                    }
                    break;
                case VoxelGridType.Engine:
                    if (engineGrid != null)
                    {
                        Grid = engineGrid;
                        if (rayCasterManager != null)
                            rayCasterManager.ColorLookupTable = RecommendedColorLookupTable;
                        return true;
                    }
                    break;
                case VoxelGridType.Hazelnut:
                    if (hazelnutGrid != null)
                    {
                        Grid = hazelnutGrid;
                        if (rayCasterManager != null)
                            rayCasterManager.ColorLookupTable = RecommendedColorLookupTable;
                        return true;
                    }
                    break;
            }
            return false;
        }

        /// <summary>
        /// Load a voxel grid for the first time from the raw file
        /// </summary>
        /// <param name="type">The voxel grid type to load</param>
        private IEnumerator loadNewGrid(VoxelGridType type)
        {
            // If we don't have a remembered grid we load the grid
            // We load the path to the selected voxel grid
            string path = "/";
            switch (type)
            {
                case VoxelGridType.Bucky:
                    path += "bucky32x32x32.raw";
                    break;
                case VoxelGridType.Bunny:
                    path += "bunny512x512x361.raw";
                    break;
                case VoxelGridType.Hazelnut:
                    path += "hnut256_uint.raw";
                    break;
                case VoxelGridType.Engine:
                    path += "engine256x256x256.raw";
                    break;
            }
            
            // And read the binary file
            BinaryReader binReader = new BinaryReader(File.Open((Application.streamingAssetsPath + path), FileMode.Open));
            Grid = new double[SizeX, SizeY, SizeZ];
            for (int z = 0; z < SizeZ; z++)
            {
                int percentage = (int) Math.Round(((float)  z) / ((float) SizeZ) * 100f);
                loadVoxelGridProgressFill.GetComponent<RectTransform>().sizeDelta = 
                    new Vector2(progressBar.GetComponent<RectTransform>().rect.width / 100 * percentage, 0);
                loadVoxelGridtaskProgress.text = percentage.ToString() + "%";
                yield return null; // yield to update UI
                for (int y = 0; y < SizeY; y++)
                {
                    for (int x = 0; x < SizeX; x++)
                    {
                        Grid[x, y, z] = binReader.ReadByte();
                        Grid[x, y, z] /= 255;
                    }
                }
            }
            binReader.Close();
            
            // And then we remember it so we can re-use it next time
            switch (type)
            {
                case VoxelGridType.Bucky:
                    buckyGrid = Grid;
                    break;
                case VoxelGridType.Bunny:
                    bunnyGrid = Grid;
                    break;
                case VoxelGridType.Engine:
                    engineGrid = Grid;
                    break;
                case VoxelGridType.Hazelnut:
                    hazelnutGrid = Grid;
                    break;
            }

            yield return null;
        }

        /// <summary>
        /// Load a voxel grids data
        /// </summary>
        /// <param name="type">Voxel grid to load data for</param>
        private IEnumerator loadGrid(VoxelGridType type)
        {
            if (!loadKnownGrid(type))
            {
                yield return loadNewGrid(type);
            }
            //change material texture accordingly
            Renderer ren = this.GetComponent<Renderer>();
            Texture tex;
            switch (type)
            {
                //set path of voxel grid and 3d texture for preview
                case VoxelGridType.Bucky:
                    tex = Resources.Load<Texture>("3DTex/BuckyTex");
                    ren.material.SetTexture("_MainTex",tex);
                    break;
                case VoxelGridType.Bunny:
                    tex = Resources.Load<Texture>("3DTex/BunnyTex");
                    ren.material.SetTexture("_MainTex",tex);
                    break;
                case VoxelGridType.Hazelnut:
                    tex = Resources.Load<Texture>("3DTex/HazelTex");
                    ren.material.SetTexture("_MainTex",tex);
                    break;
                case VoxelGridType.Engine:
                    tex = Resources.Load<Texture>("3DTex/EngineTex");
                    ren.material.SetTexture("_MainTex",tex);
                    break;
            }
        }

        /// <summary>
        /// Set a new voxel grid type
        /// </summary>
        /// <param name="type">The new voxel grid type</param>
        public IEnumerator setVoxelGrid(VoxelGridType type)
        {
            RecommendedColorLookupTable = getReccomendedColorLookupTable(type);
            setReccommendedRotation(type);
            setGridSizes(type);
            yield return loadGrid(type);
            if (rayCasterManager != null)
                rayCasterManager.ColorLookupTable = RecommendedColorLookupTable;
        }
        
        /// <summary>
        /// The position of the visualized box's mesh
        /// </summary>
        public Vector3 Position
        {
            get => transform.position;
            set
            {
                if (value == transform.position) return;
                transform.position = value;
            }
        }

        /// <summary>
        /// The rotation of the visualized box's mesh
        /// </summary>
        public Vector3 Rotation
        {
            get => transform.eulerAngles;
            set
            {
                if (value == transform.eulerAngles) return;
                transform.eulerAngles = value;
            }
        }

        /// <summary>
        /// The scale of the visualized box's mesh
        /// </summary>
        public Vector3 Scale
        {
            get => transform.localScale;
            set
            {
                if (value == transform.localScale) return;
                transform.localScale = value;
            }
        }
        
        void Awake()
        {
            instance = this;
        }

        void Start()
        {
            rayCasterManager = RayCasterManager.Get();
        }

        public static VoxelGrid Get()
        {
            return instance;
        }
    }
}

