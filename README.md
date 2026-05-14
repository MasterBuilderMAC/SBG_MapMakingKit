This is used to log everything needed for localization to be used in relinking in the Unity editor.

Instructions:
1. Place the dll into the BepInEx plugins folder
2. Under "BepInEx/config" open the BepInEx.cfg file
3. Under Logging.Disk, find LogLevels and set it to "All"
4. Run the game, and load into the driving range
5. In the main BepInEx folder, copy the LogOutput.log to your Unity project "Editor" folder (or somewhere temporarily until you get there)
6. Remove the dll from your plugins folder, or change the extension from .dll to .disabled
