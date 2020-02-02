using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public class CurriculumManager : using UnityEngine;

public class CurriculumManager : MonoBehaviour {
    public float prop_to_pass = 0.75f; // Proportion of checkpoints to go through to pass next difficulty

    public float[] map_difficulties = {0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f};

    // TODO(oleguer): Put more parameters such as checkpount distance, random rotation...

    private int current_level = 0;

    private int counter = 0;

    public float get_difficulty(float prop_of_flags) {
        if (prop_of_flags >= prop_to_pass) {
            if (counter++ > 5) {
                current_level = Mathf.Min(current_level + 1, map_difficulties.Length-1);
                Debug.Log("Difficulty increased to " + map_difficulties[current_level].ToString() + " (flag prop: " + prop_of_flags.ToString() + ")");
                counter = 0;
            }
        }
        return map_difficulties[current_level];
    }
}