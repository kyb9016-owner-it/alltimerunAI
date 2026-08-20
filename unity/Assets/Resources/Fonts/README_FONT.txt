Unity no longer allows loading the built-in Arial font via script (GetBuiltinResource).
To get UI text to display:

Option A (easiest)
  - Select the GamePrototype object in the Hierarchy.
  - In Inspector, find "Ui Font Override" under Optional Art Overrides.
  - Assign any Font asset (e.g. copy LegacyRuntime.ttf from your Unity install into Assets, then drag it here).

Option B
  - Put a .ttf font in this folder (Assets/Resources/Fonts/) and name it "LegacyRuntime" or "Font".
  - The script will load it automatically via Resources.Load.
