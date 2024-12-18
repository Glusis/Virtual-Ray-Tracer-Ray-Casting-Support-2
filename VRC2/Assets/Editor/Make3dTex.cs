using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public class Make3dTex : MonoBehaviour
{
    [MenuItem("CreateVoxelGrid/3DTexture")]
    public static void CreateAllTextures(){
        CreateTexture3D("/bunny512x512x361.raw","BunnyTex",512,512,361);
        CreateTexture3D("/bucky32x32x32.raw","BuckyTex",32,32,32);
        CreateTexture3D("/engine256x256x256.raw","EngineTex",256,256,256);
        CreateTexture3D("/hnut256_uint.raw","HazelTex",256,256,256);
    }

    
    public static void CreateTexture3D(String filepath, String filename, int xsize, int ysize, int zsize)
    {
        //configure file path
        float[,,] grid = getGridFromFile(filepath,xsize,ysize, zsize);
        
        // Create the texture and apply the configuration
        TextureFormat format = TextureFormat.RGBA32;
        TextureWrapMode wrapMode =  TextureWrapMode.Clamp;
        Texture3D texture = new Texture3D(xsize, ysize, zsize, format, false);
        texture.wrapMode = wrapMode;

        for (int z = 0; z < zsize; z++)
        {
            for (int y = 0; y < ysize; y++)
            {
                for (int x = 0; x < xsize; x++)
                {
                    texture.SetPixel(x,y,z,new Color(0.0f,
                        0.0f, 0.0f, (float)grid[x,y,z]));
                }
            }
        }
        
        // Apply the changes to the texture and upload the updated texture to the GPU
        texture.Apply();        

        // Save the texture
        AssetDatabase.CreateAsset(texture, "Assets/_Project/Resources/3DTex/" + filename + ".asset");
    }

    private static float[,,] getGridFromFile(string path, int sizeX, int sizeY, int sizeZ)
    {
        // And read the binary file
        BinaryReader binReader = new BinaryReader(File.Open((Application.streamingAssetsPath + path), FileMode.Open));
        float[,,] grid = new float[sizeX, sizeY, sizeZ];
        for (int z = 0; z < sizeZ; z++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    grid[x, y, z] = binReader.ReadByte();
                    grid[x, y, z] /= 255;
                }
            }
        }
        binReader.Close();

        return grid;
    }


    [MenuItem("CreateVoxelGrid/NormalMap")]
    public static void CreateNormalMap()
    {
        //set dimensions
        int xsize = 512;
        int ysize = 512;
        int zsize = 361;
        
        //get voxel grid from assets
        float[,,] grid = getGridFromFile("/bunny512x512x361.raw", xsize,ysize,zsize);
        //create new 3d texture
        TextureFormat format = TextureFormat.RGBA32;
        TextureWrapMode wrapMode =  TextureWrapMode.Clamp;
        Texture3D texture = new Texture3D(xsize, ysize, zsize, format, false);
        texture.wrapMode = wrapMode;
        //loop through grid and set normal as color in 3d texture
        int delta = 3;
        for (int z = delta; z < zsize-delta; z++)
        {
            for (int y = delta; y < ysize-delta; y++)
            {
                for (int x = delta; x < xsize-delta; x++)
                {
                    texture.SetPixel(x,y,z,centralDiffColor(grid,x,y,z,xsize,ysize,zsize, delta));
                }
            }
        }
        
        // Apply the changes to the texture and upload the updated texture to the GPU
        texture.Apply();        

        // Save the texture
        AssetDatabase.CreateAsset(texture, "Assets/_Project/Resources/3DTex/bunny_normals.asset");
    }

    private static Color centralDiffColor(float[,,] grid, int x, int y, int z, int xsize, int ysize, int zsize, int delta)
    {
        Color color = new Color(0,0,0,1);
        try
            {
                //TODO: apply transfer function and use alpha channel
                //use central differences to make gradient vectors
                float xdif = grid[x + delta, y, z] - grid[x - delta, y, z];
                float ydif = grid[x, y + delta, z] - grid[x, y - delta, z];
                float zdif = grid[x, y, z + delta] - grid[x, y, z - delta];
                color = new Color(xdif, ydif, zdif, 0);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
                UnityEngine.Debug.Log("vars: " + x + " " + y + " " + z);
            }
        return color;
    }
}
