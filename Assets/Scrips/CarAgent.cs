using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using Barracuda;
using System.Linq;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAgent : Agent
    {
        public bool render_debug = false;
        public bool render_objects = false;
        public bool log = true;
        // Gameobjects
        public GameObject terrain_manager_game_object;
        public GameObject flag;
        public GameObject academy_gameobject;
        public float time_limit = 40;
        public int episodes_per_map = 3;
        public float checkpoint_threshold = 4; // Checkpoint area radius
        public float checkpoints_distance = 10;  // Distance between checkpoints
        public float time_scale = 100;
        public int checkpoints_window = 3; // Number of future checkpoints included in state (Remember to change state space size!!!!)
        public float lidar_range = 40;
        public float[] lidar_rays = {-45f, -22.5f, 0f, 22.5f, 45f}; // Angles of lidar rays wrp to forward direction

        private CurriculumManager curriculum_manager;
        private CarController m_Car; // the car controller we want to use

        // Classes
        private TerrainManager terrain_manager;
        private Rigidbody rBody;
        private float timer;
        private List<Vector3> augmentedPath = new List<Vector3>();
        private int next_checkpoint_idx = 1;
        private float[] accelerations = new float[] {-1, 0, 1};
        private float[] steerings = new float[] {-1, -0.8f, -0.6f, -0.4f, -0.2f, 0, 0.2f, 0.4f, 0.6f, 0.8f, 1};
        
        // Learning curve control
        private float checkpoints_reached = 0;
        private float map_diagonal = 0;
        private int episode = 0;
        private float cum_reward = 0;

        void Start () {
            Time.timeScale = time_scale;
            Time.fixedDeltaTime = 0.02f;

            m_Car = GetComponent<CarController>();
            rBody = GetComponent<Rigidbody>();
            curriculum_manager = academy_gameobject.GetComponent<CurriculumManager>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            timer = Time.time;
            terrain_manager.LoadMap("Text/terrainTrain3", false);
            VisibilityGraph visibilityGraph = terrain_manager.GetComponent<VisibilityGraph>();
            augmentedPath = GetAugmentedPath(visibilityGraph.GetPathPoints());
            float width = terrain_manager.myInfo.x_high - terrain_manager.myInfo.x_low;
            float height = terrain_manager.myInfo.z_high - terrain_manager.myInfo.z_low;
            map_diagonal = Mathf.Sqrt(Mathf.Pow(width, 2) + Mathf.Pow(height, 2));

        }

        public override void AgentReset(){
            //float success = augmentedPath.Count > 0 ? checkpoints_reached / augmentedPath.Count : 0;
            //if (episode++ % episodes_per_map == 0 || success > curriculum_manager.prop_to_pass) {
            //    bool retry = true;
            //    do {
            //        try {
            //            float map_difficulty = curriculum_manager.get_difficulty(success) + 0.1f;
            //            //Debug.Log("difficulty: "+map_difficulty);
            //            terrain_manager.SelectMapRandom(map_difficulty);
            //            //terrain_manager.LoadMap("Text/terrainTrain3", false);
            //            // For state normalization
            //            float width = terrain_manager.myInfo.x_high - terrain_manager.myInfo.x_low;
            //            float height = terrain_manager.myInfo.z_high - terrain_manager.myInfo.z_low;
            //            map_diagonal = Mathf.Sqrt(Mathf.Pow(width, 2) + Mathf.Pow(height, 2));
            //            VisibilityGraph visibilityGraph = terrain_manager.GetComponent<VisibilityGraph>();
            //            augmentedPath = GetAugmentedPath(visibilityGraph.GetPathPoints());
            //            retry = false;
            //        } catch(System.Exception e) {
            //            Debug.Log("Impossible to generate path, retry");
            //        }
            //    } while(retry);
            //}
            checkpoints_reached = 0;
            this.rBody.velocity = Vector3.zero;
            this.transform.position = terrain_manager.myInfo.start_pos;
            this.transform.rotation = Quaternion.LookRotation(augmentedPath[1] - augmentedPath[0]);  // TODO: Should we fix rotation to 0? Could be pointing wrong direction... should it be random?
            next_checkpoint_idx = 1;
            timer = Time.time;
            if (render_objects) {
                terrain_manager.DrawPath(augmentedPath, checkpoint_threshold);
            }
            cum_reward = 0;
        }

        private List<Vector3> GetAugmentedPath(List<Vector3> path_points) {  // TODO (This shouldnt be here! In terrain manager or visibuity graph)
            List<Vector3> augmentedPath = new List<Vector3>();
            for (int i = 0; i < path_points.Count - 1; i++) {
                Vector3 start = path_points[i];
                Vector3 end = path_points[i+1];
                Vector3 unit = (end - start).normalized;
                int n_new_points = Mathf.FloorToInt(Vector3.Distance(start, end) / checkpoints_distance) - 1;
                augmentedPath.Add(start);
                for (int j = 0; j < n_new_points; j++) {
                    Vector3 new_point = start + (j + 1) * checkpoints_distance * unit;
                    augmentedPath.Add(new_point);
                }
            }
            augmentedPath.Add(path_points.Last());
            return augmentedPath;
        }



        public override void CollectObservations()
        {
            // Position foreach checkpoint
            for (int i = 0; i < checkpoints_window; i++) {
                int checkpoint_idx = Mathf.Min(next_checkpoint_idx + i, augmentedPath.Count - 1);  // Repeat point if it is the last one
                Vector3 next_checkpoint_direction_relative = transform.InverseTransformDirection(augmentedPath[checkpoint_idx] - transform.position);
                AddVectorObs(next_checkpoint_direction_relative.x / map_diagonal);  // Side distance
                AddVectorObs(next_checkpoint_direction_relative.z / map_diagonal);  // Forward distance
                if (render_debug) {
                    Debug.DrawLine(transform.position, augmentedPath[checkpoint_idx], Color.yellow, 0.1f);
                }
            }

            // Agent velocity
            Vector3 velocity_relative = transform.InverseTransformDirection(rBody.velocity);
            AddVectorObs(velocity_relative.x / map_diagonal);  // Drift speed
            AddVectorObs(velocity_relative.z / map_diagonal);  // Forward speed

            // "Lidar"
            foreach (float angle in lidar_rays) {
                // float angle_rad = 0f; // Uncomment this (use to debug lidar distance)
                float angle_rad = Mathf.PI*angle/180f;
                Vector3 ray_dir = transform.TransformDirection(new Vector3(Mathf.Sin(angle_rad), 0, Mathf.Cos(angle_rad)));
                RaycastHit hit;
                Ray ray = new Ray(transform.position, ray_dir);
                Physics.Raycast(ray, out hit);
                float distance = (Mathf.Clamp(hit.distance - 2f, 0, lidar_range)) / map_diagonal;
                AddVectorObs(distance);
                if (render_debug) {
                    Debug.DrawLine(transform.position, transform.position + Mathf.Clamp(hit.distance - 2f, 0, lidar_range) * ray_dir.normalized, Color.red, 0.1f);
                }
            }
        }

        public override void AgentAction(float[] vectorAction)
        {
            float reward = -1e-3f;
            int n = Mathf.Min(checkpoints_window, augmentedPath.Count - next_checkpoint_idx);
            for (int i = 0; i < n; i++) {
                Vector3 checkpoint = augmentedPath[next_checkpoint_idx + i];
                if (Vector3.Distance(transform.position, checkpoint) < checkpoint_threshold) {  // If checkpoint activted
                    checkpoints_reached = checkpoints_reached + i + 1;
                    if (log){
                        Debug.Log("checkpoints_reached: " + checkpoints_reached.ToString());
                    }
                    if (checkpoint == augmentedPath.Last()) {  // If last checkpoint ( == checks equality with precision 1e-5)
                        reward += checkpoints_window * 0.5f + 0.5f; // Give all the remaining rewards
                        Done();
                        Debug.Log("Finish!!");
                        break;
                    } else {
                        reward += (i+1)*0.5f;
                        next_checkpoint_idx = next_checkpoint_idx + i + 1;
                        if (log) {
                            Debug.Log("Taken checkpoint "+(next_checkpoint_idx - 1));
                        }
                        break;
                    }
                }
            }
            AddReward(reward);
            cum_reward += reward;

            int steer = (int) vectorAction[0];
            int acc = (int) vectorAction[1];
            m_Car.Move(steerings[steer], accelerations[acc], accelerations[acc], 0.0f);
            //m_Car.Move(vectorAction[0], vectorAction[1], vectorAction[1], 0.0f);
            if (Time.time - timer > time_limit) {
                Done();
            }
        }

        public override float[] Heuristic()
        {
            // var action = new float[4];
            var action = new float[2];
            float steer = Input.GetAxis("Horizontal");
            float acc = Input.GetAxis("Vertical");
            action[0] = toDiscreteAction(steerings, steer);
            action[1] = toDiscreteAction(accelerations, acc);
            return action;
        }

        private float toDiscreteAction(float[] buckets, float action) {
            float discreteAction = buckets.Count() - 1;
            for (int i = 0; i < buckets.Count() - 1; i++) {
                float midpoint = (buckets[i] + buckets[i + 1]) / 2;
                if (action < midpoint) {
                    discreteAction = i;
                    break;
                }
            }
            return discreteAction;
        }
    }
}