# Assembly assignment requirements

The GhostGrub scene implements the required assembly flow and the 15-part bonus. Code compilation and serialized-reference checks pass. Visual placement, reach, text readability, and controller behavior still require a headset Play Mode check before submission.

| Requirement | Implementation |
| --- | --- |
| VR environment with an accessible assembly object | Existing Meta XR camera/controller rig, counter, ingredient dispensers, and sandwich assembly station in Assets/GhostGrub.unity. This is the enabled build startup scene. Reach must be verified in headset. |
| An object to assemble; third-party assets permitted | Sandwich assembled from the existing food model and ingredient prefabs. |
| At least 8 parts | All six recipes now contain at least eight separately placed ingredient layers. Repeated ingredients are separate physical parts; there are also nine distinct ingredient types available. |
| Parts clearly separated in the assembly area | Nine existing dispenser locations provide separate bread, bacon, cheese, chicken, lettuce, tomato, pickle, onion, and egg ingredients. Scene layout and reach need a visual check. |
| Final positions indicated | The next ingredient's placed visual appears at its exact target offset, rotation, and stack height using the existing green ghost material. Its colliders are disabled. The indication is visible before grabbing and advances after placement. |
| Clear sequence and next-part UI | Customer ticket displays assembly count, NEXT ingredient, numbered steps, completed markers, and completion/delivery instructions. Placement is unrestricted. Guidance counts the correctly assembled prefix and flags wrong or extra layers with pickup/discard/rebuild instructions. |
| Finish the assembly task | Station pickup is available as soon as any ingredient has been placed, including wrong or incomplete assemblies. Nearby loose-sandwich pickup excludes sandwiches still on the station so pickup always goes through the station and creates a fresh base. The completed sandwich can be delivered through the existing recipe validation flow. |
| Bonus 1: at least 15 parts | Monster Garden Melt has 15 separate layers. |
| Bonus 2: reverse disassembly | Not implemented. |
| Bonus 3: two assembly objects | Not claimed; the recipes are variants of one sandwich assembly system. |

## Recipe sizes

- BPT: 8
- Chicken Sammich: 8
- Unholy Bonewich: 8
- Graveyard Club: 11
- Haunted Breakfast Stack: 10
- Monster Garden Melt: 15

## Verification performed

- Compiled gameplay scripts with Unity's bundled Roslyn compiler and the generated editor compilation configuration; no diagnostics.
- Validated all recipe lengths, bread at both ends, and ingredient availability.
- Checked all nine ingredient catalog references resolve to the correct IngredientItem prefab components, with placed visuals and positive stack heights.
- Checked the queue-to-station reference and scene-local serialized references.
- Checked the diff for whitespace errors.

## Headset acceptance check

1. Start GhostGrub and confirm the counter and nine separate dispensers are reachable, with legible ticket text.
2. When a customer arrives, confirm the ticket starts at 0/N, names Bread as the next part, and the green bread preview is visible at the board before grabbing anything.
3. Place the wrong ingredient: it must be added, correct-step progress must stop, and the ticket must show the mistake. Pick up and discard it, then rebuild.
4. Place the correct part: progress increments once and the preview advances to the next layer's final pose. Confirm the preview does not collide with hands or loose ingredients.
5. Pick up an incomplete sandwich with the trigger: pickup must succeed and a fresh assembly must appear on the board.
6. Complete an eight-part order and the 15-part Monster Garden Melt: verify numbered guidance, completed markers, preview alignment, and completion instructions.
7. Pick up and deliver the completed sandwich. Verify the customer accepts it, the queue advances, and the next customer's recipe starts at zero with a fresh preview.
8. Test dropping, re-picking up, discarding, and rebuilding a finished sandwich before delivery. The station starts a fresh copy of the current recipe after any pickup, including incomplete or incorrect sandwiches.

The requirement wording is interpreted as physical ingredient layers, including repeated types. If the assessor requires unique component types rather than separately assembled parts, the 15-part bonus would require additional unique ingredients; confirm that interpretation with the assessor.
