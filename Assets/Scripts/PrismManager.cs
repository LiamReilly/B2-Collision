using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrismManager : MonoBehaviour
{
    public int prismCount = 10;
    public float prismRegionRadiusXZ = 5;
    public float prismRegionRadiusY = 5;
    public float maxPrismScaleXZ = 5;
    public float maxPrismScaleY = 5;
    public GameObject regularPrismPrefab;
    public GameObject irregularPrismPrefab;

    private List<Prism> prisms = new List<Prism>();
    private List<GameObject> prismObjects = new List<GameObject>();
    private GameObject prismParent;
    private Dictionary<Prism,bool> prismColliding = new Dictionary<Prism, bool>();

    private const float UPDATE_RATE = 0.5f;

    #region Unity Functions

    void Start()
    {
        Random.InitState(0);    //10 for no collision

        prismParent = GameObject.Find("Prisms");
        for (int i = 0; i < prismCount; i++)
        {
            var randPointCount = Mathf.RoundToInt(3 + Random.value * 7);
            var randYRot = Random.value * 360;
            var randScale = new Vector3((Random.value - 0.5f) * 2 * maxPrismScaleXZ, (Random.value - 0.5f) * 2 * maxPrismScaleY, (Random.value - 0.5f) * 2 * maxPrismScaleXZ);
            var randPos = new Vector3((Random.value - 0.5f) * 2 * prismRegionRadiusXZ, (Random.value - 0.5f) * 2 * prismRegionRadiusY, (Random.value - 0.5f) * 2 * prismRegionRadiusXZ);

            GameObject prism = null;
            Prism prismScript = null;
            if (Random.value < 0.5f)
            {
                prism = Instantiate(regularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<RegularPrism>();
            }
            else
            {
                prism = Instantiate(irregularPrismPrefab, randPos, Quaternion.Euler(0, randYRot, 0));
                prismScript = prism.GetComponent<IrregularPrism>();
            }
            prism.name = "Prism " + i;
            prism.transform.localScale = randScale;
            prism.transform.parent = prismParent.transform;
            prismScript.pointCount = randPointCount;
            prismScript.prismObject = prism;

            prismScript.num = i;
            //prismScript.setBounds();

            prisms.Add(prismScript);
            prismObjects.Add(prism);
            prismColliding.Add(prismScript, false);
        }

        StartCoroutine(Run());
    }
    
    void Update()
    {
        #region Visualization

        DrawPrismRegion();
        DrawPrismWireFrames();
        DrawGridLines();
        DrawBoxes();

        #if UNITY_EDITOR
            if (Application.isFocused)
            {
                UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView));
            }
        #endif

        #endregion
    }

    IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            foreach (var prism in prisms)
            {
                prismColliding[prism] = false;
            }

            foreach (var collision in PotentialCollisions())
            {
                if (CheckCollision(collision))
                {
                    prismColliding[collision.a] = true;
                    prismColliding[collision.b] = true;

                    ResolveCollision(collision);
                }
            }

            yield return new WaitForSeconds(UPDATE_RATE);
        }
    }

    #endregion

    #region Incomplete Functions


    public int Quadtree_Depth = 5;
    private IEnumerable<PrismCollision> PotentialCollisions()
    {
        /*
        for (int i = 0; i < prisms.Count; i++) {
            for (int j = i + 1; j < prisms.Count; j++) {
                var checkPrisms = new PrismCollision();
                checkPrisms.a = prisms[i];
                checkPrisms.b = prisms[j];

                yield return checkPrisms;
            }
        }
        // */


        
        foreach(Prism p in prisms) {
            p.setBounds();
        }
        QuadTree tree = new QuadTree(Quadtree_Depth, new Vector2(0.0f, 0.0f), prismRegionRadiusXZ);
        // */

        /* Uses a hashset to keep track of collisions
        This way if A and B are overlapping on many quadrants, you'll
        notice but only do the intensive check a single time.
        // */
        HashSet<int> collisionKeys = new HashSet<int>();
        List<PrismCollision> colList = new List<PrismCollision>();
        
        foreach(Prism p in prisms) {
            List<Prism[]> cols = tree.register(p);

            if(cols.Count == 0) {
                //print("0 collisions");
                continue;
            }

            //print(cols.Count);

            foreach(Prism[] col in cols) {        
                // very simply: order prisms from 0 to n-1, use the key n*bigger + smaller
                int bigger = max(col[0].num, col[1].num), smaller = min(col[0].num, col[1].num);
                int key = bigger*prismCount + smaller;

                //print("Made it here");

                // check if this key (unique for every combination of 2 prisms) is NOT already in the set
                if(!collisionKeys.Contains(key)) {
                    var check = new PrismCollision();
                    check.a = col[0];
                    check.b = col[1];

                    //print("Collision between " + col[0].num + " and " + col[1].num);
                    collisionKeys.Add(key);
                    colList.Add(check);
                }
            }
        }
        // */

        return colList;
    }

 

    private bool CheckCollision(PrismCollision collision)
    {
       
        var prismA = collision.a;
        var prismB = collision.b;

        
        collision.penetrationDepthVectorAB = Vector3.zero;

        return true;
    }
    
    #endregion

    #region Private Functions
    
    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        prismObjA.transform.position += pushA;
        prismObjB.transform.position += pushB;

        Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
    }
    
   private int max(int a, int b) {
        if(a > b)
            return a;
        return b;
    }
    private int min(int a, int b) {
        if(a < b)
            return a;
        return b;
    }

    #endregion

    #region Visualization Functions

    private void DrawPrismRegion()
    {
        var points = new Vector3[] { new Vector3(1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1), new Vector3(-1, 0, 1) }.Select(p => p * prismRegionRadiusXZ).ToArray();
        
        var yMin = -prismRegionRadiusY;
        var yMax = prismRegionRadiusY;

        var wireFrameColor = Color.yellow;

        foreach (var point in points)
        {
            Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
        }

        for (int i = 0; i < points.Length; i++)
        {
            Debug.DrawLine(points[i] + Vector3.up * yMin, points[(i + 1) % points.Length] + Vector3.up * yMin, wireFrameColor);
            Debug.DrawLine(points[i] + Vector3.up * yMax, points[(i + 1) % points.Length] + Vector3.up * yMax, wireFrameColor);
        }
    }

    private void DrawPrismWireFrames()
    {
        for (int prismIndex = 0; prismIndex < prisms.Count; prismIndex++)
        {
            var prism = prisms[prismIndex];
            var prismTransform = prismObjects[prismIndex].transform;

            var yMin = prism.midY - prism.height / 2 * prismTransform.localScale.y;
            var yMax = prism.midY + prism.height / 2 * prismTransform.localScale.y;

            var wireFrameColor = prismColliding[prisms[prismIndex]] ? Color.red : Color.green;

            foreach (var point in prism.points)
            {
                Debug.DrawLine(point + Vector3.up * yMin, point + Vector3.up * yMax, wireFrameColor);
            }

            for (int i = 0; i < prism.pointCount; i++)
            {
                Debug.DrawLine(prism.points[i] + Vector3.up * yMin, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMin, wireFrameColor);
                Debug.DrawLine(prism.points[i] + Vector3.up * yMax, prism.points[(i + 1) % prism.pointCount] + Vector3.up * yMax, wireFrameColor);
            }
        }
    }

    private void DrawGridLines() {
        QuadTree tree = new QuadTree(Quadtree_Depth, new Vector2(0.0f, 0.0f), prismRegionRadiusXZ);
        
        tree.draw();
    }

    private void DrawBoxes() {
        Color c = Color.white;
        Vector3 botLeft, topLeft, botRight, topRight;

        foreach(Prism p in prisms) {
            if(p.bounds.Length == 0)
                continue;

            botLeft = new Vector3(p.bounds[0].x, 0, p.bounds[0].y);
            topLeft = new Vector3(p.bounds[0].x, 0, p.bounds[1].y);
            botRight = new Vector3(p.bounds[1].x, 0, p.bounds[0].y);
            topRight = new Vector3(p.bounds[1].x, 0, p.bounds[1].y);

            Debug.DrawLine(botLeft, topLeft, c);
            Debug.DrawLine(botLeft, botRight, c);
            Debug.DrawLine(topLeft, topRight, c);
            Debug.DrawLine(botRight, topRight, c);
        }
    }

    #endregion

    #region Utility Classes

    private class PrismCollision
    {
        public Prism a;
        public Prism b;
        public Vector3 penetrationDepthVectorAB;
    }

    public class Tuple<K,V>
    {
        public K Item1;
        public V Item2;

        public Tuple(K k, V v) {
            Item1 = k;
            Item2 = v;
        }
    }

    #endregion
}
