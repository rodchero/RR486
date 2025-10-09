# 🎮 Tabletop Tanks
Project for CISC486 by Roman Rodchenkov

# ASSIGNMENT-RELATED ITEMS
- readme was updated since A1 to reflect new FSMs, minor changes in scope, etc
- youtube link for demo video: https://youtu.be/hx0DoiJUpA4

## 📌 Overview
Tabletop Tanks is a 3-d survival/action game in which the player controls a miniature tank, and must clear progressively harder levels that include enemies and obstacles. Inspired by the Wii Play "Tanks!" game.

## 🕹️ Core Gameplay
Player controls the "player tank" from a top-down view, and:  
- shoots shells, drives around, and places landmines to destroy enemy tanks
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

## 🎬 Scripted Events [not yet implemented]
- Lose event (player tank destroyed) - bring up score menu
- Win event (plays victory music and loads next level. If last level, displays win screen/menu)
- Long shot event (destroying an enemy tank from a large distance will play a sound effect and score extra points)
- Ricochet kill event (destroying an enemy tank with a ricochet shell will play a sound effect and score extra points)

## 🌍 Environment
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
Unity 6.2 (6000.2.6f2) [Updated since A1]  
C# scripts for PlayerController, EnemyTank_{type}, ShellProjectile, RocketProjectile, LandMine, etc  
NavMesh for AI pathing  
Physics materials and layers configured in Project Settings  
GitHub repository with regular commits and meaningful messages  
Assets and sounds from free packs, unity asset store, and self made  

## 🤝 Team Plan
I am working on this project solo  