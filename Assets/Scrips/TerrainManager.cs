﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json; // Import JSON.NET from Unity Asset store
using System.Linq;


public class TerrainManager : MonoBehaviour {
    public TerrainInfo myInfo;

    public GameObject flag;

    TextAsset jsonTextFile;
    public GameObject terrain;

    private string train_maps_prefix = "Text/terrainTrain";
    Cell start;
    Cell goal;

    float height;
    public int[] maps = new int[] {1,2,3,4,5,6}; // Will change the filenames to start from 0
    float[] map_difficulties;
    void Start(){
        height = terrain.transform.position.y;
        map_difficulties = new float[maps.Length];

        // Scan all maps and assign them their difficulty
        for (int i = 0; i < maps.Length; i++) {
            // Load map information from json
            jsonTextFile = Resources.Load<TextAsset>(train_maps_prefix+maps[i]);
            TerrainInfo mapInfo = TerrainInfo.CreateFromJSON(jsonTextFile.text, height);

            // Compute map difficulty
            int occupied = CountObstacles(mapInfo.traversability);
            int tot = (mapInfo.traversability.GetLength(0) - 1) * (mapInfo.traversability.GetLength(1) - 1);
            float map_difficulty = (float) occupied / (float) tot;
            map_difficulties[i] = map_difficulty;
        }
    }
    void Awake(){}
  
    public void SelectMapRandom(float difficulty) {
        int map_idx = GetMapIdxForDifficulty(difficulty);
        ResetMap();
        LoadTrainMap(maps[map_idx]);
        // Adding blocks to reach the given difficulty
        AddRandomBlocks(difficulty);
    }

    public float GetHeight() { return height; }

    private int GetMapIdxForDifficulty(float max_difficulty) {
        // Returns a random index of a map with difficulty < max_difficulty
        List<int> map_indices = new List<int>();
        for (int i = 0; i < maps.Length; i++) {
            if (map_difficulties[i] <= max_difficulty) {
                map_indices.Add(i);
            }
        }
        return RandomChoice(map_indices);
    }

    private int RandomChoice(List<int> values) {
        int idx = RandomInteger(0, values.Count - 1);
        return values[idx];
    }

    public void ResetMap() {
        float height = terrain.transform.position.y;
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("obstacle");
        for (int i = 0; i < obstacles.Length; i++) { // Destroying cubes from previous episode
            if (Mathf.Abs(obstacles[i].transform.position.y - height) < 10) {
                GameObject.DestroyImmediate(obstacles[i]);
            }
        }
        GameObject[] flags = GameObject.FindGameObjectsWithTag("flag");
        for (int i = 0; i < flags.Length; i++) { // Destroying flags from previous episode
            if (Mathf.Abs(flags[i].transform.position.y - height) < 10) {
                GameObject.DestroyImmediate(flags[i]);
            }
        }
        GameObject[] lines = GameObject.FindGameObjectsWithTag("line");
        for (int i = 0; i < lines.Length; i++) { // Destroying lines from previous episode
            if (Mathf.Abs(lines[i].transform.position.y - height) < 10) {
                GameObject.DestroyImmediate(lines[i]);
            }
        }
        GameObject[] line_renderers = GameObject.FindGameObjectsWithTag("line_renderer");
        for (int i = 0; i < line_renderers.Length; i++) { // Destroying line_renderers from previous episode
            if (Mathf.Abs(line_renderers[i].transform.position.y - height) < 10) {
                GameObject.DestroyImmediate(line_renderers[i]);
            }
        }
    }

    public void LoadTrainMap(int map_idx) {
        string file_name = "Text/terrainTrain"+map_idx;
        jsonTextFile = Resources.Load<TextAsset>(file_name);
        
        float height = terrain.transform.position.y;
        myInfo = TerrainInfo.CreateFromJSON(jsonTextFile.text, height);
        float x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
        float z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N; 
        int rows = myInfo.traversability.GetLength(0);
        int cols = myInfo.traversability.GetLength(1);
        
        Cell[] corners = new Cell[] {new Cell(1,1), new Cell(1,cols-2), new Cell(rows-2,1), new Cell(rows-2,cols-2)};
        int start_idx, goal_idx;
        do {
            start_idx = RandomInteger(0,corners.Length - 1);
            goal_idx = RandomInteger(0,corners.Length - 1);
        } while (start_idx == goal_idx);
        start = corners[start_idx];
        goal = corners[goal_idx];
        
        myInfo.start_pos.x = myInfo.x_low + start.row * x_step + x_step * 0.5f;
        myInfo.start_pos.y = height;
        myInfo.start_pos.z = myInfo.z_low + start.col * z_step +z_step * 0.5f;
        
        myInfo.goal_pos.x = myInfo.x_low + goal.row * x_step + x_step * 0.5f;
        myInfo.goal_pos.y = height;
        myInfo.goal_pos.z = myInfo.z_low + goal.col * z_step + z_step * 0.5f;  
        myInfo.CreateCubes();
    }

    public void LoadMap(string file_name, bool use_start_goal_info, bool swap_start_end = false) {
        ResetMap();
        jsonTextFile = Resources.Load<TextAsset>(file_name);
        float height = terrain.transform.position.y;
        myInfo = TerrainInfo.CreateFromJSON(jsonTextFile.text, height);

        float x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
        float z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N; 
        int rows = myInfo.traversability.GetLength(0);
        int cols = myInfo.traversability.GetLength(1);
        if (use_start_goal_info) {
            int start_row = Mathf.FloorToInt((myInfo.start_pos.x - myInfo.x_low) / x_step);
            int start_col = Mathf.FloorToInt((myInfo.start_pos.z - myInfo.z_low) / z_step);
            int goal_row = Mathf.FloorToInt((myInfo.goal_pos.x - myInfo.x_low) / x_step);
            int goal_col = Mathf.FloorToInt((myInfo.goal_pos.z - myInfo.z_low) / z_step);
            start = new Cell(start_row, start_col);
            goal = new Cell(goal_row, goal_col);
        } else {
            if (swap_start_end) {
                Cell[] corners = new Cell[] {new Cell(1, 1), new Cell(rows - 2, cols - 2)};
                int idx = RandomInteger(0, 1);
                start = corners[idx];
                goal = corners[Mathf.Abs(idx - 1)];
            } else {
                start = new Cell(1, 1);
                goal = new Cell(rows - 2, cols - 2);
            }
            myInfo.start_pos.x = myInfo.x_low + start.row * x_step + x_step * 0.5f;
            myInfo.start_pos.y = height;
            myInfo.start_pos.z = myInfo.z_low + start.col * z_step +z_step * 0.5f;
            myInfo.goal_pos.x = myInfo.x_low + goal.row * x_step + x_step * 0.5f;
            myInfo.goal_pos.y = height;
            myInfo.goal_pos.z = myInfo.z_low + goal.col * z_step + z_step * 0.5f;  
        }
        myInfo.CreateCubes();
    }

    private void AddRandomBlocks(float difficulty) {
        float[,] traversability;
        int iteration = 0;
        do {
            traversability = GenerateTraversability(difficulty, start, goal);
            if (iteration++ > 0 && iteration % 3000 == 0) {
                difficulty -= 0.01f;
            }
        } while (! IsTraversable(traversability, start, goal));
        myInfo.traversability = traversability;
        myInfo.CreateCubes();
    }

    private float[,] GenerateTraversability(float difficulty, Cell start, Cell goal) {
        //Difficulty: proportion of map covered by blocks [0, 1]
        
        // IMPORTANT: rows are X axis, cols are Z axis
        int rows = myInfo.traversability.GetLength(0);
        int cols = myInfo.traversability.GetLength(1);
        float[,] traversability = myInfo.traversability.Clone() as float[,];
        int nblocks = Mathf.FloorToInt(difficulty * (rows - 1) * (cols - 1)) - CountObstacles(traversability);

        int placed_blocks = 0;
        while(placed_blocks < nblocks) {
            int row = Mathf.RoundToInt(Random.Range(1, rows - 1));
            int col = Mathf.RoundToInt(Random.Range(1, cols - 1));
            if ((row == start.row && col == start.col) || //Don't place over start
                (row == goal.row && col == goal.col) || // Don't place over goal
                traversability[row, col] == 1.0f) // Don't place where already placed
            {
                continue;
            }
            traversability[row, col] = 1.0f;
            placed_blocks ++;
        }
        return traversability;
    }

    private int CountObstacles(float[,] traversability) {
        // Counts the number of occupied blocks (border walls excluded)
        int rows = traversability.GetLength(0);
        int cols = traversability.GetLength(1);
        int tot = 0;
        for (int r = 0; r < rows; r++) {
            for (int c = 0; c < cols; c++) {
                if (traversability[r,c] == 1.0f) {
                    tot++;
                }    
            }
        }
        tot -= 2 * (rows + cols); // Don't count borders
        tot += 4; // Corners were counted twice 
        return tot;
    }

    private bool IsTraversable(float[,] traversability, Cell start, Cell goal) {
        int rows = traversability.GetLength(0);
        int cols = traversability.GetLength(1);
        bool[,] visited = new bool[rows, cols];
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(start);
        while(queue.Count > 0) {
            Cell cell = queue.Dequeue();
            if (cell.Equals(goal)) {
                return true;
            }
            Cell[] neighbors = new Cell[]{
                new Cell(cell.row-1, cell.col), new Cell(cell.row+1, cell.col),
                new Cell(cell.row, cell.col-1), new Cell(cell.row, cell.col+1)};
            foreach (Cell c in neighbors) {
                if (! (c.row < 0 || c.row >= rows || c.col < 0 || c.col >= cols)) {
                    if (!visited[c.row, c.col] && traversability[c.row, c.col] == 0) {
                        queue.Enqueue(c);
                        visited[c.row, c.col] = true;
                    }
                }
            }
        }
        return false;
    }

    private int RandomInteger(int low, int high) {
        int r = Mathf.FloorToInt(Random.Range(low, high+1));
        if (r == high+1) {
            r = high;
        }
        return r;
    }

    private class Cell{
        public int row, col;
        public Cell(int row, int col) {
            this.row = row;
            this.col = col;
        }
        public bool Equals(Cell that) {
            if (this.row == that.row && this.col == that.col) {
                return true;
            } else {
                return false;
            }
        }
    }



    // Update is called once per frame
    void Update () {
		
	}

    public void DrawLine(Vector3 a, Vector3 b, Color color, float width = 0.1f){
        var go = new GameObject();
        go.tag = "line";
        var lr = go.AddComponent<LineRenderer>();
        lr.tag = "line";
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
                    Debug.DrawLine(path[i], path[i+1], Color.red, 1f);
                }
            }
    }

    public void DrawCircle(GameObject gameObject, float radius, float lineWidth) {
        var segments = 180;
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
        line.tag = "line_renderer";
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
        // Debug.LogWarning(start_pos.y);
        // Debug.LogWarning(traversability);
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
                    cube.transform.position = new Vector3(get_x_pos(i), start_pos.y, get_z_pos(j));
                    cube.transform.localScale = new Vector3(x_step, 15.0f, z_step);
                }
            }
        }
        int cubes = GameObject.FindGameObjectsWithTag("obstacle").Count();
    }



    public static TerrainInfo CreateFromJSON(string jsonString, float height)
    {
        //Debug.Log("Reading json");
        TerrainInfo ti = JsonConvert.DeserializeObject<TerrainInfo>(jsonString);
        // NOTE: SUper sketchy way to set terrain height
        ti.start_pos.y = height;
        ti.goal_pos.y = height;
        return ti;
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