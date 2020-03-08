using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuadTree
{
    bool isLeaf;

    /* Contains the 4 sub-quadtrees.
    1 3
    0 2
    // */
    HashSet<Prism> contained;

    QuadTree[] subtrees;
    Vector2 center;
    float radius;


    public QuadTree(int depth, Vector2 point, float rad) {
        center = point;
        radius = rad;

        if(depth <= 0) {
            contained = new HashSet<Prism>();
            isLeaf = true;
            return;
        }

        subtrees = new QuadTree[4];
        subtrees[0] = new QuadTree(depth-1, new Vector2(point.x - radius/2, point.y - radius/2), radius/2);
        subtrees[1] = new QuadTree(depth-1, new Vector2(point.x - radius/2, point.y + radius/2), radius/2);
        subtrees[2] = new QuadTree(depth-1, new Vector2(point.x + radius/2, point.y - radius/2), radius/2);
        subtrees[3] = new QuadTree(depth-1, new Vector2(point.x + radius/2, point.y + radius/2), radius/2);
    }

    /* Registers a prism into a quadrant

     - If the quadrant is not a leaf node (smallest size), register to each of the four subquadrants
     - If it is a leaf but already has this prism, just return -1

     - Otherwise, return a 2-element list (basically a tuple) of the two colliding prisms.

    // */
    public List<Prism[]> register(Prism p) {

        //if this isn't a leaf, just send the prism down the tree
        if(!isLeaf) {
            if(p.bounds[0].x < center.x && p.bounds[0].y < center.y) {
                subtrees[0].register(p);
            }
            
            if(p.bounds[0].x < center.x && p.bounds[1].y > center.y) {
                subtrees[1].register(p);
            }

            if(p.bounds[1].x > center.x && p.bounds[0].y < center.y) {
                subtrees[2].register(p);
            }

            if(p.bounds[1].x > center.x && p.bounds[1].y > center.y) {
                subtrees[3].register(p);
            }

            return null;
        }

        //if we already have this in the set (somehow), just ignore it
        if(contained.Contains(p)) {
            return null;
        }

        List<Prism[]> collisions = new List<Prism[]>();

        //so now we know we don't have it in the set, and we're a leaf.
        //note this would only get sent to us if it's within bounds
        foreach (Prism member in contained) {
            Prism[] collision = new Prism[2];
            collision[0] = member;
            collision[1] = p;

            contained.Add(p);
        }

        return collisions;
    }

    /*
    public QuadTree(int depth, Vector2 minpoint, Vector2 maxpoint) {
        bounds = new Vector2[2];
        bounds[0] = minpoint;
        bounds[1] = maxpoint;

        if(depth <= 0) {
            isLeaf = true;
            return;
        }

        float avgX = (maxpoint.x - minpoint.x) / 2;
        float avgY = (maxpoint.y - minpoint.y) / 2;

        subtrees = new QuadTree[4];
        subtrees[0] = new QuadTree(depth-1, minpoint, new Vector2(avgX, avgY));
        subtrees[1] = new QuadTree(depth-1, new Vector2(minpoint.x, avgY), new Vector2(avgX, maxpoint.y));
        subtrees[2] = new QuadTree(depth-1, new Vector2(avgX, minpoint.y), new Vector2(maxpoint.x, avgY));
        subtrees[3] = new QuadTree(depth-1, new Vector2(avgX, avgY), maxpoint);
    }
    // */


}
