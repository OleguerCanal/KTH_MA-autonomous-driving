using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Dijkstra {
    
    public static List<int> get_shortest_path(float[, ] adjancenies) {
        // 0 is origin
        // 1 is goal
        // others are corners
        // https://en.wikipedia.org/wiki/Dijkstra%27s_algorithm
        int n = adjancenies.GetLength(0);
        float[] dist = new float[n];
        int[] prev = new int[n];

        HashSet<int> Q = new HashSet<int>();
        for (int i = 0; i < n; i++) {
            dist[i] = Mathf.Infinity;
            prev[i] = -1;
            Q.Add(i);
        }
        dist[0] = 0;
        while (Q.Count > 0) {
            float min_dist = Mathf.Infinity;
            int u = 0;
            foreach (int i in Q) {
                if (dist[i] <= min_dist) {
                    u = i;
                    min_dist = dist[i];
                }
            }
            Q.Remove(u);
            for (int v = 0; v < n; v++) {
                if (adjancenies[u, v] > 0) { // If neighbours
                    float alt = dist[u] + adjancenies[u, v];
                    if (alt < dist[v]) {
                        dist[v] = alt;
                        prev[v] = u;
                    }
                }
            }
        }

        // Compute the path backwards
        List<int> shortest_path = new List<int>();
        int point = 1;
        while (point != 0) {
            shortest_path.Insert(0, point);
            point = prev[point];
        }
        shortest_path.Insert(0, 0);  // Add origin
        return shortest_path;
    }
}
