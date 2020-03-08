using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prism : MonoBehaviour
{
    public int pointCount = 3;
    public Vector3[] points;
    public float midY, height;

    /* array holding the bounding box's specs
    bounds[0]: bottom left
    bounds[1]: top right
    // */
    public Vector2[] bounds; 

    //int holding the vector's assigned number
    public int num;

    //sets up the boundary rectangle
    public virtual void setBounds() {
        float minx = points[0].x, minz = points[0].z, maxx = points[0].x, maxz = points[0].z;

        foreach(Vector3 p in points) {
            if(p.x < minx)
                minx = p.x;
            if(p.x > maxx)
                maxx = p.x;
            if(p.z < minz)
                minz = p.z;
            if(p.z > maxz)
                maxz = p.z;
        }

        bounds = new Vector2[2];
        bounds[0] = new Vector2(minx, minz);
        bounds[1] = new Vector2(maxx, maxz);
    }

    public GameObject prismObject;
}
