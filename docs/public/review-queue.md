# Review queue

Review status describes product acceptance, not just whether code was committed. Failed experiments are archived for traceability; users do not need to review each rejected video.

## Awaiting user review
| Candidate | Scope and review focus | Remaining limitations |
| --- | --- | --- |
| [Stopped Crit launch facing](https://github.com/Slpekamer/HMI-Game/tree/wip/stationary-launch-facing-20261102) | Limited direction correction after a real stationary launch; reviewed normal-rule 4.4-second full-view A/B | Moving backfire unchanged, initial turn residual, feet rotate with root, no device certification |
| [Integrated rule-3 WIP](https://github.com/Slpekamer/HMI-Game/tree/wip/rule3-integration-20261102) | Real projectiles, filled cyan/caramel bodies, explicit save upgrade, codex focus and removal of the combat-overlay guidance sentence | Shadows, crowd occlusion and moving-attack presentation remain; WIP, not a release |

## Returned for changes / archival only
| Archived branch | Why it is not a current review request |
| --- | --- |
| [Surface impact](https://github.com/Slpekamer/HMI-Game/tree/wip/impact-surface-rejected-20261102) | Extra geometry complexity for an almost invisible symbol; retain simpler hurt feedback |
| [Ready / Strafe and melee-priority](https://github.com/Slpekamer/HMI-Game/tree/wip/ready-melee-aim-rejected-20261102) | Limited gait readability and excessive whole-body target switching; not adopted |
| [Common melee fan](https://github.com/Slpekamer/HMI-Game/tree/wip/common-melee-fan-rejected-20261102) | First normal-scale image did not convey a clear attack shape |

## Awaiting engineering review
No additional completed candidate is listed here yet. New implementation or diagnostic work must receive code, behavior and actual-image review before moving into the user queue. Nothing in this queue authorizes automatic merging into main, an APK delivery or a hardware-support claim.
