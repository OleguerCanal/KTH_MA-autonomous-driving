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
            //if (Time.time - timer > time_limit) // TODO(oleguer): Add colision here
            //{
            this.rBody.velocity = Vector3.zero;
            this.transform.position = terrain_manager.myInfo.start_pos;
            this.transform.rotation = Quaternion.Euler(0, 0, 0);
            timer = Time.time;
            //}
            tot_steer = 0;
            tot_acc = 0;
            nsteer = 0;
            nacc = 0;
        }

        public override void CollectObservations()
        {
            // Target and Agent positions, velocity
            Vector3 goal_relative = this.transform.InverseTransformPoint(terrain_manager.myInfo.goal_pos);
            AddVectorObs(goal_relative.x);
            AddVectorObs(goal_relative.z);

            // Agent velocity
            AddVectorObs(rBody.velocity.x);
            AddVectorObs(rBody.velocity.z);
        }

        float tot_steer = 0;
        float tot_acc = 0;
        int nsteer = 0;
        int nacc = 0;
        public override void AgentAction(float[] vectorAction)
        {
            float steer = vectorAction[0];
            float acc = vectorAction[1];
            tot_steer+=steer;
            tot_acc+=acc;
            nsteer++;
            nacc++;
            Debug.Log("steer: "+steer+"     acc: "+acc);
            Debug.Log("avg steer: "+(tot_steer/nsteer)+"    avg acc: "+(tot_acc/nacc));
            
            // m_Car.Move(vectorAction[0], vectorAction[1], vectorAction[2], vectorAction[3]);
            m_Car.Move(steer, acc, acc, 0.0f);

            float distanceToTarget = Vector3.Distance(this.transform.position,
                                                    terrain_manager.myInfo.goal_pos);

            // Reached target
            if (distanceToTarget < 3f)
            {
                SetReward(1.0f);
                Debug.Log("Done!");
                Done();
            }
            // Timer
            if (Time.time - timer > time_limit)
            {
                Debug.Log("Time Limit");
                Done();
            }
            SetReward(-0.01f * Mathf.Atan(distanceToTarget));

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
