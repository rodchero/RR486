# 🎮 Tabletop Tanks
Project for CISC486 by Roman Rodchenkov

# ASSIGNMENT 4 INFORMATION 

A4 Video Link:  
https://youtu.be/GryNfTKoCCw  

No build available; all gameplay is run via the editor "play" mode.  

## Very Local Multiplayer (2 unity instances on one machine)
- 1: Create a ParrelSync clone project: Top Navigation Bar ("ParrelSync") -> Clones Manager -> Create new clone
- 2: Open clone (2nd instance of unity editor): Clones Manager -> Open In New Editor (clone 0) (I recommend "split-screening" both editor instances as in my video)
- 3: Start host and clone in editor (play), click "2 Players" button on both (main menu), click start on the host (original editor)
- 4: "Enjoy" the "seamless" multiplayer experience


## OR Regular Local Multiplayer (over same internal network)
- 1: Clone a4 branch onto 2 computers. Decide which one is host and which one is client (arbitrary).
- 2: Setup Host: on the host computer, find out its local IP address, and put that into the network host field (Menu Scene -> Network Manager (GameObject) -> UDP Transport (Component) -> Client Settings -> Address (field)).
- 3: On the host computer, make sure firewall is disabled, or add a rule to prevent connection blocking (for me, i did "sudo ufw disable" for the demonstration, but this is platform-specific and may not affect you). The server runs on port 5050 by default (see parameters in Network Manager).
- 4: Set client flags: On client computer, go to network manager component as before and set the "Start Server Flags" to "None". You should see a message "During play mode this instance will start as a CLIENT". (Menu Scene -> Network Manager (GameObject) -> Network Manager (script) -> Start Server Flags (parameter). Uncheck all except "None"). 
- 5: Start Host and Client games (play button in the editor) and follow steps 3&4 from "Very Local Multiplayer" Section

## OR Singleplayer
- Might be a little broken since adding network functionality 
- Make sure editor instance is set as Host (do opposite of step 4 for Local Multiplayer)
- Singleplayer should work as before (a3)


## 📌 Overview
Tabletop Tanks is a 3-d survival/action game in which the player controls a miniature tank, and must clear progressively harder levels that include enemies and obstacles. Inspired by the Wii Play "Tanks!" game.

## 🕹️ Core Gameplay
Player controls the "player tank" from a top-down view, and:  
- shoots shells, drives around, and places landmines to destroy enemy tanks [no landmines yet]
- avoids environmental hazards and enemy shells, rockets, and mines to survive
- Win by being the last (allied*) tank alive
- Lose if your tank is destroyed by the enemy

## 👥 Player Setup
- Single player as the player tank 
- Optional co-op mode where a second player controls an allied tank and helps clear levels  

## 🤖 AI Design [2 types implemented so far]  
Several (4 to 8) types of enemy tanks with different behaviour, projectiles, movespeed, health, etc  

### EnemyMovementFSM
The current setup uses 2 FSM that separately handle shooting and moving (some enemies do not move)  
#### Idle
- Actions: Nothing  
- Transitions: If player gets close enough (50m), Chase or Flank (random selection)  
#### Chase
- Actions: Moves towards player and around obstacles (navmesh navigation)  
- Transitions: If player exceeds activation distance (50m), Idle  
#### Flank
- Actions: Moves towards a point behind/to the side of the player (navmesh navigation)  
- Transitions: After a timeout, switch to Chase. If player exceeds activation distance (50m), Idle  

### EnemyTurretFSMwithLeading
#### Idle
- Actions: Nothing
- Transitions: If player gets close enough (50m), Active
#### Active
- Actions: Rotates turret towards player and shoots if it has line of sight. If no line of sight, doesn't shoot (to avoid accidentally self-destructing). Will try to lead shots on a moving player (if it has a clear shot, and no wall is in the way). Shoots as fast as the parameterized reloadtime.
- Transitions: If player exceeds activation distance (50m), Idle. If hit by a projectile, Destroyed  
#### Destroyed
- Actions: Destroys the enemy gameobject and plays an explosion VFX.
- Transitions: None, this is a final state.

### EnemyTurretFSM
- Like EnemyTurretFSMwithLeading, but only aims/shoots directly at player. Same states & transitions.  

### GameState and LevelState FSMs  
- the overall game state (MainMenu, InLevel, BetweenLevels) and level state (Ongoing, Win, Lose) are managed using an FSM (technically)  

## 🎬 Scripted Events [some not yet implemented]
- Lose event (player tank destroyed) - bring up score menu
- Win event (plays victory music and loads next level. If last level, displays win screen/menu)
- Long shot event (destroying an enemy tank from a large distance will play a sound effect and score extra points)
- Ricochet kill event (destroying an enemy tank with a ricochet shell will play a sound effect and score extra points)

## 🌍 Environment [destructibility not yet implemented]
- Various levels with different obstacles, enemy arrangements and layout
- Some walls/objects will be "destructible" and may allow for new paths or angles to shoot shells if blown up (with a landmine)
- "Tabletop" environment : on a wood floor/surface, with various blocks and everyday items as environmental obstacles (ie, mug, pen, computer, various toys and wood blocks, etc)

## 🧪 Physics Scope
Rigidbody on player tank(s) and enemy tanks  
Colliders on walls, triggers on landmine (proximity/radius based on tank positions)  
Physics-based shell/rocket projectiles (which may or may not bounce off the environment)  
- shells move at constant speed (no acceleration) until the hit a wall or tank, with up to one bounce/ricochet off a wall
- rockets have acceleration and a low initial velocity, and do not bounce
- bouncy rockets act like rockets but bounce up to one time off walls

## 🛠️ Systems and Mechanics [Mostly unimplemented]
- Score : points for destroying enemy tanks (varous points depending on enemy difficulty). More points for "tricks", ie, ricochets or long-distance shots, or landmine kills
- Levels : Player progresses through a set of levels in order to beat the game. Can select highest level cleared from level select menu
- Camera : Third-person, top-down view following player tank. Can see over walls
- Audio : Background music, SFX for trick kills, explosions, winning/losing, etc
- VFX : for explosions cause by landmines or tank destructions

## 🎮 Controls
W/A/S/D movement (tank movement)  
Mouse to "aim" tank turret  
Left mouse button to shoot a shell  
Right mouse button to place a landmine [unimplemented]  
ESC to access pause menu  

## 📂 Project Setup aligned to course topics
Unity 6.2 (6000.2.6f2)  
C# scripts for PlayerController, EnemyTank_{type}, ShellProjectile, RocketProjectile, LandMine, etc  
NavMesh for AI pathing  
Physics materials and layers configured in Project Settings  
GitHub repository with regular commits and meaningful messages  
Assets and sounds from free packs, unity asset store, and self made  

## 🤝 Team Plan
I am working on this project solo  
