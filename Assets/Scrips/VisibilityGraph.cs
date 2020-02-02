using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VisibilityGraph : MonoBehaviour {

    public float margin = 4;
    public GameObject terrain_manager_game_object;
    
	// Use this for initialization
	void Start () {
	}


    public List<Vector3> GetCorners() {
        TerrainManager terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        TerrainInfo myInfo = terrain_manager.myInfo;
        float[,] traversability = myInfo.GetPaddedTraversability();
        float x_step = (myInfo.x_high - myInfo.x_low) / myInfo.x_N;
        float z_step = (myInfo.z_high - myInfo.z_low) / myInfo.z_N; 
        float y = myInfo.start_pos.y;
        int rows = traversability.GetLength(0);
        int cols = traversability.GetLength(1);
        List<Vector3> valid_corners = new List<Vector3>();

        for (int r = 0; r < rows; r++) {
            if (r == 0 || r == rows - 1) { continue; } // Skip row padding
            for (int c = 0; c < cols; c++) {
                if (c == 0 || c == cols - 1) { continue; } // Skip column padding
                if (traversability[r, c] == 0.0f) { continue; } // Empty cells don't have borders
                // A corner of a cell in a given direction is valid iff
                // - The cell that contains it and the two adjacent cells that also touch the generating cell are free
                // - The cell that contains it is free and the two adjacent cells that also touch the generating cell are full
                int[] corner_steps = new int[] {-1, -1, +1, +1, -1};
                int[] adjacent_steps = new int[] {0, -1, 0, +1, 0, -1};
                for (int i = 0; i < 4; i++) {
                    if (traversability[r + corner_steps[i], c + corner_steps[i + 1]] == 1.0f) { continue; } // Corner is in a full cell
                    // The two adjacent cells that also touch the generating cell are
                    Cell c1 = new Cell(r + adjacent_steps[i], c + adjacent_steps[i + 1]);
                    Cell c2 = new Cell(r + adjacent_steps[i + 1], c+ adjacent_steps[i + 2]);
                    if ((traversability[c1.row, c1.col] == 0.0f && traversability[c2.row, c2.col] == 0.0f)
                        || (traversability[c1.row, c1.col] == 0.0f && traversability[c2.row, c2.col] == 0.0f)) { // Both free or both full
                        Vector3 center = new Vector3(myInfo.x_low + (r - 1 + 0.5f) * x_step, y, myInfo.z_low + (c - 1 + 0.5f) * z_step);
                        float x = center.x + corner_steps[i] * (x_step / 2 + margin / Mathf.Sqrt(2));
                        float z = center.z + corner_steps[i + 1] * (z_step / 2 + margin / Mathf.Sqrt(2));
                        Vector3 corner = new Vector3(x, y, z);
                        valid_corners.Add(corner);
                    }
                }
            }
        }
        return valid_corners;
    }

    public List<Vector3> GetPathPoints() {
        // float time = Time.time;
        TerrainManager terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        List<Vector3> path_points = new List<Vector3>();
        List<Vector3> corners = new List<Vector3>();
        corners.Add(terrain_manager.myInfo.start_pos);
        corners.Add(terrain_manager.myInfo.goal_pos);
        corners.AddRange(GetCorners());

        float[, ] adjancenies = get_adjacency_matrix(corners);
        List<int> path_indexes = Dijkstra.get_shortest_path(adjancenies);
        foreach (int path_index in path_indexes) {
            path_points.Add(corners[path_index]);
            if (path_index > 0) {
                //DrawLine(path_points[path_points.Count-1], path_points[path_points.Count - 2], Color.yellow);
            }
        }
        return path_points;
    }
	
	// Update is called once per frame
	void Update () {
	}

    float[, ] get_adjacency_matrix(List<Vector3> corners) {
        float[, ] adjancenies = new float[corners.Count, corners.Count];
        for (int i = 0; i < corners.Count; i++) {
            for (int j = i + 1; j < corners.Count; j++) {
                Vector3 direction = corners[j] - corners[i];
                Vector3 normal = new Vector3(-direction.z, direction.y, direction.x).normalized;
                float step = (margin - 0.1f) / 2;
                int[] signs = new int[] {-1, 0, 1};
                bool free = true;
                foreach (int sign in signs) {
                    if (Physics.Linecast(corners[i] + sign * step * normal, corners[j] + sign * step * normal)) {
                        adjancenies[i, j] = -1;
                        adjancenies[j, i] = -1;
                        free = false;
                    }
                }
                if (free) {
                    float dist = Vector3.Distance(corners[i], corners[j]);
                    adjancenies[i, j] = dist;
                    adjancenies[j, i] = dist;
                }
            }
        }
        return adjancenies;
    }
}
