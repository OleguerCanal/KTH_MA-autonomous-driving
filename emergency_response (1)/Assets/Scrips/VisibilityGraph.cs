using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VisibilityGraph : MonoBehaviour {

    public float margin;
    public GameObject terrain_manager_game_object;
    public List<Vector3> path_points;  // Access this to get the optimal path points
    
	// Use this for initialization
	void Start () {
        List<Vector3> corners = new List<Vector3>();
        TerrainManager terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        corners.Add(terrain_manager.myInfo.start_pos);
        corners.Add(terrain_manager.myInfo.goal_pos);
		GameObject[] obstacles = GameObject.FindGameObjectsWithTag("obstacle");
        float x_step= (terrain_manager.myInfo.x_high - terrain_manager.myInfo.x_low) / terrain_manager.myInfo.x_N;
        float z_step = (terrain_manager.myInfo.z_high - terrain_manager.myInfo.z_low) / terrain_manager.myInfo.z_N;
        foreach (GameObject obstacle in obstacles) {
            Vector3 center = obstacle.transform.position;
            int[] signs = new int[] {-1, -1, -1, 1, 1, -1, 1, 1};
            for (int i = 0; i < 8; i += 2) {
                float x = center.x + signs[i]*(x_step/2 + margin/Mathf.Sqrt(2));
                float y = center.y;
                float z = center.z + signs[i+1]*(z_step/2 + margin/Mathf.Sqrt(2));
                Vector3 corner = new Vector3(x, y, z);
                bool inside_rigidbody = false; 
                foreach (GameObject obstacle_j in obstacles) {
                    Collider collider = obstacle_j.GetComponent<Collider>();
                    inside_rigidbody = (inside_rigidbody || collider.bounds.Contains(corner));
                }
                if (!inside_rigidbody) {
                    corners.Add(corner);
                }
                else{
                    Debug.Log("inside rigidbody");
                }
            }
        }
        float[, ] adjancenies = get_adjacency_matrix(corners);
        List<int> path_indexes = Dijkstra.get_shortest_path(adjancenies);
        path_points = new List<Vector3>();
        foreach (int path_index in path_indexes) {
            path_points.Add(corners[path_index]);
            if (path_index > 0) {
                DrawLine(path_points[path_points.Count-1], path_points[path_points.Count - 2], Color.yellow);
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
	}

    private void DrawLine(Vector3 a, Vector3 b, Color color)
    {
        var go = new GameObject();
        var lr = go.AddComponent<LineRenderer>();
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.SetColors(color, color);
        // lr.SetWidth(0.1f, 0.1f);
    }

    float[, ] get_adjacency_matrix(List<Vector3> corners) {
        float[, ] adjancenies = new float[corners.Count, corners.Count];
        for (int i = 0; i < corners.Count; i++) {
            for (int j = i + 1; j < corners.Count; j++) {
                if (Physics.Linecast(corners[i], corners[j])) {
                    adjancenies[i, j] = -1;
                    adjancenies[j, i] = -1;
                    continue;
                }                
                float dist = Vector3.Distance(corners[i], corners[j]);
                adjancenies[i, j] = dist;
                adjancenies[j, i] = dist;
                // DrawLine(corners[i], corners[j], Color.yellow);
            }
        }
        return adjancenies;
}
}
