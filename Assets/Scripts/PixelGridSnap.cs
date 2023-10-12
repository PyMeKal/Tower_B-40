using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelGridSnap : MonoBehaviour
{
    public int pixelsPerUnit = 16;

    // Update is called once per frame
    void Update()
    {
        Vector3 parentPos = transform.parent.position;
        Vector3 mod = new Vector3(Mod(parentPos.x, 1f/pixelsPerUnit), Mod(parentPos.x, 1f/pixelsPerUnit));
        transform.position = parentPos - mod;
    }

    float Mod(float a, float b)
    {
        if (a > 0f)
        {
            return a % b;
        }

        return -(-a % b);
    }
}
