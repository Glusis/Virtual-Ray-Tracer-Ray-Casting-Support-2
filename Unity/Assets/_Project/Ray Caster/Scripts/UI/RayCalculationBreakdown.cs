using System;
using System.Collections.Generic;
using System.Data.Common;
using _Project.Ray_Caster.Scripts;
using _Project.Ray_Caster.Scripts.RC_Ray;
using _Project.Ray_Tracer.Scripts;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Control the ray calculation window used to visualize the compositing method
/// </summary>
public class RayCalculationBreakdown : MonoBehaviour
{
    private RayCasterManager rayCasterManager;
    private RTSceneManager rtSceneManager;
    private RCRay selectedRay;
    /// <summary>
    /// Whether the ray calculation window is visible
    /// </summary>
    private bool isDisplaying = true;

    /// <summary>
    /// The gameobject that needs to be deactivated when hiding the ray calculation window
    /// </summary>
    [SerializeField] private GameObject toHide;
    /// <summary>
    /// The column of the ray calculation table where the sample RGBA is displayed
    /// </summary>
    [SerializeField] private GameObject sampleColumn;
    /// <summary>
    /// The column of the ray calculation table where the composited color displayed
    /// </summary>
    [SerializeField] private GameObject compositedColumn;
    /// <summary>
    /// The column of the ray calculation table where the composited opacity is displayed
    /// </summary>
    [SerializeField] private GameObject opacityColumn;
    /// <summary>
    /// The column of the ray calculation table where the density is displayed
    /// </summary>
    [SerializeField] private GameObject denstiyColumn;
    /// <summary>
    /// The gameobject where the formula image is displayed
    /// </summary>
    [SerializeField] private GameObject formulaCell;
    /// <summary>
    /// The image for the accumulate compositing method
    /// </summary>
    [SerializeField] private Sprite AccumulateFormulas;
    /// <summary>
    /// The image for the average compositing method
    /// </summary>
    [SerializeField] private Sprite AverageFormulas;
    /// <summary>
    /// The image for the maximum compositing method
    /// </summary>
    [SerializeField] private Sprite MaximumFormulas;
    /// <summary>
    /// The image for the first compositing method
    /// </summary>
    [SerializeField] private Sprite FirstFormulas;
    
    void Start()
    {
        rayCasterManager = RayCasterManager.Get();
        rtSceneManager = RTSceneManager.Get();
    }
    
    void FixedUpdate()
    {
        if (rayCasterManager.hasSelectedRay)
        {
            if (!isDisplaying)
            {
                startDisplaying();
            }
            int width = rtSceneManager.Scene.Camera.ScreenWidth;
            int index = rayCasterManager.selectedRayCoordinates.x + width * rayCasterManager.selectedRayCoordinates.y;
            // the first ray is the 0 length base ray. it's child is the pre-voxelgrid part. so the "granchild" is the ray with samples in it
            RCRay ray = rayCasterManager.rays[index].Children[0].Children[0].Data as RCRay;
            updateTable(ray.Samples, ray.earlyRayActivationIndex);
        }
        else
        {
            if (isDisplaying)
                stopDisplaying();
        }
    }
    
    /// <summary>
    /// Stop displaying this calculation window
    /// </summary>
    private void stopDisplaying()
    {
        toHide.SetActive(false);
        isDisplaying = false;
    }

    /// <summary>
    /// Start displaying this calculation window
    /// </summary>
    private void startDisplaying()
    {
        toHide.SetActive(true);
        isDisplaying = true;
    }

    /// <summary>
    /// Update the table
    /// </summary>
    /// <param name="samples">The samples from which to take data to display</param>
    private void updateTable(List<Sample> samples, int guardActivationIndex)
    {
        // First we set the formulas for the selected compositing type
        Image formulasImage = formulaCell.GetComponent<Image>();
        formulasImage.sprite = formulas(rayCasterManager.CompositingMethod);
        
        // Then we fill in the calculation table
        updateColumn(getStrings(samples, guardActivationIndex, densityString), denstiyColumn);
        updateColumn(getStrings(samples, guardActivationIndex, sampleString), sampleColumn);
        updateColumn(getStrings(samples, guardActivationIndex, colorString), compositedColumn);
        updateColumn(getStrings(samples, guardActivationIndex, opacityString), opacityColumn);
    }

    /// <summary>
    /// Update a column in the table
    /// </summary>
    /// <param name="strings">The strings to display in the column</param>
    /// <param name="column">The column to update</param>
    private void updateColumn(List<string> strings, GameObject column)
    {
        // We go through all the strings and make sure that they are displayed in a cell
        for (int i = 0; i < strings.Count; i++)
        {
            GameObject currentCell;
            if (i + 2 < column.transform.childCount) // i+2 because the first 2 are header and base values
            {
                currentCell = column.transform.GetChild(i + 2).gameObject;
                currentCell.SetActive(true);
            }
            else
            {
                // there are not enough cells, so we create more
                currentCell = Instantiate(column.transform.GetChild(1).gameObject, parent:column.transform);
                currentCell.transform.localScale = new Vector3(1, 1, 1);
                currentCell.SetActive(true);
            }
            Text textBox = currentCell.GetComponent<Text>();
            textBox.text = strings[i];
        }
        // The next for loop will go through all cells that we don't need anymore and set them to inactive
        for (int i = strings.Count+2; i < column.transform.childCount; i++)
        {
            GameObject currentCell = column.transform.GetChild(i).gameObject;
            currentCell.SetActive(false);
        }
    }

    /// <summary>
    /// Get the strings to be displayed in the table
    /// </summary>
    /// <param name="samples">The samples used to create the strings with</param>
    /// <param name="earlyRayTerminationActivationIndex">The index at which early ray termination starts</param>
    /// <param name="stringGenerator">The function used to generate a single string</param>
    /// <returns></returns>
    private List<string> getStrings(List<Sample> samples, int earlyRayTerminationActivationIndex,
        Func<Sample, string> stringGenerator)
    {
        List<string> result = new List<string>();
        for (int i = 0; i < samples.Count; i++)
        {
            if (i == earlyRayTerminationActivationIndex)
            {
                result.Add("Ray Termination");
                //result.Add(stringGenerator(samples[^1]));
                //return result;
            }
            result.Add(stringGenerator(samples[i]));
        }
        return result;
    }

    /// <summary>
    /// How to generate a string for a sample displayed in the sample column
    /// </summary>
    /// <param name="sample">sample to generate a string for</param>
    /// <returns></returns>
    private string sampleString(Sample sample)
    {
        return string.Format("({1:F1},{2:F1},{3:F1},{4:F1})", sample.RecordedDensity, sample.SampleColor.r, sample.SampleColor.g, sample.SampleColor.b, sample.SampleColor.a);
    }

    /// <summary>
    /// How to generate a string for a sample displayed in the composited color column
    /// </summary>
    /// <param name="sample">sample to generate a string for</param>
    /// <returns></returns>
    private string colorString(Sample sample)
    {
        return string.Format("({0:F1},{1:F1},{2:F1})", sample.CompositedSampleColor.r, sample.CompositedSampleColor.g, sample.CompositedSampleColor.b);
    }

    /// <summary>
    /// How to generate a string for a sample displayed in the composited opacity column
    /// </summary>
    /// <param name="sample">sample to generate a string for</param>
    /// <returns></returns>
    private string opacityString(Sample sample)
    {
        return string.Format("{0:F1}", sample.CompositedSampleColor.a);
    }

    /// <summary>
    /// How to generate a string for a sample displayed in the density column
    /// </summary>
    /// <param name="sample">sample to generate a string for</param>
    /// <returns></returns>
    private string densityString(Sample sample)
    {
        return string.Format("{0:F1}", sample.RecordedDensity);
    }
    
    /// <summary>
    /// Get the formula image for a compositing method
    /// </summary>
    /// <param name="type">Compositing method to get the formula image for</param>
    /// <returns>image sprite with formulas of compositing method</returns>
    private Sprite formulas(RCRay.CompositingMethodType type)
    {
        switch (type)
        {
            case RCRay.CompositingMethodType.Accumulate:
                return AccumulateFormulas;
            case RCRay.CompositingMethodType.Average:
                return AverageFormulas;
            case RCRay.CompositingMethodType.First:
                return FirstFormulas;
            case RCRay.CompositingMethodType.Maximum:
                return MaximumFormulas;
        }
        return null;
    }
}
