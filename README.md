# RR486
Project for CISC486 by Roman Rodchenkov

# 🎮 {placeholder name} 

## 📌 Overview
{name} is a 3-d survival/action game in which the player controls a tank, and must clear progressively harder levels that include  enemies and obstacles. Inspired by the Wii Play "Tanks!" game.

## 🎯 Game Genre
Survival and Action 

## 🕹️ Core Gameplay
Player controls the "player tank" from a top-down view, and:
- shoots shells, drives around, and places landmines to destroy enemy tanks
- avoids environmental hazards and enemy shells, rockets, and mines to survive
Win by being the last (allied*) tank alive
Lose if your tank is destroyed by the enemy

## 👥 Player Setup
Single player as the player tank 
Optional co-op mode where a second player controls an allied tank and helps clear levels 

## 🤖 AI Design
Planning on having several (4 to 8) types of enemy tanks with different behaviour, projectiles, movespeed, health, etc.

### Enemy Tank FSM (basic)
Idle  
Patrol (move around in a set path)  
Fighting (shooting tank shells at player, dodging shells from player)
Destroyed (immobile, destroyed and inactive)

### Enemy Tank FSM (rocket)
Idle  
Patrol (move around in a set path)  
Fighting (shooting rockets at player, dodging shells from player)
Retreat (gets distance from player) 
Destroyed (immobile, destroyed and inactive)

## 🎬 Scripted Events
- Lose event (player tank destroyed) - bring up score menu
- Win event (plays victory music and loads next level. If last level, displays win screen/menu)
- Longshot event (destroying an enemy tank from a large distance will play a satisfying sound effect and score extra points)
This isn't really the kind of game for a "after x seconds, this happens" type of event. The levels are relatively short (~15 seconds of gameplay) and most of the events depend on the player's actions.

## 🌍 Environment
Various levels with different obstacles, enemy arrangements and layout.
Some walls will be "destructible" and may allow for new pathing or angles to shoot shells if blown up (with the landmine).

## 🧪 Physics Scope
Rigidbody on player tank(s) and enemy tanks 
Colliders on walls, triggers on landmine (proximity/radius based on tank positions).
Kinematics-based shell/rocket projectiles (which may or may not bounce off the environment).
- shells move at constant speed (no acceleration) until the hit a wall or tank, with up to one bounce/ricochet off a wall
- rockets have acceleration and a low initial velocity, and do not bounce
- bouncy rockets act like rockets but bounce up to one time off walls

## 🧩 Systems and Mechanics
- Score : points for destroying enemy tanks (varous points depending on enemy difficulty). More points for "tricks", ie, ricochets or long-distance shots, or landmine kills
- Health/armor : How many hits a (player/enemy) tank can take before being destroyed. Typically 1 hit, but some tanks may be tougher.
- Levels : Player progresses through a set of levels in order to beat the game. Can select highest level cleared from level select menu.
- Camera : Third-person, top-down view following player tank. Can see over walls
- Audio : Background music, SFX for trick kills, explosions, winning/losing, etc
- VFX : for explosions cause by landmines or tank destructions

## 🎮 Controls
W/A/S/D movement (tank movement)
Mouse to "aim" tank turret
Left mouse button to shoot a shell 
Right mouse button to place a landmine 
ESC to access pause menu

## 📂 Project Setup aligned to course topics
Unity 6.2 (6000.2.3f1)
C# scripts for PlayerController, EnemyTank_{type}, ShellProjectile, RocketProjectile, LandMine, etc
NavMesh for AI pathing   
Physics materials and layers configured in Project Settings  
GitHub repository with regular commits and meaningful messages  

## 🤝 Team Plan
I am working on this project solo.