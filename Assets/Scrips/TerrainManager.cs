using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json; // Import JSON.NET from Unity Asset store


public class TerrainManager : MonoBehaviour {


    //public TestScriptNoObject testNoObject = new TestScriptNoObject();

    public string terrain_filename = "Text/terrainC";
    public TerrainInfo myInfo;

    public GameObject flag;

    // Use this for initialization
    void Start()
    {

    }

    // Use this for initialization
    void Awake()
    {

        var jsonTextFile = Resources.Load<TextAsset>(terrain_filename);

        myInfo = TerrainInfo.CreateFromJSON(jsonTextFile.text);

        myInfo.CreateCubes();

        // this code is used to create new terrains and obstacles
        //myInfo.TerrainInfo2();
        //myInfo.file_name = "test88";
        //string myString = myInfo.SaveToString();
        //myInfo.WriteDataToFile(myString);

        // Uncomment this to display start and goal flags
        //Instantiate(flag, myInfo.start_pos, Quaternion.identity);
        //Instantiate(flag, myInfo.goal_pos, Quaternion.identity);
    }
  

    public List<Vector3> GenerateRandomTrajectory(int npoints, int distance, float maxtheta) {
        // Generate random trajectory of npoints with the given distance. Each point is
        // At no more than +- maxtheta degrees from the precedent
        float margin = 4;
        float y = myInfo.start_pos.y;
        List<Vector3> points = new List<Vector3>();
        points.Add(myInfo.start_pos);
        Vector3 secondPoint = new Vector3(221, y, 230);
        points.Add(secondPoint);
        for (int i = 1; i < npoints; i++) {
            Vector3 nextPoint;
            do {
                nextPoint = GenerateNewPoint(points[i - 1], points[i], distance, maxtheta);
            } while (!InsideMap(nextPoint, margin));
            points.Add(nextPoint);
        }
        return points;
    }

    Vector3 GenerateNewPoint(Vector3 pointA, Vector3 pointB, float distance, float maxtheta) {
        // pointB and pointA are respectively the last and last - 1 points
        // Generate the next point with the given maximum angle from the direction
        // pointA -> pointB and the given distance from pointB
        Transform lastPointTransform = GetTransformedDirection(pointA, pointB);
        Vector3 forward = new Vector3(0, 0, distance);
        float rotation = SampleFromNormal(maxtheta, maxtheta/6);
        Vector3 rotatedRelative = Quaternion.Euler(0, rotation, 0) * forward;
        Vector3 newPoint = lastPointTransform.TransformPoint(rotatedRelative);
        return newPoint;
    }

    Transform GetTransformedDirection(Vector3 pointA, Vector3 pointB) {
        // Returns a Transform with position = pointB, orientated
        // in the direction pointA -> pointB
        Vector3 direction = (pointB - pointA).normalized;
        GameObject empty = new GameObject();
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        empty.transform.position = pointB;
        empty.transform.rotation = rotation;
        return empty.transform;
    }

    float SampleFromNormal(float maxtheta, float stdev) {
        // Sample from normal distribution with zero mean and given stdev
        int n = Mathf.CeilToInt(3 * Mathf.Pow(stdev, 2) / Mathf.Pow(maxtheta, 2));
        float tot = 0;
        for (int i = 0; i < n; i++) {
            tot += Random.Range(-maxtheta, +maxtheta);
        }
        return tot / n;
    }

    bool InsideMap(Vector3 point, float margin) {
        float x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
        float z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N;

        if (point.x < myInfo.x_low + x_step + margin || point.x > myInfo.x_high - x_step - margin) {
            return false;
        }
        if (point.z < myInfo.z_low + z_step + margin || point.z > myInfo.z_high - z_step - margin) {
            return false;
        }
        return true;
    }



    // Update is called once per frame
    void Update () {
		
	}

    public void DrawLine(Vector3 a, Vector3 b, Color color, float width = 0.1f){
        var go = new GameObject();
        var lr = go.AddComponent<LineRenderer>();
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.SetColors(color, color);
        lr.SetWidth(width, width);
        // lr.Set
    }

    public void DrawPath(List<Vector3> path, float checkpoint_threshold) {
        for (int i = 0; i < path.Count; i++) {
                GameObject check_flag = Instantiate(flag, path[i], Quaternion.identity);
                DrawCircle(check_flag, checkpoint_threshold, 1);
                if (i < path.Count -1) {
                    DrawLine(path[i], path[i+1], Color.green);
                    Debug.DrawLine(path[i], path[i+1], Color.red, 1000);
                }
            }
    }

    public void DrawCircle(GameObject gameObject, float radius, float lineWidth) {
        var segments = 360;
        var line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = segments + 1;
        var pointCount = segments + 1; // add extra point to make startpoint and endpoint the same to close the circle
        var points = new Vector3[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            var rad = Mathf.Deg2Rad * (i * 360f / segments);
            points[i] = new Vector3(Mathf.Sin(rad) * radius, 0, Mathf.Cos(rad) * radius);
        }
        line.SetPositions(points);
    }
}



[System.Serializable]
public class TerrainInfo
{
    public string file_name;
    public float x_low;
    public float x_high;
    public int x_N;
    public float z_low;
    public float z_high;
    public int z_N;
    public float[,] traversability;

    public Vector3 start_pos;
    public Vector3 goal_pos;


    //public TerrainInfo()
    //{
    //    return;
    //}

    public int get_i_index(float x)
    {
        int index = (int) Mathf.Floor(x_N * (x - x_low) / (x_high - x_low));
        if (index < 0)
        {
            index = 0;
        }else if (index > x_N - 1)
        {
            index = x_N - 1;
        }
        return index;

    }
    public int get_j_index(float z) // get index of given coordinate
    {
        int index = (int)Mathf.Floor(z_N * (z - z_low) / (z_high - z_low));
        if (index < 0)
        {
            index = 0;
        }
        else if (index > z_N - 1)
        {
            index = z_N - 1;
        }
        return index;
    }

    public float get_x_pos(int i)
    {
        float step = (x_high - x_low) / x_N;
        return x_low + step / 2 + step * i;
    }

    public float get_z_pos(int j) // get position of given index
    {
        float step = (z_high - z_low) / z_N;
        return z_low + step / 2 + step * j;
    }

    public void CreateCubes()
    {
        float x_step = (x_high - x_low) / x_N;
        float z_step = (z_high - z_low) / z_N;
        for (int i = 0; i < x_N; i++)
        {
            for (int j = 0; j < z_N; j++)
            {
                if (traversability[i, j] > 0.5f)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.tag = "obstacle";
                    cube.transform.position = new Vector3(get_x_pos(i), 0.0f, get_z_pos(j));
                    cube.transform.localScale = new Vector3(x_step, 15.0f, z_step);
                }

            }
        }
    }



    public static TerrainInfo CreateFromJSON(string jsonString)
    {
        //Debug.Log("Reading json");
        return JsonConvert.DeserializeObject<TerrainInfo>(jsonString);
    }

    public string SaveToString()
    {
        //return JsonUtility.ToJson(this);
        return JsonConvert.SerializeObject(this);
    }

    public void WriteDataToFile(string jsonString)
    {
        string path = Application.dataPath + "/Resources/Text/saved_terrain.json";
        Debug.Log("AssetPath:" + path);
        System.IO.File.WriteAllText(path, jsonString);
#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }

}