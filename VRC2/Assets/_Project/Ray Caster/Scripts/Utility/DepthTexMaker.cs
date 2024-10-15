using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Enables Camera Depth Texture generation, so it can be accessed by shaders.
 * Could be done anywhere but having its own script makes it easier to find.
 */
[ExecuteInEditMode]//Enables Camera Depth texture while in editor, so depth testing works without having to hit 'play'. Disable if the project doesn't build with it on
public class DepthTexMaker : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
