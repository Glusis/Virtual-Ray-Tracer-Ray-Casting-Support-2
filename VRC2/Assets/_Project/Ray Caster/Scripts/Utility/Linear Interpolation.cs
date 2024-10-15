using _Project.Ray_Caster.Scripts.Voxel_Grid;
using UnityEngine;
using System;
using System.Runtime.InteropServices;
using _Project.Ray_Caster.Scripts.RC_Ray;

/// <summary>
/// Utility for performing linear interpolation
/// </summary>
public class LinearInterpolation
{
    private int findIndexBelowPoint(float point, RCRay.ColorTableEntry[] line)
    {
        for (int i = 1; i < line.Length; i++)
        {
            if (point < line[i].Density)
            {
                //Debug.Log("returing " + (i - 1) + " because " + point + " < " + line[i].Density);
                return i - 1;
            }
        }
        return line.Length - 2;
    }
    
    public Color MonoLinearInterpolation(float point, RCRay.ColorTableEntry[] line)
    {
        if (point <= line[0].Density)
        {
            return line[0].ColorAlpha;
        }
        // find between which indices of the line our point is
        int lowerIndex = findIndexBelowPoint(point, line);
        // We check how much distance is between the density below our point and the density above our point
        float differenceBetweenClosestValues = line[lowerIndex + 1].Density - line[lowerIndex].Density;
        // We then calculate the closeness from 0 - 1 between our point and its neighbouring density values
        float distanceToLowerSide = (point - line[lowerIndex].Density) / differenceBetweenClosestValues;
        float distanceToHigherSide = (line[lowerIndex + 1].Density - point) / differenceBetweenClosestValues;
        //Debug.Log(string.Format("point: {0} evaluated to differenceBetweenCloststValues = {1} closenessToLowerSide = {2} closenessToHigherSide = {3}", point, differenceBetweenClosestValues, closenessToLowerSide, closenessToHigherSide));

        // We multiply the colour of the neighbours by their closeness
        //Debug.Log("point: "+ point +" lowerNeighbour: "+ line[lowerIndex].Density + line[lowerIndex].ColorAlpha+" higherNeighbour: "+ line[lowerIndex+1].Density + line[lowerIndex].ColorAlpha);
        return line[lowerIndex].ColorAlpha * distanceToHigherSide + line[lowerIndex + 1].ColorAlpha * distanceToLowerSide;
    }
    
    public double TriLinearInterpolation(Vector3 point, VoxelGrid voxelGrid)
    {
        double[,,] grid = voxelGrid.Grid;
        // Get the bottom left corner of the cube surrounding the point
        int x = point.x <= 0 ? 0 : (int)point.x;
        int y = point.y <= 0 ? 0 : (int)point.y;
        int z = point.z <= 0 ? 0 : (int)point.z;
        // edge cases in case the point is exactly on the far edge of the voxelgrid and original cube would be out of bound
        x = x < voxelGrid.SizeX - 1 ? x : voxelGrid.SizeX - 2;
        y = y < voxelGrid.SizeY - 1 ? y : voxelGrid.SizeY - 2;
        z = z < voxelGrid.SizeZ - 1 ? z : voxelGrid.SizeZ - 2;
        // // xD, yD, and zD are the differences between each of x, y, z and the smaller coordinate related
        float xD, yD, zD;
        xD = point.x - x;
        yD = point.y - y;
        zD = point.z - z;
        // Interpolate the x so we are left with a plane
        double[,] plane = new double[2, 2];
        plane[0, 0] = grid[x,y,z] * (1 - xD) + grid[x + 1,y,z] * xD;
        plane[0, 1] = grid[x,y,z+1] * (1 - xD) + grid[x + 1,y,z+1] * xD;
        plane[1, 0] = grid[x,y+1,z] * (1 - xD) + grid[x + 1,y+1,z] * xD;
        plane[1, 1] = grid[x,y+1,z+1] * (1 - xD) + grid[x + 1,y+1,z+1] * xD;
        // Interpolate the y so we are left with the z line
        double[] line = new double[2];
        line[0] = plane[0, 0] * (1 - yD) + plane[1, 0] * yD;
        line[1] = plane[0, 1] * (1 - yD) + plane[1, 1] * yD;
        // Interpolate the z so we are left with the final value
        return line[0] * (1 - zD) + line[1] * zD;
    }
}
