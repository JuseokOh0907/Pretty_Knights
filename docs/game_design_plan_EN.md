# Mobile 2D Hunting RPG Design Plan

> Document version: 2026-08-01  
> Purpose: Single source of truth for the current concept, production status, and technical direction  
> Platforms: iOS and Android, with an Android APK build planned

## 1. Project Summary

This is a mobile 2D RPG in which the player repeatedly hunts normal monsters in portrait mode to farm experience and items, then rotates to landscape mode for direct character control, map exploration, boss hunts, and higher-value rewards.

The core product idea is to connect two distinct play rhythms through the progression of one character.

- Portrait mode: quick status checks, automated routine hunting, common farming, and progression management
- Landscape mode: direct control, exploration, boss encounters, and rare rewards
- Progression result: reflected through stats, skills, weapons, clothing, and the character's visible appearance

## 2. Confirmed Current Direction

- The player will choose one character from the available sprite characters and focus on developing that character.
- Character movement, attacks, and skill animations will support eight directions, including diagonals.
- The game will use an angled top-down 2D perspective inspired by the DS-era Pokemon games, with combat-space readability similar to The Binding of Isaac.
- The asset pipeline must leave room for clothing and weapon designs to change as the character grows.
- The game normally opens into the portrait hunting/base screen.
- Portrait combat is used for repeated normal-monster hunting, experience, leveling, and probabilistic item farming.
- Landscape combat is used for direct controls, tilemap exploration, elite and boss hunts, and rare rewards.
- Both stats and skills will exist.
- Maps will be built with tilemaps.
- Skill hit detection will use clear cell/sensor-based shapes similar in readability to Crazy Arcade's water-balloon range.
- Mobile VFX will be deliberately restrained, while skill range and impact timing remain explicit.
- Cards, poker hands, multiplayer deception, and cooperative platforming are out of the current scope.

## 3. Core Gameplay Loop

```text
Launch game
→ Review portrait auto-hunt status
→ Collect XP, common gear, and materials
→ Manage stats, skills, and equipment
→ Select a boss expedition
→ Rotate into landscape mode
→ Explore, fight directly, and defeat the boss
→ Gain rare gear, unique materials, and new regions
→ Unlock a new portrait auto-hunting area
```

Portrait mode prepares the next expedition. Landscape mode expands the world and the available reward pool.

## 4. Portrait Mode: Routine Hunting and Base Management

### Goals

- Allow short, convenient, one-handed sessions.
- Provide steady progression through normal-monster hunting.
- Show the character, offline result, current hunting area, and next action at a glance.

### Combat Model

- Basic movement, attacks, and skill use are automated.
- The player configures the hunting area, target monster, skill priority, and automatic item-dismantling rules.
- While the app is open, the game displays the actual battle animation.
- While the app is closed, rewards are calculated from elapsed time, combat power, and the selected hunting area.
- Auto-hunting should not stop because the inventory is full; auto-dismantle and storage rules should handle overflow.

### Main-Screen Information Priority

1. Current character and level
2. Auto-hunting area and status
3. Experience progress
4. Recently acquired items
5. Claimable rewards
6. Button to enter the next boss expedition

## 5. Landscape Mode: Exploration and Boss Hunting

### Goals

- Give the player direct control of the character.
- Explore tilemaps and discover shortcuts, secrets, elites, and bosses.
- Use the levels, equipment, and skills prepared in portrait mode.
- Earn rare gear, boss-specific materials, and access to new hunting regions.

### Gameplay Elements

- Direct eight-direction movement and facing
- Basic attacks, dodge, and active skills
- Reading monster telegraphs and attack patterns
- Exploiting boss weak points or breakable parts
- Tilemap-based exploration
- Strong reward presentation after boss defeat

Stats should make landscape combat more manageable, but they should not replace learning and responding to boss mechanics.

## 6. Progression System

### 6.1 Stats

Stats provide stable numerical growth through levels and equipment.

Initial candidates:

- Vitality: maximum HP
- Attack: basic damage
- Defense: incoming damage reduction
- Agility: movement and dodge performance
- Focus: skill power and reuse rate

The final stat list and formulas are not confirmed. The initial version should favor automatic level growth and equipment choices over manual free-stat allocation.

### 6.2 Skills and Hunting Insight Proposal

> Status: Recommended direction, pending final confirmation

Instead of awarding generic skill points only through leveling, the game may award `Hunting Insight` based on how a monster is hunted.

Examples:

- First kill: basic monster information and a basic node
- Dodge a specific attack three times: mobility or dodge node
- Break a specific body part first: armor-break or attack node
- Win without healing: mastery bonus
- Finish with a trap or status effect: special node

Candidate skill branches:

- Predator: damage, bleeding, healing on kill
- Tracker: mobility, weak-point display, ambush
- Survivor: defense, dodge, status resistance
- Beast: boss-derived signature active skills

To keep the mobile HUD readable, the recommended active-skill loadout is limited to two or three skills.

### 6.3 Equipment and Appearance

- Normal monsters: common equipment and crafting materials
- Elite monsters: advanced equipment blueprints
- Bosses: signature weapons, outfits, and core crafting materials

Rare equipment should not rely only on random drops. Bosses should guarantee a signature material so repeated clears eventually let the player craft the desired item.

## 7. Character Sprite and Equipment Structure

The sprite pipeline should be standardized so weapons and clothing can change later.

```text
Base body
→ Hair/head accessory
→ Top/armor
→ Weapon
→ Secondary equipment
→ Foreground effect
```

Required conventions:

- All layers share the same canvas size, pivot, and frame count.
- Assets follow an eight-direction standard covering the four cardinal and four diagonal directions.
- Hand ownership and front/back occlusion rules remain consistent in every direction.
- The ground pivot and root position stay fixed across animation frames.
- The MVP should swap weapon and top/armor layers first.
- Asset cost must account for `directions x actions x frames` for every new equipment set.

## 8. Skill Hit Detection and VFX

### 8.1 Design Principles

- Separate gameplay hit detection from visual effects.
- One range calculation should drive preview indicators, actual damage, AI avoidance, and the skill-description UI.
- Even minimal VFX must clearly communicate the affected area and impact timing.

### 8.2 Shared Range Patterns

- Forward: one or two cells in front of the character
- Line: travels in the facing direction and stops at a wall
- Cross: expands from the center in four cardinal directions
- Area: circular or rectangular area around the character
- Dash: covers the complete movement path

Initial implementation skills:

1. Forward slash
2. Piercing line attack
3. Area burst

### 8.3 Directional VFX

- A right-facing effect may be horizontally flipped for the left direction.
- Up and down should usually have separate assets because of perspective and occlusion.
- Upper-right/upper-left and lower-right/lower-left diagonal pairs may share mirrored source effects.
- Sprites that expose right-hand or left-hand equipment ownership must use direction-specific art instead of a simple mirror.
- Standardize a directional `SkillOrigin` anchor for every character.
- Upward attacks render partly behind the character; downward attacks render in front.

### 8.4 Minimal VFX Package

1. Telegraph: simple ground range indicator
2. Impact: short four-to-eight-frame directional effect
3. Response: enemy flash, recoil, damage number, sound, and short haptic feedback

Visual effects remain restrained, but hit-stop, audio, and enemy reactions preserve impact.

## 9. Map and Exploration

- Maps are tilemap-based and each region should support exploration, combat, and a boss route.
- The perspective is fixed to angled top-down 2D. Characters and props use their ground position for Y-sorting and front/back occlusion.
- Tall objects such as walls, trees, and buildings separate their ground collision area from their upper occluding artwork.
- The character may appear relatively small against the environment to reinforce the scale of the world.
- For mobile readability, an initial target is roughly 10-13% of screen height for the character.
- Even a wrong branch should contain at least one reward: materials, a monster, a secret, or a landmark.
- Regions should be distinguished through palette, lighting, ambience, primary monsters, and large environmental landmarks.
- The final topology, whether open world, region-select based, or interconnected in a Metroidvania-like way, is not yet confirmed.

## 10. UI/UX and Orientation

### Portrait UI

- Default game-entry screen
- Optimized for one-handed use and short checks
- Focused on the character, hunt progress, XP, recent rewards, and expedition launch
- Candidate bottom navigation: Base, Character, Skills, Bestiary
- Recommended skill-tree layout: vertical scrolling with a bottom detail sheet

### Landscape UI

- Movement control on the left
- Attack, dodge, and two or three active skills on the right
- Only HP, skill resource, and the current objective remain at the top
- Immediate drops and boss-clear rewards are presented in landscape
- Detailed equipment sorting and skill progression happen after returning to portrait

### Orientation Transition

- Portrait and landscape use separate scenes or separate UI prefabs.
- Orientation and resolution are application-wide state, not scene-local state, so entering a scene requires an explicit orientation change.
- Portrait and landscape canvases use their own reference resolutions and `CanvasScaler` settings.
- Orientation changes only when entering or leaving a hunt, not while browsing menus.
- Scene-specific screen scaling is currently deferred for later implementation.

## 11. Optimization Principles

- Keep skill detection independent from VFX.
- Use sprite atlases and shared materials.
- Pool skill and impact effects.
- Limit large transparent overlays, full-screen effects, and real-time lighting.
- Do not create a damage object for every tile; collect targets from one range result.
- Resolve multihit skills at fixed intervals rather than every frame.
- Disable off-screen animations and effects.
- Simulate offline rewards from elapsed time instead of running background combat.

## 12. Current Production Status

> Based on the status reported by the project owner

- Sprite-cell PNG separation is in progress.
- Character running and movement animations are nearing completion.
- Next major task: directional skill VFX.
- Urgent content tasks: monster sprites and the tilemap.
- Portrait/landscape scene scaling and orientation switching are planned for later.
- The shared workspace currently contains planning documents only; the actual game-project path has not been included.

## 13. Hackathon MVP Proposal

### Required

- One playable character
- Eight-direction movement, running, and basic attack
- Three skills sharing the range-detection system
- Three normal monster types
- One boss
- One tilemap region
- Portrait auto-hunting loop
- Landscape direct exploration and boss loop
- XP, leveling, common drops, and boss rewards
- Minimal portrait and landscape UI
- Save data and offline-reward calculation

### Add If Time Allows

- Two weapons and two top/armor appearances
- Monster-specific hunting challenges
- Six to nine Hunting Insight skill nodes
- Boss-part breaking
- Secret rooms and shortcuts
- Automatic dismantling filters

### Out of Scope

- Multiplayer
- Cards, deck building, and poker hands
- Large open world
- Complex crafting economy
- Multiple playable characters
- Heavy real-time lighting and large particle counts

## 14. Open Decisions

1. Final playable character and starting weapon
2. Camera size, tile projection, collision rules, and occlusion rules for the angled top-down perspective
3. Final stat list, formulas, and level curve
4. Whether to adopt the skill tree and Hunting Insight system
5. Offline reward cap and drop formula
6. Guaranteed versus random ratio for boss rare items
7. Map topology and the first region's theme
8. Portrait/landscape reference resolutions and transition behavior
9. How many equipment sprite layers the first release will support

## 15. Immediate Production Order

1. Lock the playable character and complete the core eight-direction asset set.
2. Lock canvas, pivot, frame, and equipment-layer conventions.
3. Build hit detection and VFX for forward slash, piercing line, and area burst.
4. Produce chaser, ranged, and elite monster sprites.
5. Connect one tilemap region and boss space as a greybox.
6. Make portrait auto-hunting and landscape direct play share the same character data.
7. Connect XP, drops, boss rewards, and save data.
8. Add responsive UI and portrait/landscape transitions.

## 16. Parked Earlier Concepts

The following concepts were explored earlier but are not part of the current core scope:

- Hold'em-hand deck-building RPG
- Fifty-two-card collection and hand rerolls
- Cooperative extraction platformer
- Three-minute escape and resource-collection runs
- Multiplayer card deception and cheat detection
- Solo Metroidvania focused primarily on traditional platforming

These ideas may be reconsidered for a later mode or a separate project, but they should not be mixed back into the current MVP.
