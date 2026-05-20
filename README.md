# VR School Lab Fire Safety Training

This VR application is a training simulation designed to teach students proper fire safety protocols in a school laboratory. **This project was built as a part of my assignments for the course CSGY9223 Special Topics: Designing XR Environments for Skill Learning**

## Overview
 
The application consists of **3 scenes**, each grounded in a different learning framework.
- **Scene 1** is single player where users learn how to operate a fire extinguisher properly.
- **Scene 2** is also single player where users identify and resolve different fire hazards.
- Both these scenes support two distinct learner types with adaptive scaffolding:
| Learner Type | Goal |
|---|---|
| **Beginner** | Learn fire extinguisher operation and hazard identification |
| **Returning Staff** | Practice under time pressure to improve speed and independence |
- **Scene 3** is multiplayer setup (using Photon PUN 2 networking framework) where players cooperate to resolve a fire emergency

## Demo

- Demo for fire extinguisher training and open lab inspection - [YouTube Link Scene 1 & 2](https://youtu.be/6pV94pCbNg8)
- Demo for collaborative multiplayer emergency response - [YouTube Link 3](https://youtu.be/3b0PbYRrr_E)

## Scene 1 — Fire Extinguisher Training

Users learn to operate a fire extinguisher through the **P.A.S.S. method**:
1. Grab the extinguisher by its **top handle**
2. Rotate and check the **pressure gauge** (must be in the green zone)
3. Execute **P.A.S.S.**:
   - **P**ull the pin
   - **A**im the nozzle at the base of the fire (not the flames)
   - **S**queeze the trigger
   - **S**weep side to side until the fire is out

### Scene 1 Adaptive Scaffolding
| Feature | Beginner User | Returning Staff |
| :--- | :--- | :--- |
| **Instruction** | Sequential audio guides and visual references | No explicit instructions; error sounds only |
| **Visual Cues** | Glowing materials highlight the next interaction | No glowing guides or hand-holding |
| **Feedback** | Aim warnings and a persistent progress board | Progress board tracks steps without hints |
| **Urgency** | Relaxed pacing to encourage learning | Immediate fire alarms and spatial audio |
| **Consequences** | None (Learning phase) | Fire grows and turns deep red after 7s of inaction |
| **Metrics** | Focus is purely on completion | Timer tracks performance for speed improvement |

## Scene 2 — Open Lab Inspection
 
An exploratory chemistry lab environment where users identify and resolve **3 fire hazards**. Extends the skills from Scene 1 into an unstructured setting.

### Scene 2 Adaptive Scaffolding
| Feature | Beginner User | Returning Staff |
| :--- | :--- | :--- |
| **Objective Info** | Checklist board reveals there are exactly 3 hazards | User is not told how many hazards exist |
| **Proximity Labels** | Tells the user WHAT the problem is and HOW to fix it | Only tells the user WHAT the problem is |
| **Spatial Hints** | Controllers pulse (haptics) when near an unfixed hazard | No haptic feedback provided |
| **Progression** | Success sounds play and checklist updates upon fixing | User must interpret context to confirm fixes |
| **Completion** | Ends automatically when all 3 hazards are resolved | User clicks an "End" button when they feel done |
| **Evaluation** | Focus is on guided discovery | Generates a final report (Missed hazards, time, score) |

### Key feedback mechanisms for scene 1 and 2
1. **Audio cues** telling what needs to be done next
2. **Success, Warning and Error audios** - when an action is completed successfully or failed appropriate success, error or warning audio is played
3. **Visual highlights**, objects which need to be interacted with (like extinguisher pin, handle) have a glow to guide the user's eyes to the next interaction point
4. **Haptic feedback** for tactile feel like when squeezing the handles and discharging the extinguishing agent
5. **Progress board** which provides a constant visual checklist so that the user always knows their current status


## Scene 3 — Collaborative Multiplayer Emergency Response
 
This is a networked 2 player scene where players must cooperate to resolve a fire emergency. Neither player can succeed alone and tasks are deliberately split to require coordination.

### Roles
Players select roles from a shared canvas
| Role | Color | Responsibilities |
|---|---|---|
| **Fire Responder** | 🔴 Red hat | Operate fire extinguisher |
| **Evacuation Warden** | 🔵 Blue hat | Fire alarm, ventilation, exit path |

### Key Multiplayer Mechanics
* **Collaborative Planning:** Before the fire breaks out, players must discuss and confirm the exact same action plan from 3 preset options. If they select different plans, the system prompts them to try again.
* **Shared Status Board:** A centrally located canvas updates task progress and a shared countdown timer in real time for both clients.
* **Adaptive Collaborative Scaffolding:** The system monitors team progress. For example, if the Warden is taking too long to activate the ventilation, the system audibly prompts the Responder to go help their partner.
* **Shared Consequences (Social Interdependence):** Players win or lose as a team. The simulation ends in a shared failure if the Responder fails to control the fire, the Warden chooses the wrong exit or the global timer runs out.
* **Synchronized fire and smoke** spread and clearance is mirrored across both clients.

### Split Feedback System
To reduce cognitive load and prevent sensory clutter, audio and haptic feedback is intentionally split between local and global channels.
| Feedback Type | Visibility / Audio Scope | Purpose |
| :--- | :--- | :--- |
| **Task Errors, Haptics & Success Sounds** | Local (Only heard/felt by the active player). | Prevents confusing the partner with irrelevant cues. |
| **Environment Noise (Fire, Alarms, Vents)** | Global (Heard by both players). | Maintains shared situational awareness. |
| **Game End States (Win / Loss)** | Global (Experienced by both players). | Reinforces team outcomes. |

## Learning Frameworks

### Procedural Learning (Scene 1)
* **Step by Step Acquisition:** Teaches strict sequential procedure of the P.A.S.S. method (Pull, Aim, Squeeze, Sweep) through guided physical interaction and real time validation.

### Constructivism (Scenes 1 & 2)
*Knowledge built through active exploration and consequence*
* **Exploratory discovery:** In Scene 2, hazards are not revealed. Users must physically walk the lab and identify problems themselves.
* **Adaptive scaffolding:** Assistance scales dynamically. Beginners receive explicit UI/audio guidance, while proficient users rely entirely on environmental context.
* **Action consequence learning:** Inaction or incorrect actions yield tangible, physical consequences in the world. For example delaying action in Scene 1 causes the fire to visibly grow, shift deep red and emit louder crackling audio.
* **Non linear problem solving:** Scene 2 features multiple valid learning paths; users can identify and resolve the 3 hazards in any order.

### Constructionism (Scene 2)
*Knowledge expressed through building a tangible artifact*
* **Performance artifacts:** Proficient users generate a unique Post Action Inspection Report upon completion. This report acts as an artifact of their specific choices, reflecting their missed hazards, resolved issues and time efficiency.
* **Environmental transformation:** The VR environment itself becomes a physical record of the user's work. Fixed hazards permanently change state (eg cleared exits, safely mounted extinguishers) to reflect the user's interventions.

### Social Learning Theory & Distributed Cognition (Scene 3)
* **Distributed cognition:** Critical information and actions are deliberately split between the two players. Mimicking real world emergencies, neither player can succeed using only their individual resources.
* **Social interdependence:** Shared win/loss states naturally force cooperation. If one player fails their specific duty (eg failing to extinguish the fire or choosing the wrong exit), both players fail the simulation.
* **Role theory:** Players adopt mutually exclusive, clearly defined roles (Fire Responder vs. Evacuation Warden). This gives players clear social responsibilities and focuses their learning on role specific tasks.
* **Collaborative scaffolding:** The system monitors the team as a whole, not just individuals. If one player struggles, the system prompts their partner to assist (eg directing the Responder to help with the ventilation switch), encouraging peer awareness.


## Acknowledgements
This project was built upon the open source repository [XR2IND-VR](https://github.com/giorgosfatouros/XR2IND-VR) which was originally demonstrated in our class workshop. 