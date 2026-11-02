# Stationary launch facing — limited candidate, awaiting user review

## Exact scope
Crit pilot only. A genuine current-scope player ricochet Launched transition can establish a frozen initial flight direction while the existing resolved movement input is zero. No nearest-target query, target tracking, wall query, Core aiming change or Impact-driven turn is introduced. A launch while moving is not queued for the next stop. Nonzero input into a wall still counts as moving; zero world displacement alone is not enough.

CharacterPresentation remains the single root-yaw writer. The override replaces, rather than follows, its old movement-facing interpolation. Maximum turn speed is 240 degrees per second using shortest-angle render steps. Moving/no-override behavior retains the prior movement-facing policy. There is no second animation Advance, attack replay, damage delay, torso twist or new movement clip.

The initial direction is read from the exact same-step projectile view only when it is still the original leg. If it already impacted, the complete same-step transition sequence may provide the first nonzero actual traveled segment. Missing, zero-length, overflowed or ambiguous direction is suppressed rather than guessed from a later bounce or target slot.

## Input and lifecycle
The input router is sampled only through its pre-existing gameplay path. Extra cosmetic polling was rejected because it has side effects. Once an existing zero-input sample establishes a valid held direction, every render advances it, including renders without a simulation Step. The next normal nonzero input sample clears it. Thus input response retains the bounded fixed-tick sampling delay; it is not an unconditional immediate key-event response.

Pause freezes yaw and invalidates input knowledge while preserving the held direction. A resume Step without a fresh original input sample cannot turn or accept a new stationary launch. The next existing zero sample resumes turning; a nonzero sample clears the override. Focus/hidden/scope and ownership cleanup are separately guarded. No old launch is replayed after reset, no target generation is followed, and no time is accumulated for a later catch-up snap.

## Evidence and rejected intermediate version
225 pure checks and 231 Host checks passed in the scoped development runs. They include true resume branches, missing input, zero-overlap direction, overflow, repeated events and resets. The original no-Step-freeze design was rejected because high-refresh displays turned too slowly. The final render-time policy measured approximately 120 degrees in 0.5 seconds at 30/60/120/144 Hz while respecting the per-render speed bound.

The native A/B uses one normal-rule seed and fixed input route: stand60 ticks, right72, left72, stand60. No HP refill, enemy placement or spawn-rate adjustment was used. Both sides ended after 264 actual updates in Combat at 100HP and two coins. Movie files contain 265 frames at60fps (4.416667 seconds): the final frame is a same-state render flush, not another tick. A was reused from a previous recording batch; B is the new candidate. Recorded authority, positions, camera fields and local bone/morph data agree; actor yaw first differs at the final stationary launch. A composite camera table also contains actor basis and therefore is intentionally not byte-identical as a whole.

At normal image scale, the final stopped shot progressively faces its actual lower-left launch direction and holds, exposing the face and ring more clearly. This is a limited benefit, not complete aiming or locomotion acceptance. The shot still precedes completed rotation, so initial residual remains. Feet rotate with the root, with no authored turn-in-place gait. Moving backward fire earlier in the same clip is deliberately unchanged. Short scoped clean exits do not prove historical exit failures fixed. Strict editor-import shutdown diagnostics and mobile/hardware uncertainty remain.
