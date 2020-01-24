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
        private CarController m_Car; // the car controller we want to use
        public GameObject terrain_manager_game_object;
        public GameObject flag;
        public VisibilityGraph visibilityGraph;

        TerrainManager terrain_manager;
        Rigidbody rBody;
        private float timer;
        public float time_limit = 30;
        public List<Vector3> augmentedPath;
        int next_checkpoint_idx = 1;
        float checkpoint_threshold = 4;
        float cum_reward = 0;
        float[] accelerations = new float[] {-1, 0, 1};
        float[] steerings = new float[] {-1, -0.8f, -0.6f, -0.4f, -0.2f, 0, 0.2f, 0.4f, 0.6f, 0.8f, 1};
        void Start () {
            m_Car = GetComponent<CarController>();
            rBody = GetComponent<Rigidbody>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            //visibilityGraph = terrain_manager.GetComponent<VisibilityGraph>(); 
            //augmentedPath =  AugmentPath();
            //augmentedPath = visibilityGraph.path_points;
            augmentedPath = terrain_manager.GenerateTrajectory(25, 25, 90);
            timer = Time.time;
            Time.timeScale = 1;
            Time.fixedDeltaTime = 0.02f;
        }

        public override void AgentReset()
        {
            this.rBody.velocity = Vector3.zero;
            this.transform.position = terrain_manager.myInfo.start_pos;
            this.transform.rotation = Quaternion.Euler(0, 0, 0);
            next_checkpoint_idx = 1;
            timer = Time.time;
            cum_reward = 0;
            augmentedPath = terrain_manager.GenerateTrajectory(25, 25, 90);
            terrain_manager.DrawPath(augmentedPath, checkpoint_threshold);
        }

        public override void CollectObservations()
        {
            float width = terrain_manager.myInfo.x_high - terrain_manager.myInfo.x_low;
            float height = terrain_manager.myInfo.z_high - terrain_manager.myInfo.z_low;
            float diagonal = Mathf.Sqrt(Mathf.Pow(width, 2) + Mathf.Pow(height, 2));

            Vector3 next_checkpoint = augmentedPath[next_checkpoint_idx];
            Vector3 next_checkpoint_direction = next_checkpoint - transform.position;
            Vector3 next_checkpoint_direction_relative = transform.InverseTransformDirection(next_checkpoint_direction);
            AddVectorObs(next_checkpoint_direction_relative.x / diagonal);
            AddVectorObs(next_checkpoint_direction_relative.z / diagonal);
            
            Vector3 velocity_relative = transform.InverseTransformDirection(rBody.velocity);
            // Agent velocity
            AddVectorObs(velocity_relative.x / diagonal);
            AddVectorObs(velocity_relative.z / diagonal);
            //terrain_manager.DrawLine(transform.position, augmentedPath[next_checkpoint_idx], Color.red);
        }


        public override void AgentAction(float[] vectorAction)
        {
            //float steer = vectorAction[0];
            //float acc = vectorAction[1];
            
            Vector3 next_checkpoint = augmentedPath[next_checkpoint_idx];
            if (Vector3.Distance(transform.position, next_checkpoint) < checkpoint_threshold) {
                if (next_checkpoint_idx == augmentedPath.Count - 1) {
                    Debug.Log("Finished!");
                    SetReward(1.0f);
                    cum_reward += 1.0f;
                    Done();
                } else {
                    Debug.Log("Reached checkpoint num "+next_checkpoint_idx);
                    SetReward(0.5f);
                    next_checkpoint_idx ++;
                    cum_reward += 0.5f;
                }
                //Debug.Log("Checkpoint reached! Next checkpoint "+next_checkpoint_idx);                
            } else {
                SetReward(-1e-3f);
                cum_reward -= 1e-3f;
            }

            if (Time.time - timer > time_limit) {
                Done();
            }

            // Elements of vectorAction[] start from 1
            int steer = (int) vectorAction[0];
            int acc = (int) vectorAction[1];
            m_Car.Move(steerings[steer], accelerations[acc], accelerations[acc], 0.0f);
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
