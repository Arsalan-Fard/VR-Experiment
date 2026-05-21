# VR Experiment

A within-subjects VR experiment built in Unity for Meta Quest 3S, measuring participant responses across two conditions with a washout phase between them. Participants navigate through rooms, make choices, rate their selections, and encounter success or failure outcomes — all while head pose and event data stream via LSL to LabRecorder.

## Experimental Design

- **Two-condition within-subjects design** with counterbalanced ordering
- **8 predefined sequences** controlling condition order and skybox environments
  - Sequences 1–4: Condition 1 first (fail outcome) then Condition 2 (success outcome)
  - Sequences 5–8: Condition 2 first then Condition 1
- **Middle isolation phase** (washout) between conditions to mitigate carryover effects

### Experiment Flow

```
Sequence Selection → Condition 1 → Middle (Washout) → Condition 2 → End
```

Each condition follows the same structure:

1. **Choice phase** — select a box (black/white/gray), confirm, rate (0–10), confirm, door opens
2. **Navigation phase** — traverse glass rooms, wait for barriers to progressively open, rate (0–10) after last barrier opens
3. **Reveal phase** — enter reveal room (hover door handle for 2s), outcome displayed

The middle phase provides complete visual isolation before the second condition begins.

## Requirements

- **Headset:** Meta Quest 3S
- **Engine:** Unity with XR Interaction Toolkit
- **Data recording:** LabRecorder on a laptop connected to the same WiFi network
- **Native library:** `liblsl.so` cross-compiled for Android arm64 (included in `Assets/Plugins/Android/arm64-v8a/`)

## Project Structure

```
Assets/Scenes/MainScene/
├── Scripts/
│   ├── ExperimentStateManager.cs   # State machine (Condition1 → Middle → Condition2)
│   ├── SequenceManager.cs          # Sequence loading, participant ID, skybox assignment
│   ├── BoxChoiceManager.cs         # Box selection and rating UI flow
│   ├── RevealManager.cs            # Reveal room triggering and outcome display
│   ├── RoomManager.cs              # Glass room isolation and timers
│   ├── BarrierManager.cs           # Progressive barrier opening with delays
│   ├── RatingManager.cs            # Rating event logging to LSL
│   ├── FamilarityManager.cs        # Practice environment (X button toggle)
│   ├── DoorManager.cs              # Door open/close animations
│   ├── DoorHandleInteractable.cs   # Hover-to-open interaction
│   ├── StayOnPlaneToAdvance.cs     # Return plane trigger for middle phase
│   ├── SequenceSelectionUI.cs      # Experimenter menu for sequence/participant setup
│   ├── QuestEventOutlet.cs         # LSL event marker stream
│   ├── HeadPoseOutlet.cs           # LSL head pose stream (50 Hz)
│   └── ...
├── SequenceConfig.asset            # 8 sequences with skybox indices
└── MainScene.unity                 # Main experiment scene
```

## Data Collection

Two LSL streams are sent from the Quest to LabRecorder:

| Stream | Type | Rate | Channels |
|--------|------|------|----------|
| `Quest.HeadPose` | MoCap | 50 Hz | 7 floats: PosX, PosY, PosZ, RotX, RotY, RotZ, RotW |
| `Quest.Events` | Markers | Irregular | 1 string: event marker |

### Event Markers

All markers are sent as strings on the `Quest.Events` stream via `QuestEventOutlet.Send()`. Dynamic fields are shown in braces.

**Lifecycle**

| Marker | Source | When |
|--------|--------|------|
| `app_start:participant:{id}:sequence:{num}` | QuestEventOutlet | App launches, outlet initializes |
| `app_quit` | QuestEventOutlet, ExperimentStateManager | App closes or End button pressed |

**Sequence Registration**

| Marker | Source | When |
|--------|--------|------|
| `sequence_registered:participant:{pid}:sequence:{n}` | SequenceSelectionUI | Start pressed (no override) |
| `sequence_registered:participant:{pid}:sequence:{n}:condition_override:C{c}` | SequenceSelectionUI | Start pressed (with override) |
| `condition_override:C{n}` | SequenceSelectionUI | Condition override selected mid-experiment |

**Phase Transitions**

| Marker | Source | When |
|--------|--------|------|
| `condition{label}_start` | ExperimentStateManager | Condition begins (label = 1 or 2) |
| `middle_start` | ExperimentStateManager | Washout phase begins |
| `condition1_reveal_waiting` | ExperimentStateManager | Waiting for Continue after C1 reveal |
| `experiment_end` | ExperimentStateManager | Experiment finishes |

**Box Choice**

| Marker | Source | When |
|--------|--------|------|
| `box_choose_black` | BoxChoiceManager | Black box grabbed |
| `box_choose_white` | BoxChoiceManager | White box grabbed |
| `box_choose_gray` | BoxChoiceManager | Gray box grabbed |

**Ratings**

| Marker | Source | When |
|--------|--------|------|
| `rating_{context}_{value}` | RatingManager | Slider submitted (context set per instance, value 1–10) |
| `rating_slider_enabled` | RevealManager | Rating slider becomes available |

**Doors**

| Marker | Source | When |
|--------|--------|------|
| `door_open:{name}` | DoorManager | A door opens |
| `door_close:{name}` | DoorManager | A door closes |
| `door_handle_activated:{name}` | DoorHandleInteractable | Handle hover completed (2s) |

**Barriers**

| Marker | Source | When |
|--------|--------|------|
| `barrier_trigger:{name}` | BarrierManager | Player enters barrier trigger zone |
| `barrier_timer_start:{name}:{d}s` | BarrierManager | Barrier countdown begins |
| `barrier_timer_end:{name}` | BarrierManager | Barrier countdown finishes |
| `barrier_manual_open:{name}` | BarrierManager | Right B manually opens the next closed barrier |
| `barrier_open:{name}` | BarrierManager | Barrier door opens |
| `last_barrier_open` | BarrierManager | Final barrier opens |

**Glass Rooms**

| Marker | Source | When |
|--------|--------|------|
| `room_enter:{name}` | RoomManager | Player enters a room trigger |
| `glass_room_on:{name}` | RoomManager | Glass room isolation activates |
| `timer_start:{name}:{lockSeconds}s` | RoomManager | Room lock timer starts |
| `timer_end:{name}` | RoomManager | Room lock timer ends |
| `glass_room_off:{name}` | RoomManager | Glass room isolation deactivates |

**Reveal**

| Marker | Source | When |
|--------|--------|------|
| `reveal_success` | RevealManager | Success outcome displayed |
| `reveal_fail` | RevealManager | Fail outcome displayed |

**Middle Phase**

| Marker | Source | When |
|--------|--------|------|
| `return_plane_held` | StayOnPlaneToAdvance | Player presses Y on return plane |

## Controller Mapping

| Button | Action |
|--------|--------|
| Left Menu | Toggle sequence selection menu |
| Left X | Toggle familiarity/practice environment |
| Left Y (on return plane) | Advance from middle phase to Condition 2 |
| Right B | Manually open the next closed turnstile/barrier |
| Grab | Select box / interact with cubes |
| Hover (on door handle) | Open door (2s hover hold) |

## Running the Experiment

1. Build and deploy to Meta Quest 3S
2. Connect Quest and LabRecorder laptop to the same WiFi network
3. Open LabRecorder — it will auto-discover `Quest.HeadPose` and `Quest.Events` streams
4. Launch the app on Quest
5. Press the left menu button to open the sequence selection menu
6. Set participant ID and sequence number, then press Start
7. Begin recording in LabRecorder

## Barrier Delay Schedule

Barriers open progressively with the following delays: 20s, 30s, 60s, 120s, 90s. Some barriers add glass room lock time to their delay.
