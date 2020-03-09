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


    public bool seeGrid = true, seeBoxes = false;

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

        if(seeGrid)
            DrawGridLines();

        if(seeBoxes)
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

    #region COMPLETE Functions


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
        #region Great Jolly Kappadia

        var prismA = collision.a;
        var prismB = collision.b;

        //Generate minkowski difference
        //List<Vector3> minkowskiDiff = new List<Vector3>();
        Vector3 offset = new Vector3(0, 1, 0);

        /*
        foreach (Vector3 vecA in prismA.points) {
            foreach (Vector3 vecB in prismB.points) {
                Vector3 nextVec = vecA - vecB;
                minkowskiDiff.Add(nextVec);
                //Debug.DrawLine(nextVec, nextVec + offset, Color.black, 5f); 

            }
        }
        // */

        List<Vector3> simplex = new List<Vector3>();

        Vector3 initDir = new Vector3(1, 0, 0);
        simplex.Add(MinkowskiSupport(prismA.points, prismB.points, initDir));
        initDir = -initDir;


        while(true){
            simplex.Add(MinkowskiSupport(prismA.points, prismB.points, initDir));

            //Optimization to make sure the last element added actually passed the origin
            //If not the Minkowski sum doesn't contain the origin
            if(Vector3.Dot(simplex.Last(), initDir) <= 0){
                return false;
            }
            else{
                var values = containsOrigin(simplex, initDir);
                initDir = values.Item2;
                if(values.Item1){
                    break;
                }
            }
        }

        foreach(Vector3 v in simplex){
            //Debug.DrawLine(v, v + 2*offset, Color.blue, 5f);
        }
        //Debug.DrawLine(Vector3.zero, Vector3.zero + 2*offset, Color.white, 5f);




        #endregion

        #region Environmental Protection Agency

        List<Edge> edges = new List<Edge> ();
        for (int x = 0; x < simplex.Count; x++)
        {
            for (int y = x + 1; y < simplex.Count; y++)
            {
                Edge e = new Edge(simplex[x], simplex[y]);
                if(!edges.Contains(e))
                {
                    edges.Add(e);
                }
            }
        }

        //Doesn't work currently, so commented out
        //This is suppoped to be part of the EPA algorithm
        float improvement = 10, best = -1, bestdist = -1;
        int curpos;

        while(improvement > 0.5)
        {

            curpos = -1;
            bestdist = -1;

            for(int pos = 0; pos < edges.Count; pos++)
            {
                float result = edges[pos].originDistance();
                if(bestdist < 0 || result < bestdist)
                {
                    bestdist = result;
                    curpos = pos;
                }
            }

            best = bestdist;

            Vector3 newPoint = MinkowskiSupport(prismA.points, prismB.points, edges[curpos].normal());

            simplex.Add(newPoint);
            edges.Add(new Edge(edges[curpos].v1, newPoint));
            edges.Add(new Edge(newPoint, edges[curpos].v2));

            edges.Remove(edges[curpos]);

            float newOD1 = edges[edges.Count - 2].originDistance();
            float newOD2 = edges[edges.Count - 1].originDistance();

            bestdist = min(newOD1, newOD2);

            improvement = best - bestdist;
        }

        curpos = -1;
        bestdist = -1;

        for(int pos = 0; pos < edges.Count; pos++)
        {
            float result = edges[pos].originDistance();
            if(bestdist < 0 || result < bestdist)
            {
                bestdist = result;
                curpos = pos;
            }
        }

        collision.penetrationDepthVectorAB = edges[curpos].normal() * (bestdist + 0.05f);

        #endregion

        return true;
    }

    private (bool, Vector3) containsOrigin(List<Vector3> simplex, Vector3 dir){
        Vector3 a = simplex.Last();
        Vector3 a0 = -a;

        if(simplex.Count == 3){
            Vector3 b = simplex.ElementAt(1);
            Vector3 c = simplex.ElementAt(0);

            Vector3 ab = b-a;
            Vector3 ac = c-a;

            Vector3 abNor = tripleProduct(ac, ab, ab);
            Vector3 acNor = tripleProduct(ab, ac, ac);

            if(Vector3.Dot(abNor, a0) > 0){
                simplex.Remove(c);
                dir = abNor;
            }else if(Vector3.Dot(acNor, a0) > 0){
                simplex.Remove(b);
                dir = acNor;
            }else{
                return (true, dir);
            }
        }else{
            Vector3 b = simplex.ElementAt(0);
            Vector3 ab = b-a;
            Vector3 abNor = tripleProduct(ab, a0, ab);
            dir = abNor;
        }

        return (false, dir);
    }

    private Vector3 tripleProduct(Vector3 a, Vector3 b, Vector3 c){
        return Vector3.Cross(Vector3.Cross(a, b), c);
    }

    private Vector3 MinkowskiSupport(Vector3[] shape1, Vector3[] shape2, Vector3 vec){
        Vector3 p1 = FarthestPointInDirection(shape1, vec);
        Vector3 p2 = FarthestPointInDirection(shape2, -vec);

        return p1-p2;
    }

    private Vector3 FarthestPointInDirection(Vector3[] vertices, Vector3 vec){
        float highest = -float.MaxValue;
        Vector3 support = new Vector3(0, 0, 0);

        foreach(Vector3 v in vertices){
            float dot = Vector3.Dot(v, vec);

            if(dot > highest){
                highest = dot;
                support = v;
            }
        }

        return support;
    }
    
    #endregion

    #region Private Functions
    
    private void ResolveCollision(PrismCollision collision)
    {
        var prismObjA = collision.a.prismObject;
        var prismObjB = collision.b.prismObject;

        var pushA = -collision.penetrationDepthVectorAB / 2;
        var pushB = collision.penetrationDepthVectorAB / 2;

        for (int i = 0; i < collision.a.pointCount; i++)
        {
            collision.a.points[i] += pushA;
        }
        for (int i = 0; i < collision.b.pointCount; i++)
        {
            collision.b.points[i] += pushB;
        }
        //prismObjA.transform.position += pushA;
        //prismObjB.transform.position += pushB;

        //Debug.DrawLine(prismObjA.transform.position, prismObjA.transform.position + collision.penetrationDepthVectorAB, Color.cyan, UPDATE_RATE);
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

    private float min(float a, float b) {
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

    private class Edge
    {
        public Vector3 v1, v2;

        public Edge(Vector3 a, Vector3 b)
        {
            v1 = a;
            v2 = b;
        }

        public bool Equals(Edge e)
        {
            return v1.Equals(e.v1) && v2.Equals(e.v2);
        }

        public float originDistance()
        {
            float numerator = System.Math.Abs(  v2.x*v1.z - v2.z*v1.x  );
            float denom = (float) System.Math.Sqrt((v2.z - v1.z) * (v2.z - v1.z) + (v2.x - v1.x) * (v2.x - v1.x));

            return numerator / denom;
        }

        public Vector3 normal()
        {
            Vector3 AB = new Vector3(v2.x - v1.x, v2.y - v1.y, v2.z - v1.z);
            Vector3 A0 = new Vector3(v1.x, v1.y, v1.z);

            return (Vector3.Cross(Vector3.Cross(AB, A0), AB)).normalized;
        }
    }

    #endregion
}
