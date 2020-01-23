import gym

from gym_unity.envs import UnityEnv
from stable_baselines.deepq.policies import MlpPolicy
from stable_baselines import DQN
import numpy as np

def main():
    env = UnityEnv("./envs/BuildTest.x86_64",
                   np.random.randint(0, 1000),
                   flatten_branched=True,
                   no_graphics=False)
    model = DQN(MlpPolicy, env, verbose=1, tensorboard_log='./logs/')
    model.learn(total_timesteps=10000000)



if __name__ == '__main__':
    main()
