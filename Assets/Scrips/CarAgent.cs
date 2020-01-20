using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAgent : Agent
    {
        private CarController m_Car; // the car controller we want to use
        public GameObject terrain_manager_game_object;

        TerrainManager terrain_manager;
        Rigidbody rBody;
        private float timer;
        public float time_limit = 20;
        void Start () {
            m_Car = GetComponent<CarController>();
            rBody = GetComponent<Rigidbody>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            timer = Time.time;
        }

        public override void AgentReset()
        {
            if (Time.time - timer > time_limit) // TODO(oleguer): Add colision here
            {
                this.rBody.velocity = Vector3.zero;
                this.transform.position = terrain_manager.myInfo.start_pos;
                this.transform.rotation = Quaternion.Euler(0, 0, 0);
                timer = Time.time;
            }
        }

        public override void CollectObservations()
        {
            // Target and Agent positions, velocity
            AddVectorObs(terrain_manager.myInfo.goal_pos);
            AddVectorObs(this.transform.position);

            // Agent velocity
            AddVectorObs(rBody.velocity.x);
            AddVectorObs(rBody.velocity.z);
        }


        public override void AgentAction(float[] vectorAction)
        {
            // m_Car.Move(vectorAction[0], vectorAction[1], vectorAction[2], vectorAction[3]);
            m_Car.Move(vectorAction[0], vectorAction[1], 0.0f, 0.0f);

            // Rewards
            float distanceToTarget = Vector3.Distance(this.transform.position,
                                                    terrain_manager.myInfo.goal_pos);

            // SetReward(-distanceToTarget);


            // Reached target
            if (distanceToTarget < 3f)
            {
                Debug.Log("Done!");
                SetReward(999999.0f);
                Done();
            }

            // Timer
            if (Time.time - timer > time_limit)
            {
                Debug.Log("Time Limit");
                // SetReward(-0.01f);  // Discount for time?
                SetReward(-100);
                Done();
            }

        }

        public override float[] Heuristic()
        {
            // var action = new float[4];
            var action = new float[2];
            action[0] = Input.GetAxis("Horizontal");
            action[1] = Input.GetAxis("Vertical");
            // action[2] = 0;
            // action[3] = 0;
            return action;
        }
    }
}