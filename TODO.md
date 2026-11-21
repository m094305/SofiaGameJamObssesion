# TODO / future add-ons

## NPC vision (not started)
- **Real line-of-sight vision.** Today `NPCGuard.CanSeePlayerNow()` is a range + angle check
  (7 units / 90° ahead, plus 3 units / 240° peripheral) with one line-cast against the
  `Walls` layer. Ideas: several raycasts across the cone so partial cover works, and a visible
  vision cone on screen so the player can read it.
- **No one-hit kills while an NPC sees you.** Current rule (`GuardHealth.TakeDamage`):
  a hit the NPC *doesn't* see is an instant stealth kill; a hit it *does* see only does normal
  damage and makes it hostile. Revisit this when combat is reworked.

## Lake ending
- Make the two Timeline cutscenes and drop them into **Cutscene 1** (all three NPCs dead) and
  **Cutscene 2** (anyone alive) on the "Lake Ending" object (`InteractableObject`).

## Housekeeping
- The Player has two `PlayerInventory` components (one on Player, one on its "Inventory"
  child). The buttons and the inventory panel both use the one on Player; the child's copy
  could be removed.
- `Assets/Objects/narcissus *.png` are duplicates of the art in `Assets/Dani/` and aren't used.
