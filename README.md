# 🎮 Teporaca - Core Mechanics & Systems (C# Repository)

Welcome to the **Teporaca** mechanics repository. This space serves as a technical portfolio showcasing the architecture, logic, and core systems I implemented in C# during the game's development.

> **Note:** This repository exclusively contains C# scripts authored by me, along with visual reference material. Full project assets, 3D models, and the complete engine project remain private to protect the startup's intellectual property.

## 🌍 Project Context

**Teporaca** is a commercial indie title currently in development. The project has a solid track record, having been officially showcased at the **Game Conference MX 2025** in Guadalajara and awarded a one-year incubation scholarship at **Tecnológico de Monterrey** following a game jam victory.

As the **Technical Lead** and main programmer, my primary focus in architecting these systems was to ensure scalable, clean, and highly optimized code that allows for rapid iteration without sacrificing performance.

## 🛠️ Technologies & Architecture

* **Language:** C#
* **Engine:** Unity 6000.2.14f1
* **Input:** Unity New Input System
* **Core Architectural Patterns:** Finite State Machines (FSM), Observer Pattern (Event-driven), Singleton, and Data-Driven Design via ScriptableObjects.

## ⚙️ Highlighted Mechanics & Architecture

Here is a breakdown of the core systems you can explore in this repository. These scripts showcase a strong focus on modularity, decoupling, and performance optimization.

### 1. Data-Driven Melee Combat
* **Core Files:** `MeeleFighter.cs`, `AttackData.cs`, `CombatController.cs`
* **Description:** A comprehensive combat system utilizing ScriptableObjects (`AttackData`) to define attack properties such as animations, impact timings, and movement behavior during attacks. The `MeeleFighter` acts as a State Machine handling attack phases (Windup, Impact, Cooldown) via Coroutines.
* **Technical Highlight:** Hitboxes are strictly synced with animation frames using custom timing variables (`ImpactStartTime` and `ImpactEndTime`). I also implemented an i-frame system during dodges and a stamina management system that dictates the flow of combat.

### 2. Event-Driven UI & Decoupling
* **Core File:** `UI_PlayerStats.cs`
* **Description:** The player's HUD (Health and Stamina) is completely decoupled from the logic scripts. 
* **Technical Highlight:** Utilized C# `Action` events (`OnHealthChanged`, `OnStaminaChanged`) inside the `MeeleFighter` script. The UI simply subscribes to these events on `Start()` and unsubscribes on `OnDestroy()`, ensuring zero performance overhead from `Update()` polling and preventing tight coupling between game logic and presentation.

### 3. AI State Machine & Group Management
* **Core Files:** `EnemyController.cs`, `EnemyManager.cs`, `VisionSensor.cs`
* **Description:** Enemy logic is divided between individual AI behavior and a centralized manager. The `EnemyController` uses a Dictionary-based Finite State Machine (FSM) to transition seamlessly between states like Idle, CombatMovement, and Attack.
* **Technical Highlight:** The `EnemyManager` handles group combat dynamics. Instead of all enemies attacking simultaneously, the manager orchestrates attack turns using timers and evaluates the closest threat based on the camera's targeting direction, ensuring a balanced and readable combat experience for the player.

### 4. Input & Locomotion
* **Core Files:** `PlayerControllerE.cs`, `CameraControllerE.cs`
* **Description:** Movement and camera logic tightly integrated with Unity's New Input System.
* **Technical Highlight:** The code dynamically handles Root Motion toggling. For example, during a dodge, it evaluates if the player is locked onto an enemy; if so, it handles movement via code to maintain spatial positioning, otherwise, it relies on Root Motion for animation accuracy.

## 📸 Visual Showcase

<img width="2560" height="1366" alt="menu" src="https://github.com/user-attachments/assets/1930cbe2-4204-4a5e-b769-273ebc7beedb" />

<img width="2560" height="1440" alt="Captura de pantalla 2026-02-27 160312" src="https://github.com/user-attachments/assets/64b6f453-6aef-4605-9a07-86e19d079610" />

<img width="2560" height="1440" alt="Captura de pantalla 2026-02-27 160332" src="https://github.com/user-attachments/assets/0704492e-71cb-494e-ac16-edc36c928ad5" />

<img width="2560" height="1440" alt="Captura de pantalla 2026-02-27 160232" src="https://github.com/user-attachments/assets/b3af7df4-67af-40a9-b02d-37392a8a3aff" />

https://github.com/user-attachments/assets/a7e24655-2a18-4e4a-b5ff-c2d77a979574

https://github.com/user-attachments/assets/4c3f4fbf-c57c-4a99-b283-8c56bc3685d2

https://github.com/user-attachments/assets/8d2e1b3a-5da4-4856-9de7-e5d06607a444

## 📬 Contact & Links

* **LinkedIn:** www.linkedin.com/in/ivan-eduardo-granillo-torres-52a927302 
* **Teporaca Info:** https://cuackerstudios.itch.io/teporaca 
