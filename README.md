# Petta

> **These are experimental prototypes** — not finished products. The goal is to explore different aspects of game development in Unity (graphics, controls, gameplay systems) and see how things work in practice.

Built with **Unity 6000.3.10f1**.

---

## Projects

### Petta2 — Visual & Animation Prototype

This project was created primarily to **experiment with graphics and 3D animations**. The focus is on importing AI-generated models and trying out different visual styles and character animations.

**Key highlights:**

- **AI-Generated 3D Assets** — All models are generated with and downloaded from [Meshy AI](https://www.meshy.ai/), including fully textured FBX models with PBR materials (albedo, normal, metallic, roughness maps)
- **Multiple Meshy imports** — 13+ imported 3D models including architectural elements (e.g. "Crimson Arch Window"), test models, and character assets
- **Walking animation** — A `CharacterWalker` script that makes characters walk back and forth along a path, automatically turning around at the end of each leg
- **Input System** — Uses Unity's new Input System with mapped actions for Move, Look, Attack, Interact, Crouch, Sprint, and Jump (supports keyboard, gamepad, and touch)
- **Expandable** — Many more animations and models can be downloaded from Meshy and added to the project at any time

### PettaRoma — Gameplay & Controls Prototype

This project was built to **test Roblox-style controls and core gameplay mechanics** — understanding how third-person movement, camera orbiting, interaction systems, and game loops actually work under the hood.

**Architecture:**
The codebase follows a clean, modular structure:
- `_Player/` — Player controller and camera
- `_Core/` — Game systems (managers, AI, interactions, events)
- `_UI/` — HUD and mobile input controls
- `_Levels/` — Level scenes and environment assets

**Player & Camera:**
- **Third-person CharacterController** — Camera-relative movement with walk/run speeds, jump, gravity, and ground checking
- **Roblox-style orbit camera** — Smooth follow camera with right-click (desktop) or touch drag (mobile) to orbit around the player, configurable pitch limits, distance, and wall collision avoidance via raycast
- **Teleportation** — Player can be teleported to specific locations (used for shop doors, respawning)
- **ScriptableObject stats** — Player stats (speed, jump force, gravity, health, attack damage) defined in a `PlayerStats` ScriptableObject for easy tweaking

**Gameplay Systems:**
- **Coin collectibles** — Rotating and bobbing coins that the player picks up on contact, tracked by the GameManager
- **Checkpoint system** — Invisible trigger zones that save the player's last spawn point, with a visual pulse animation on activation
- **Enemy AI** — State machine-based enemies ("angry pizzas") with Patrol → Chase → Attack states, configurable detection/attack ranges, health, and waypoint-based patrol routes
- **NPC Dialogue** — Interactive NPCs that cycle through dialogue lines when the player presses the interact button (e.g. "Welcome to Roma!", "Watch out for the angry pizzas...")
- **Shop Door interaction** — Doors that teleport the player inside/outside the Petta Shop
- **Interactable base class** — Abstract base class for all interactive objects with trigger-based proximity detection and EventBus-driven UI prompts

**Event-Driven Communication:**
- **EventBus** — A static event hub for decoupled communication between systems (coin changes, health updates, checkpoint reached, dialogue show/hide, player death/respawn, enemy death)

**UI & Input:**
- **HUD Manager** — Displays coin counter, health bar, interaction prompts, dialogue panels, and checkpoint notifications
- **Virtual Joystick** — On-screen touch joystick for mobile with dead zone and handle visual feedback
- **Virtual Buttons** — Touch buttons for jump, attack, interact, and sprint
- **InputManager** — Central input abstraction that reads keyboard/mouse on desktop and receives virtual touch input from UI controls, with consumable one-shot flags for actions

**Audio:**
- **AudioManager** — Singleton that generates procedural sound effects at runtime (coin ping, jump blip, hurt buzz, checkpoint chime) — no external audio assets required

**Level Management:**
- **BootLoader** — Minimal boot scene that initializes managers and loads the first level (`Level1_Rome`)
- **LevelManager** — Scene loading by level ID, supports future level expansion
- **Level1_Rome** scene — A Rome-themed level with the Petta Café model imported from Meshy

**Editor Tools:**
- `RomeBlockoutBuilder` — Automated Rome level blockout
- `SceneSetupTool` / `Phase2SetupTool` — Quick scene configuration helpers
- `SetupAnimator` / `ApplyCharacterModel` — Animation and model setup utilities
- `PlaceShopModel` — Tool to position the Petta Shop model in the scene

---

## Concept Art & References

The `img/` folder contains concept art and reference images generated with ChatGPT and Meshy AI, including character designs, environment sketches, shop layouts, and logo variations.

---

## Tech Stack

| Component | Details |
|-----------|---------|
| Engine | Unity 6000.3.10f1 |
| Language | C# |
| Input | Unity Input System (new) |
| Rendering | Universal Render Pipeline (URP) |
| 3D Models | [Meshy AI](https://www.meshy.ai/) |
| Concept Art | ChatGPT Image Generation |

---

> **Note:** The `Library/`, `Logs/`, `UserSettings/`, and other auto-generated Unity folders are excluded from version control via `.gitignore`.
