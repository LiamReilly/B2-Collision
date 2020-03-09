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
        
        List<Prism[]> collisions = new List<Prism[]>();

        //if this isn't a leaf, just send the prism down the tree, take everything ya got, send it back up
        if(!isLeaf) {


            if(p.bounds[0].x < center.x && p.bounds[0].y < center.y) {
                collisions.AddRange(subtrees[0].register(p));
            }
            
            if(p.bounds[0].x < center.x && p.bounds[1].y > center.y) {
                collisions.AddRange(subtrees[1].register(p));
            }

            if(p.bounds[1].x > center.x && p.bounds[0].y < center.y) {
                collisions.AddRange(subtrees[2].register(p));
            }

            if(p.bounds[1].x > center.x && p.bounds[1].y > center.y) {
                collisions.AddRange(subtrees[3].register(p));
            }

            return collisions;
        }

        //if we already have this in the set (somehow), just ignore it
        if(contained.Contains(p)) {
            //MonoBehaviour.print("Already contained");
            return collisions;
        }

        //so now we know we don't have it in the set, and we're a leaf.
        //note this would only get sent to us if it's within bounds
        foreach (Prism member in contained) {
            Prism[] collision = new Prism[2];
            collision[0] = member;
            collision[1] = p;

            /*
            if(member.num == 8 || p.num == 8) {
                print("Collision: " + member.num + " with " + p.num);
                print ("Square centered at " + center);
            }
            // */

            collisions.Add(collision);
        }
        
        contained.Add(p);

        //MonoBehaviour.print("Added prism " + p.num + " to a place centered at " + center);

        //if(collisions.Count > 1)
            //print(collisions.Count+"");
        return collisions;
    }

    public void draw() {
        if(isLeaf) {
            return;
        }

        Vector3 leftside = new Vector3(center.x - radius, 0, center.y);
        Vector3 rightside = new Vector3(center.x + radius, 0, center.y);
        Vector3 top = new Vector3(center.x, 0, center.y + radius);
        Vector3 bottom = new Vector3(center.x, 0, center.y - radius);

        var c = Color.magenta;
        Debug.DrawLine(leftside, rightside, c);
        Debug.DrawLine(top, bottom, c);

        foreach(QuadTree qt in subtrees) {
            qt.draw();
        }
    }

    public void print(string s) {
        MonoBehaviour.print(s);
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
