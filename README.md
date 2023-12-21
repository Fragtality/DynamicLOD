# DynamicLOD

Based on muumimorko's Idea and Code in MSFS_AdaptiveLOD.<br/>
It just a small Test / Proof-of-Concept if MSFS Performance could be improved when dynamically changing the TLOD (and OLOD) based on the current AGL.<br/><br/>
That being said, DynamicLOD is not meant to be a "User facing" Tool - I can't recommend using it. It actively modifies the Memory of MSFS (basically it works like any "Cheat Trainer"). It likely **violates the Usage Terms** and can be treated as **potential ban-worthy** Behavior. It is like Cheating in a FPS.<br/>I'm not in anyway responsible for (or interested in) any Problems you're facing using something you should not use. There will be zero Support and I'm not interested in any Suggestions.<br/><br/>

If you don't know how to use it, don't comprehend what it does and the possible Consequences of that: **DO NOT USE IT**.
<br/><br/>

## Requirements

The Installer will install the following Software:
- .NET 7 Desktop Runtime (x64)
- MobiFlight Event/WASM Module

<br/>

[Download here](https://github.com/Fragtality/DynamicLOD/releases/latest)

(Under Assests, the DynamicLOD-Installer-vXYZ.exe File)

<br/><br/>

## Installation / Update
Basically: Just run the Installer.<br/>

Some Notes:
- DynamicLOD has to be stopped before installing.
- If the MobiFlight Module is not installed or outdated, MSFS also has to be stopped.
- If you upgrade from Version 0.3.0 or below, delete your old Installation manually (it is no longer needed).
- From Version 0.3.0 onwards, your Configuration is *not* be resetted after Updating.
- Do not copy over a Configuration from a Version below 0.3.0
- Do not run the Installer as Admin!
- For Auto-Start either your FSUIPC7.ini or EXE.xml (MSFS) is modified. The Installer does not create a Backup.
- It may be blocked by Windows Security or your AV-Scanner, try if unblocking and/or setting an Exception helps (for the whole Folder)
- The Installation-Location is fixed to %appdata%\DynamicLOD (your Users AppData\Roaming Folder) and can not be changed.
  - Binary in %appdata%\DynamicLOD\bin
  - Logs in %appdata%\DynamicLOD\log
  - Config: %appdata%\DynamicLOD\DynamicLOD.config

<br/><br/>

## Usage / Configuration

- Starting manually: before MSFS or in the Main Menu. It will stop itself when MSFS closes. 
- Closing the Window does not close the Program, use the Context Menu of the SysTray Icon.
- Clicking on the SysTray Icon opens the Window (again).
- Runnning as Admin NOT required (BUT: It is required to be run under the same User/Elevation as MSFS).
- You can have (exactly) three different Sets/Profiles for the AGL/LOD Pairs to switch between (manually but dynamically).
- For VR Support you have to mark the Profile with "VR Profile".
- There is no automatic Detection if VR is active: you have to manually switch between Profiles.
- The first Pair with AGL 0 can not be deleted. The AGL can not be changed. Only the xLOD.
- Additional Pairs can be added at any AGL and xLOD desired. Pairs will always be sorted by AGL.
- Plus is Add, Minus is Remove, S is Set (Change). Remove and Set require to double-click the Pair first.
- A Pair is selected (and the configured xLOD applied) when the current AGL is above the configured AGL. If the current AGL goes below the configured AGL, the next lower Pair will be selected.
- A new Pair is only selected in Accordance to the VS Trend - i.e. a lower Pair won't be selected if you're actually Climbing (only the next higer)
- **Less is more**:
  - Fewer Increments/Decrements are better. Of reasonable Step-Size (roughly in the Range of 25-75).
  - Some Time in between Changes is better.
  - Don't overdo it with extreme low or high xLOD Values. A xLOD of 100 is reasonable fine on Ground, 200-ish is reasonable fine in Air.
  - Tune your AGL/LOD Pairs to the desired Performance (which is more than just FPS).
  - FPS Adaption is just *one temporary* Adjustment on the current AGL/xLOD Pair to fight some special/rare Situations.
  - Forcing the Sim to (un)load Objects in rapid Succession defeats the Goal to reduce Stutters. It is *not* about FPS.
  - Smooth Transitions lead to smoother Experiences.
- Rest should be self-explanatory. And if it is not: You are using something you really should not use.

<br/><br/>
