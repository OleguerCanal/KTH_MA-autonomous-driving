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


        // public float speed = 10;
        public override void AgentAction(float[] vectorAction)
        {
            // Actions, size = 2
            // Vector3 controlSignal = Vector3.zero;
            // controlSignal.x = vectorAction[0];
            // controlSignal.z = vectorAction[1];
            // rBody.AddForce(controlSignal * speed);
            m_Car.Move(vectorAction[0], vectorAction[1], vectorAction[2], vectorAction[3]);

            // Rewards
            float distanceToTarget = Vector3.Distance(this.transform.position,
                                                    terrain_manager.myInfo.goal_pos);

            // SetReward(-0.01f);  // Discount for time?
            // SetReward(distanceToTarget);

            // Reached target
            if (distanceToTarget < 1.42f)
            {
                Debug.Log("Done!");
                SetReward(1000.0f);
                Done();
            }

            // Timer
            if (Time.time - timer > time_limit)
            {
                Debug.Log("Time Limit");
                // SetReward(-0.01f);  // Discount for time?
                SetReward(-distanceToTarget);
                Done();
            }

        }

        public override float[] Heuristic()
        {
            var action = new float[4];
            action[0] = Input.GetAxis("Horizontal");
            action[1] = Input.GetAxis("Vertical");
            action[2] = 0;
            action[3] = 0;
            return action;
        }
    }
}