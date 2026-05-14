This is a get started kit for making maps for Super Battle Golf.

Prerequisites:  
Unity editor version 6000.3.10f1 (ONLY this will work, has to be same as SBG).  
The exported Unity project using Asset Ripper, free version works fine.  
The custom map mod downloaded.
The BuildBundles, RelinkLocalization, and localizationLogger.dll (in releases) downloaded.

Instructions for LocalizationLogger.dll:
1. Place the dll into the BepInEx plugins folder.
2. Under "BepInEx/config" open the BepInEx.cfg file.
3. Under Logging.Disk, find LogLevels and set it to "All".
4. Run the game, and load into the driving range.
5. In the main BepInEx folder, copy the LogOutput.log to your Unity project "Editor" folder (or somewhere temporarily until you get there).
6. Remove the dll from your plugins folder, or change the extension from .dll to .disabled.

Instructions in the Unity editor:
1. Add the project in Unity Hub and open it.
2. In the bottom left, under the "Project" tab, navigate to the "Assets" folder.
3. Create a new folder called "Editor". This must be named exactly editor for unity to recognize it.
4. In the "Editor" folder, drag and drop BuildBundles, RelinkLocalization, and your LogOutput.log (from LocalizationLogger.dll) files from your file system.
5. You should now have "Build" and "Tools" tabs at the top of your editor. These will be explained later, as long as there are there you can procceed.
6. In the "Assets/Scenes/Holes" folder, create a new folder called "Custom Holes".
7. Copy and paste a level of your choosing into the custom holes folder and rename it.
8. Open your new level.
9. In the top left under "Hierarchy" search for "terrain" and select the game object.
10. On the right side in the inspector, find the terrain component and click the furthest right of the 5 tabs inside it, it should be a mountain with a cogwheel.
11. In there, under material click the concentric circles. This will have a popup with all of the materials in the project.
12. From there, search or find "default-terrain-standard" and double click.
13. Just above the camera view of the hole, there should be multiple circle options. Find the one named "shaded wireframe draw mode" and select it. It should be a cresent moon with a plus across it.
14. Modify to your hearts content, and proceed to the building section.

Instructions to build your custom hole:
1. From the tools tab at the top, select "RelinkLocalization". This must be done on each map (but only once needed), and it only applies to the current open one. This will fix most of the text in levels.
3. From the build tab at the top, select "clear all asset bundle names". This will wipe the naming from all of the bundles that were imported.
    This is not necessary to do each time, from the search bar to the right just under the "Project" tab, you can type "b:" to see what all is in bundles. You can have multiple maps in one bundle.
4. To add your map(s) to a bundle, select them in the project tab and in the bottom right, create a new assetBundle. Name it "{username}.{packname}". For example, what I am using to test is "mac.testhole".
5. To create the bundle, under the build tab at the top, select "Build AssetBundles". This may take serveral minutes.
6. Navigate to your project in your file system, and go to the "Assets/Bundles" folder. The file named "{username}.{packname}" is the only one that you need.
7. Copy that file into the "BepInEx/plugins/SBGMaps/Maps" folder where your game is installed.
