using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
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
        private CarController m_Car; // the car controller we want to use
        public GameObject terrain_manager_game_object;
        public GameObject flag;
        public float time_limit = 40;
        public int episodes_per_map = 3;
        public float checkpoint_threshold = 4; // Checkpoint area radius
        public float checkpoints_distance = 10;  // Distance between checkpoints
        public float time_scale = 100;
        public int checkpoints_window = 3; // Number of future checkpoints included in state (Remember to change state space size!!!!)
        public float lidar_range = 40;
        public float[] lidar_rays = {-45f, -22.5f, 0f, 22.5f, 45f}; // Angles of lidar rays wrp to forward direction

        // Classes
        private TerrainManager terrain_manager;
        private Rigidbody rBody;
        private float timer;

        // Path variables
        private List<Vector3> augmentedPath;
        private int next_checkpoint_idx = 1;

        private float[] accelerations = new float[] {-1, 0, 1};
        private float[] steerings = new float[] {-1, -0.8f, -0.6f, -0.4f, -0.2f, 0, 0.2f, 0.4f, 0.6f, 0.8f, 1};
        private float cum_reward = 0;
        private float map_diagonal = 0;
        private int episode = 0;
        
        void Start () {
            m_Car = GetComponent<CarController>();
            rBody = GetComponent<Rigidbody>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            timer = Time.time;
            Time.timeScale = time_scale;
            Time.fixedDeltaTime = 0.02f;

            // For state normalization
            float width = terrain_manager.myInfo.x_high - terrain_manager.myInfo.x_low;
            float height = terrain_manager.myInfo.z_high - terrain_manager.myInfo.z_low;
            map_diagonal = Mathf.Sqrt(Mathf.Pow(width, 2) + Mathf.Pow(height, 2));
        }

        public override void AgentReset(){
            if (episode++ % 3 == 0) {
                bool retry = true;
                do {
                    try {
                        terrain_manager.SelectMapRandom(0.5f);
                        VisibilityGraph visibilityGraph = terrain_manager.GetComponent<VisibilityGraph>();
                        augmentedPath =  GetAugmentedPath(visibilityGraph.GetPathPoints());
                        retry = false;
                    } catch (System.Exception e) {
                        if (log) {
                            Debug.Log("Impossible to generate path, retry");
                        }
                    }
                } while(retry);
            }
            this.rBody.velocity = Vector3.zero;
            this.transform.position = terrain_manager.myInfo.start_pos;
            this.transform.rotation = Quaternion.LookRotation(augmentedPath[1] - augmentedPath[0]);  // TODO: Should we fix rotation to 0? Could be pointing wrong direction... should it be random?
            next_checkpoint_idx = 1;
            timer = Time.time;
            cum_reward = 0;
            if (render_objects) {
                terrain_manager.DrawPath(augmentedPath, checkpoint_threshold);
            }
        }

        private List<Vector3> GetAugmentedPath(List<Vector3> path_points) {
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
                Vector3 ray_dir = transform.TransformDirection(new Vector3(lidar_range*Mathf.Sin(angle_rad), 0, lidar_range*Mathf.Cos(angle_rad)));
                RaycastHit hit;
                Ray ray = new Ray(transform.position, ray_dir);
                Physics.Raycast(ray, out hit);
                float distance = (Mathf.Clamp(hit.distance - 2f, 0, lidar_range)) / map_diagonal;
                AddVectorObs(distance);
                if (render_debug) {
                    Debug.DrawLine(transform.position, transform.position + hit.distance * ray_dir.normalized, Color.red, 0.1f);
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
                    if (checkpoint == augmentedPath.Last()) {  // If last checkpoint ( == checks equality with precision 1e-5)
                        reward += checkpoints_window*0.5f + 0.5f; // Give all the remaining rewards
                        Done();
                        if (log) {
                            Debug.Log("Finish!!");
                        }
                        break;
                    } else {
                        reward += (i+1)*0.5f;
                        next_checkpoint_idx = next_checkpoint_idx + i + 1;
                        if (log){
                            Debug.Log("Taken checkpoint "+(next_checkpoint_idx - 1));
                        }
                        break;
                    }
                }
            }
            AddReward(reward);

            // Elements of vectorAction[] start from 1
            int steer = (int) vectorAction[0];
            int acc = (int) vectorAction[1];
            m_Car.Move(steerings[steer], accelerations[acc], accelerations[acc], 0.0f);
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
