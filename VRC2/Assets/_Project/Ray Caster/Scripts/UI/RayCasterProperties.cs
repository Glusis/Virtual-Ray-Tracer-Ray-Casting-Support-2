using System;
using System.Collections;
using _Project.Ray_Caster.Scripts;
using _Project.Ray_Caster.Scripts.RC_Ray;
using _Project.Ray_Caster.Scripts.Voxel_Grid;
using _Project.Scripts;
using _Project.UI.Scripts.Control_Panel;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI for the ray caster
/// </summary>
public class RayCasterProperties : RayTracerProperties
{
    private VoxelGrid voxelGrid;
    private RayCasterManager rayCasterManager;
    private UnityRayCaster unityRayCaster;
    private Renderer ren;
    private float unitsPerVoxel;
    
    /// <summary>
    /// Toggle GPU preview
    /// </summary>
    [SerializeField] private BoolEdit showPreview;
    /// <summary>
    /// A gameobject that has a loading bar, displayed when a voxel is loading
    /// </summary>
    [SerializeField] private GameObject loadVoxelGridWindow;
    /// <summary>
    /// Allow users to edit <see cref="UnityRayCaster.DistanceBetweenSamples"/>
    /// </summary>
    [SerializeField] private FloatEdit distanceBetweenSamplesEdit;
    /// <summary>
    /// Allow users to edit <see cref="VoxelGrid.selectedVoxelGrid"/>
    /// </summary>
    [SerializeField] private TMP_Dropdown VoxelGridDropdown;
    /// <summary>
    /// Allow users to edit <see cref="RCRay.ColorCompositingMethod"/>
    /// </summary>
    [SerializeField] private TMP_Dropdown CompositingMethodDropdown;
    /// <summary>
    /// Allow users to edit <see cref="RayCasterManager.opacityCutoffValue"/>
    /// </summary>
    [SerializeField] private FloatEdit opacityCutoffValueEdit;
    /// <summary>
    /// Allow users to edit <see cref="RCRay.densityMatchingValue"/>
    /// </summary>
    [SerializeField] private FloatEdit matchingDensityEdit;
    /// <summary>
    /// Allow users to edit <see cref="RayCasterManager.ShowOpacity"/>
    /// </summary>
    [SerializeField] private BoolEdit showOpacity;
    /// <summary>
    /// Allow users to edit <see cref="UnityRayCaster.DoRayTermination"/>
    /// </summary>
    [SerializeField] private BoolEdit doRayTermination;

    /// <summary>
    /// Allow users to edit the <see cref="RayCasterManager.ColorLookupTable"/>
    /// </summary>
    [SerializeField] private FloatEdit density1;
    [SerializeField] private ColorEdit color1;
    [SerializeField] private FloatEdit density2;
    [SerializeField] private ColorEdit color2;
    [SerializeField] private FloatEdit density3;
    [SerializeField] private ColorEdit color3;
    [SerializeField] private FloatEdit density4;
    [SerializeField] private ColorEdit color4;
    [SerializeField] private FloatEdit density5;
    [SerializeField] private ColorEdit color5;

    /// <summary>
    /// The gameobject on which the histogram image of the voxel grid is displayed
    /// </summary>
    [SerializeField] private GameObject histogramImage;
    /// <summary>
    /// The 4 histograms for the 4 available voxel grids
    /// </summary>
    [SerializeField] private Sprite buckyHistogram;
    [SerializeField] private Sprite bunnyHistogram;
    [SerializeField] private Sprite engineHistogram;
    [SerializeField] private Sprite hazelnutHistogram;

    /// <summary>
    /// Shows the ray caster control panel
    /// </summary>
    public override void Show()
    {
        base.Show();
        voxelGrid = VoxelGrid.Get();
        setVoxelDens(voxelGrid.SelectedVoxelGrid);
        rayCasterManager = RayCasterManager.Get();
        ren = voxelGrid.GetComponent<Renderer>();
        unityRayCaster = (rayTracer as UnityRayCaster);
        showPreview.IsOn = rayCasterManager.ShowPreview;
        distanceBetweenSamplesEdit.Value = unityRayCaster.DistanceBetweenSamples / unitsPerVoxel;
        VoxelGridDropdown.value = VoxelGridDropdown.options.FindIndex(option => option.text == voxelGrid.SelectedVoxelGrid.ToString());
        CompositingMethodDropdown.value = CompositingMethodDropdown.options.FindIndex(option => option.text == rayCasterManager.CompositingMethod.ToString());
        CompositingMethodChanged(rayCasterManager.CompositingMethod);
        opacityCutoffValueEdit.Value = rayCasterManager.OpacityCutoffValue;
        matchingDensityEdit.Value = rayCasterManager.MatchingDensityValue;
        showOpacity.IsOn = rayCasterManager.ShowOpacity;
        doRayTermination.IsOn = unityRayCaster.DoRayTermination;
    }
    
    protected override void Awake()
    {
        base.Awake();
        // We need to remove the listeners from the flyRoRTCameraButton because it currently disables the rays
        // Without having an option to re-enable them
        flyRoRTCameraButton.onClick.RemoveAllListeners();
        // Then we add back the functionality that we do want
        
        showPreview.OnValueChanged.AddListener((value) =>
        {
            rayCasterManager.ShowPreview = value;
            Shader raycast = Shader.Find("Unlit/Raycast");
            Shader plain = Shader.Find("Standard");
            if (value)
            {
                ren.material.shader = raycast;
            }
            else
            {
                ren.material.shader = plain;
            }
        });
        
        flyRoRTCameraButton.onClick.AddListener(() =>
        { 
            FindObjectOfType<CameraController>().FlyToRTCamera(); // There should only be 1 CamerController.
        });
        
        distanceBetweenSamplesEdit.OnValueChanged.AddListener((value) =>
        {
            // float steps =  value * unitsPerVoxel;
            
            
            unityRayCaster.DistanceBetweenSamples = value*unitsPerVoxel;
            ren.material.SetFloat("_StepSize", value*unitsPerVoxel);
        });
        VoxelGridDropdown.onValueChanged.AddListener( type => StartCoroutine(VoxelGridChanged((VoxelGrid.VoxelGridType) type)));
        CompositingMethodDropdown.onValueChanged.AddListener( type =>
        {
            ren.material.SetInt("_CompositingFunction", type);
            CompositingMethodChanged((RCRay.CompositingMethodType)type);
        });
        opacityCutoffValueEdit.OnValueChanged.AddListener(value =>
        {
            ren.material.SetFloat("_AlphaCutoff", value);
            rayCasterManager.OpacityCutoffValue = value;
        });
        matchingDensityEdit.OnValueChanged.AddListener(value =>
        {
            ren.material.SetFloat("_TargetDens", value);
            rayCasterManager.MatchingDensityValue = value;
        });
        showOpacity.OnValueChanged.AddListener(value => rayCasterManager.ShowOpacity = value);
        doRayTermination.OnValueChanged.AddListener(value => unityRayCaster.DoRayTermination = value);
        
        density1.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        density2.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        density3.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        density4.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        density5.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        
        color1.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        color2.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        color3.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        color4.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
        color5.OnValueChanged.AddListener(value => rayCasterManager.ColorLookupTable = colorLookupTableChanged());
    }
    
    /// <summary>
    /// Get the changed color lookup table when a value in the transfer function is changed
    /// </summary>
    /// <returns>The updated color lookup table</returns>
    private RCRay.ColorTableEntry[] colorLookupTableChanged()
    {
        // First we make sure that the user can only have their densities in ascending order
        // We do this by setting the min value of a density to the value of the previous density
        if (density2.Value < density1.Value)
            density2.Value = density1.Value;
        if (density3.Value < density2.Value)
            density3.Value = density2.Value;
        if (density4.Value < density3.Value)
            density4.Value = density3.Value;
        if (density5.Value < density4.Value)
            density5.Value = density4.Value;
        density2.MinValue = density1.Value;
        density3.MinValue = density2.Value;
        density4.MinValue = density3.Value;
        density5.MinValue = density4.Value;
        // Then we construct the new color lookup table for the ray manager to receiver
        RCRay.ColorTableEntry[] result = new RCRay.ColorTableEntry[5];
        result[0] = new RCRay.ColorTableEntry(color1.Color, density1.Value);
        result[1] = new RCRay.ColorTableEntry(color2.Color, density2.Value);
        result[2] = new RCRay.ColorTableEntry(color3.Color, density3.Value);
        result[3] = new RCRay.ColorTableEntry(color4.Color, density4.Value);
        result[4] = new RCRay.ColorTableEntry(color5.Color, density5.Value);
        
        //update preview transfer function
        ren.material.SetFloat("_Transfer1",density1.Value);
        ren.material.SetFloat("_Transfer2",density2.Value);
        ren.material.SetFloat("_Transfer3",density3.Value);
        ren.material.SetFloat("_Transfer4",density4.Value);
        ren.material.SetFloat("_Transfer5",density5.Value);
        ren.material.SetColor("_Transfer1c",color1.Color);
        ren.material.SetColor("_Transfer2c",color2.Color);
        ren.material.SetColor("_Transfer3c",color3.Color);
        ren.material.SetColor("_Transfer4c",color4.Color);
        ren.material.SetColor("_Transfer5c",color5.Color);
        
        return result;
    }

    /// <summary>
    /// Perform the multiple actions needed when the voxel grid is changed
    /// </summary>
    /// <param name="type">New selected voxel grid</param>
    private IEnumerator VoxelGridChanged(VoxelGrid.VoxelGridType type)
    {
        loadVoxelGridWindow.SetActive(true);
        yield return new WaitForFixedUpdate();
        setHistogramImage(type);
        voxelGrid.SelectedVoxelGrid = type;
        yield return voxelGrid.setVoxelGrid(type);
        yield return new WaitForFixedUpdate();
        loadVoxelGridWindow.SetActive(false);
        yield return new WaitForFixedUpdate();
        setDisplayedColorLookupTalbe(voxelGrid.RecommendedColorLookupTable);
        setVoxelDens(type);
    }
    
    private void setVoxelDens(VoxelGrid.VoxelGridType type)
    {
        switch (type)
        {
            case VoxelGrid.VoxelGridType.Bucky:
                unitsPerVoxel = 3.0f/32;
                break;
            case VoxelGrid.VoxelGridType.Bunny:
                unitsPerVoxel = 3.0f/512;
                break;
            case VoxelGrid.VoxelGridType.Engine:
                unitsPerVoxel = 3.0f/256;
                break;
            case VoxelGrid.VoxelGridType.Hazelnut:
                unitsPerVoxel = 3.0f/256;
                break;
        }
    }

    /// <summary>
    /// Perform the multiple actions needed when the compositing method is changed
    /// </summary>
    /// <param name="type">New selected compositing method</param>
    private void CompositingMethodChanged(RCRay.CompositingMethodType type)
    {
        switch (type)
        {
            case RCRay.CompositingMethodType.Accumulate:
                opacityCutoffValueEdit.Interactable = true;
                matchingDensityEdit.Interactable = false;
                break;
            case RCRay.CompositingMethodType.Average:
                opacityCutoffValueEdit.Interactable = false;
                matchingDensityEdit.Interactable = false;
                break;
            case RCRay.CompositingMethodType.Maximum:
                opacityCutoffValueEdit.Interactable = false;
                matchingDensityEdit.Interactable = false;
                break;
            case RCRay.CompositingMethodType.First:
                opacityCutoffValueEdit.Interactable = false;
                matchingDensityEdit.Interactable = true;
                break;
        }
        rayCasterManager.CompositingMethod = type;
    }

    /// <summary>
    /// Set a new histogram image
    /// </summary>
    /// <param name="type">The type of voxel grid whose histogram should be displayed</param>
    private void setHistogramImage(VoxelGrid.VoxelGridType type)
    {
        switch (type)
        {
            case VoxelGrid.VoxelGridType.Bucky:
                histogramImage.GetComponent<Image>().sprite = buckyHistogram;
                break;
            case VoxelGrid.VoxelGridType.Bunny:
                histogramImage.GetComponent<Image>().sprite = bunnyHistogram;
                break;
            case VoxelGrid.VoxelGridType.Engine:
                histogramImage.GetComponent<Image>().sprite = engineHistogram;
                break;
            case VoxelGrid.VoxelGridType.Hazelnut:
                histogramImage.GetComponent<Image>().sprite = hazelnutHistogram;
                break;
        }
    }

    /// <summary>
    /// Update the UI for the color lookup table
    /// </summary>
    /// <param name="colorLookupTable">The color lookup table settings to display in the UI</param>
    private void setDisplayedColorLookupTalbe(RCRay.ColorTableEntry[] colorLookupTable)
    {
        density1.Value = colorLookupTable[0].Density;
        density2.Value = colorLookupTable[1].Density;
        density3.Value = colorLookupTable[2].Density;
        density4.Value = colorLookupTable[3].Density;
        density5.Value = colorLookupTable[4].Density;
        
        color1.Color = colorLookupTable[0].ColorAlpha;
        color2.Color = colorLookupTable[1].ColorAlpha;
        color3.Color = colorLookupTable[2].ColorAlpha;
        color4.Color = colorLookupTable[3].ColorAlpha;
        color5.Color = colorLookupTable[4].ColorAlpha;
    }
}
