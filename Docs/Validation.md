# Colonization validation

Validated on 18 October 2026 using Unity 6000.6.6f1. Current baseline before the town/video work: commit `cfa67a5` (`part1`).

## Latest Play Mode result

`unity test D:/GitFile/ProjectGI334_Chananchida --mode PlayMode --filter Colonization`

Result: **9 passed, 0 failed, 0 skipped**. Report: `Logs/Colonization-Final-PlayMode.xml`; log: `Logs/Colonization-Final-PlayMode.log`.

Coverage includes world/catalog initialization, terrain-specific movement and turn budgets, boarding/disembarking, next-turn clearing and founding, colony production/recruitment, nation screens, all six worker drop targets, return to unemployment, and the complete recorded gameplay sequence. The drag tests dispatch Unity pointer/drag/drop events and use the UI raycaster to verify that the visible drop target receives the event.

The recorder checks all 384 hexes visible at startup, the two passengers landing, the forest changing to cleared terrain, a colony being founded, residents moving through all six work hexes, both residents returning to unemployment, and Food/Lumber increasing by the previewed yields.

## Video

`Deliverables/Colonization_Gameplay_Walkthrough.mp4`: H.264 MP4, 1920×1080, 20 fps, 1,920 frames, 96 seconds, no audio. It is an automated playthrough rendered directly from the running Play Mode scene. It is not a human mouse recording.

Reviewed full-resolution reference renders of the final town UI and world map. Hex art uses its visible alpha bounds; the six town work slots surround the city image, with labels placed outside the artwork. The lower-left unemployment area and the production summary are visible. Chapter timestamps are saved in `Deliverables/Walkthrough_Chapters.txt`.

## Earlier checks

The asset integration's Edit Mode suite passed 5 checks for hex geometry, land/sea terrain restrictions, and all 68 distinct gameplay sprite references. The original 99 files extracted from `ALL Assets.zip` matched the archive SHA-256 checksums.

## Limits

No target-player build is claimed by this report. The earlier Windows build attempt did not produce a validated deliverable. The prototype still uses Native village artwork for the player colony and implements Food/Lumber rather than the full 16-product economy. The video has English step captions and uses the current prototype rules.

The four tracked performance-test JSON/meta files removed automatically by Unity test cleanup were restored byte-for-byte from the baseline. Existing `_Recovery` files were left untouched.
