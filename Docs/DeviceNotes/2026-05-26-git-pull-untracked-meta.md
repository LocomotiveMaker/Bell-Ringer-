# 2026-05-26 Git Pull Blocked By Untracked .meta (Notebook)

## Symptom

- `git pull` (or GitHub Desktop pull) fails with:
  - "untracked working tree files would be overwritten by merge"
- The untracked files were mostly Unity-generated `.meta` files under `Assets/Audio/...`.

## Safe Fix

Stash *including untracked files*, then pull:

```powershell
cd C:\Bell-Ringer-
git stash push -u -m "notebook-safe-before-pull-2026-05-26"
git pull --ff-only
```

## Notes

- Do not immediately `stash pop` after pulling.
  - This stash mainly contained Unity-generated `.meta` files and can reintroduce conflicts.
- If you later need something from the stash, restore specific files instead of applying everything.

