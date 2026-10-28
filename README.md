# Autonomous Car with Reinforcement Learning

A senior design (graduation) project in which a car learns to drive an S-shaped road on its own, avoiding walls and moving obstacles, using **Unity ML-Agents** and **Proximal Policy Optimization (PPO)**.

The project also compares:

- **Curriculum learning vs. single-run training** (PPO)
- **PPO vs. SAC**
- **Single-environment vs. parallel (4-environment) training**
- **Generalization**: does the agent drive, or has it memorized the track?

---

## Table of Contents

1. [Repository Structure]
2. [Requirements]
3. [Installation]
4. [Running the Trained Model (Inference)]
5. [Training From Scratch]
6. [Curriculum Learning Procedure]
7. [Project Details]
8. [Results Summary]
9. [Third-Party Assets]
10. [Author]

---

## Repository Structure

```
Senior-Design-Project_Autonomous-Car-with-RL/
├── Project Files/
│   ├── config/                      # ML-Agents trainer configuration files
│   │   ├── CarBehavior.yaml             # PPO (single environment)
│   │   ├── CarBehavior_SAC.yaml         # SAC
│   │   └── CarBehavior_Parallel.yaml    # PPO (4 parallel environments)
│   ├── results/                     # Trained models (.onnx only)
│   └── Unity Project/
│       └── Senior Design Project/   # Open this folder with Unity Hub
│           ├── Assets/
│           ├── Packages/
│           └── ProjectSettings/
└── Report and Results/              # Final report, model comparisons
```

> Intermediate checkpoints (`.pt`), TensorBoard run folders and the Windows build are **not** included. Unity's `Library/`, `Temp/` and `Logs/` folders are regenerated automatically when the project is opened.

---

## Requirements

### Software

| Component | Version used |
|---|---|
| Unity | **6.3 LTS (6000.3.11f1)**, Universal 3D template |
| ML-Agents (Unity package) | **4.0.2** (`com.unity.ml-agents`) |
| ML-Agents (Python package) | **1.1.0** (`mlagents`) |
| Python | **3.10.12** |
| PyTorch | **2.2.1** with CUDA 12.1 (`cu121`) |
| Conda | Miniconda |
| IDE | Visual Studio |
| OS | Windows 11 |

### Hardware used for development

- NVIDIA RTX 3080 Ti, Intel i9-11900K, 32 GB RAM

A CUDA-capable GPU is recommended but not required. To train on CPU, skip the CUDA PyTorch install and use the default CPU build.

> **Version compatibility matters.** The Unity package and the Python package must be compatible with each other. If you use different versions, check the official ML-Agents release notes first. Mismatched versions are the most common cause of connection errors.

---

## Installation

### 1. Clone the repository

```bash
git clone https://github.com/<your-username>/Senior-Design-Project_Autonomous-Car-with-RL.git
cd Senior-Design-Project_Autonomous-Car-with-RL
```

### 2. Install Unity

1. Install **Unity Hub** and add editor version **6000.3.11f1** (6.3 LTS).
2. In Unity Hub choose **Add → Add project from disk**.
3. Select `Project Files/Unity Project/Senior Design Project`.
4. Let Unity import everything. The first import takes a few minutes.

The ML-Agents package is listed in `Packages/manifest.json` and installs automatically. If it does not, open **Window → Package Manager → + → Add package by name** and enter:

```
com.unity.ml-agents
```

### 3. Create the Python environment

Open Miniconda Prompt:

```bash
conda create --prefix <path-to-env>\mlagents python=3.10.12
conda activate <path-to-env>\mlagents
```

(Alternatively use `conda create -n mlagents python=3.10.12` to keep it in the default location.)

### 4. Install PyTorch (CUDA 12.1)

```bash
pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121
```

### 5. Install ML-Agents

```bash
python -m pip install mlagents==1.1.0
```

### 6. Verify the installation

```bash
mlagents-learn --help
```

If the list of options is printed, the setup works. A `pkg_resources is deprecated` warning is harmless.

---

## Running the Trained Model (Inference)

This needs only Unity. No Python is required.

1. Open the project in Unity and open the main scene (`Assets/Scenes/SampleScene`).
2. Select the **car** object (`car02`) in the Hierarchy.
3. In **Behavior Parameters** set:
   - **Behavior Name:** `CarBehavior`
   - **Behavior Type:** `Inference Only`
   - **Model:** drag a `.onnx` file from `Project Files/results/` into this field
4. Press **Play**.

The on-screen UI shows episode count, speed, current reward, best reward, laps and collisions.

> The model file must match the observation and sensor setup in the scene (see [Project Details](#project-details)). If you change the sensors or observations, retrain the model.

### Manual driving (for testing the scene)

Set **Behavior Type** to `Heuristic Only` and drive with **W / A / S / D**.

---

## Training From Scratch

### Before you start

1. Select the car and set **Behavior Parameters → Behavior Type** to `Default`.
2. Make sure **Behavior Name** is `CarBehavior`. It must match the name in the YAML files.
3. Set the obstacle speeds you want (see [Curriculum Learning Procedure](#curriculum-learning-procedure)).
4. **Do not press Play yet.** Start the trainer first.

### PPO (single environment)

```bash
conda activate <path-to-env>\mlagents
mlagents-learn "<path-to>/Project Files/config/CarBehavior.yaml" --run-id=PPO_Run1
```

Wait for:

```
[INFO] Listening on port 5004. Start training by pressing the Play button in the Unity Editor.
```

Then press **Play** in Unity.

### SAC

```bash
mlagents-learn "<path-to>/Project Files/config/CarBehavior_SAC.yaml" --run-id=SAC_Run1
```

### Parallel training (4 environments)

The scene contains several copies of the `Environment` object placed side by side, all sharing one policy:

```bash
mlagents-learn "<path-to>/Project Files/config/CarBehavior_Parallel.yaml" --run-id=Parallel_Run1 --num-envs=1
```

`--num-envs=1` is correct here. The parallel cars live **inside one Unity instance**, so the trainer only needs to connect to one environment. (`--num-envs=N` would launch N separate Unity executables.)

### Resuming / fine-tuning from a previous run

```bash
mlagents-learn "<path-to>/Project Files/config/CarBehavior.yaml" --run-id=NewRun --initialize-from=PreviousRun
```

`--initialize-from` needs the previous run's checkpoint folder in the `results/` directory. Only the `.onnx` files are shipped in this repository, so to reproduce the curriculum chain you have to train the stages in order yourself.

### Monitoring with TensorBoard

```bash
tensorboard --logdir results
```

Then open `http://localhost:6006`.

### Where results are saved

`mlagents-learn` writes to a `results/` folder **relative to the directory you run it from**. Run it from a folder of your choice and use `--results-dir` if you want a different location.

---

## Curriculum Learning Procedure

Curriculum learning was the most effective approach. The agent is trained in stages. Each stage starts from the previous one with `--initialize-from`. Only the obstacle speeds change between stages. The observation and sensor setup stays identical, which is required for the network shapes to match.

Obstacle speed is set per obstacle through the `moveSpeed` field of the `ObstacleMovement` script (`0` means static).

| Stage | Run ID | Obstacles | Speeds (Obstacle 1 → 5) | Initialized from |
|---|---|---|---|---|
| 1 | `CurriculumRun1` | Disabled (learn the road) | n/a | scratch |
| 2 | `CurriculumRun2` | Static | 0 0 0 0 0 | `CurriculumRun1` |
| 3 | `CurriculumRun3` | Partly moving | 0 0 0 1 1 | `CurriculumRun2` |
| 4 | `CurriculumRun4` | Moving | 1 1 1 2 2 | `CurriculumRun3` |
| 5 | `CurriculumRun5` | Moving (final) | 1 1 2 3 3 | `CurriculumRun4` |

Example for stage 2:

```bash
mlagents-learn "<path-to>/Project Files/config/CarBehavior.yaml" --run-id=CurriculumRun2 --initialize-from=CurriculumRun1
```

For stage 1, deactivate the five `Obstacle` objects in the Hierarchy (untick the checkbox at the top of the Inspector) and re-enable them for stage 2.

**Baselines trained for comparison** (obstacle speeds 1 1 2 3 3, no curriculum, from scratch): `PPO_Run1`, `SAC_Run1`.

---

## Project Details

### Scene

- S-shaped road built from 5 planes
- Boundary walls (tag `Wall`)
- 5 waypoints (tag `Waypoint`, trigger colliders, must be passed in order)
- 1 finish line (tag `Finish`, trigger collider)
- 5 obstacles (tag `Obstacle`, moved by `ObstacleMovement`)
- Low-poly car model with four separately rotating wheels
- Canvas UI (episode, speed, reward, best reward, laps, collisions)

### Agent setup

| Component | Setting |
|---|---|
| Rigidbody | mass 1, drag 0.5, angular drag 5, freeze rotation X and Z |
| Collider | Box Collider |
| Behavior Parameters | Name `CarBehavior`, Space Size **8**, **2** continuous actions |
| Decision Requester | Decision Period 5 |
| Front Ray Perception Sensor 3D | 4 rays per direction (9 rays), max angle 60°, length 25 |
| Rear Ray Perception Sensor 3D | 2 rays per direction (5 rays), max angle 30°, length 15 |
| Detectable tags | `Wall`, `Obstacle`, `Waypoint`, `Finish` |

**Actions (continuous):**

1. Throttle / brake (−1 to 1)
2. Steering (−1 to 1)

Movement includes acceleration, braking and friction. Steering strength depends on speed, so the car cannot turn while standing still. Defaults: `maxSpeed = 10`, `accelerationForce = 5`, `brakeForce = 8`, `turnSpeed = 100`.

**Vector observations (8):**

- Velocity (x, z)
- Forward direction (x, z)
- Direction to the current target (x, z)
- Dot product between the car's right vector and the target direction (target is left or right)
- Normalized distance to the target

The current target is the next waypoint, and once all waypoints are passed, the finish line. Ray sensor observations are added automatically and are **not** counted in Space Size.

### Reward function

| Event | Reward |
|---|---|
| Moving closer to the current target | `+ distance delta × 0.1` per step |
| Passing the correct next waypoint | `+0.3` |
| Reaching the finish line | `+2.0` (episode ends) |
| Hitting a wall | `−0.5` (episode ends) |
| Hitting an obstacle | `−1.0` (episode ends) |

### Scripts (`Assets/Scripts`)

| Script | Purpose |
|---|---|
| `CarAgent.cs` | ML-Agents agent: observations, actions, rewards, episode logic |
| `ObstacleMovement.cs` | Moves an obstacle back and forth; `moveSpeed = 0` makes it static |
| `WheelRotation.cs` | Rotates the wheel meshes according to the car's speed |
| `CarUI.cs` | On-screen statistics |

### Trainer configurations

| | PPO | SAC | PPO (parallel) |
|---|---|---|---|
| File | `CarBehavior.yaml` | `CarBehavior_SAC.yaml` | `CarBehavior_Parallel.yaml` |
| Max steps | 1,000,000 | 1,000,000 | 1,000,000 |
| Batch size | 512 | 256 | 2048 |
| Buffer size | 8192 | 50,000 | 32,768 |
| Learning rate | 3e-4 | 3e-4 | 3e-4 |
| Network | 3 × 256, normalized | 3 × 256, normalized | 3 × 256, normalized |
| Gamma | 0.99 | 0.99 | 0.99 |

---

## Results Summary

Full numbers, graphs and discussion are in the `Report and Results/` folder.

- **Curriculum PPO (`CurriculumRun5`)** gave the best and most stable driving, including on a modified track.
- **Single-run PPO (`PPO_Run1`)**, trained directly with all moving obstacles, performed clearly worse.
- **SAC (`SAC_Run1`)** learned the slowest and generalized worst under the same conditions.
- **Parallel training** with 4 environments did not give the expected speed-up in this setup. A likely reason is the noise added by the moving obstacles.
- **Generalization test:** with the car and finish positions moved and the obstacle speeds changed, the curriculum model came close to the finish line. The single-run PPO model reached about half of the track, and the SAC model drove backward.

---

## Third-Party Assets

The car model comes from the Unity Asset Store.

- **Asset name:** `<asset name>`
- **Publisher:** `<publisher>`
- **License:** subject to the Unity Asset Store EULA

`<Replace the placeholders above, or remove the asset from the repository and describe how to obtain it.>`

---

## Author

**Author:** `<Mehmet Efe KAPISIZ>`
Senior Design Project, `<University of Turkish Aeronautical Association / Computer Engineering>`, `<2026>`