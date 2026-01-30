# Death and access

A mod for Death and Taxes that adds screenreader+tts output and keyboard+controller support. Works with NVDDA and JAWS. Narrator is not supported. OSX Voiceover and Linux orca should work, but have not been tested. Please open an issue if the mod does not detect your screenreader!

Built in VSCode with c# and codex extensions.

## installation
- buy the game from Placeholder Gameworks on steam or  itch
- install MellonLoader and point it to the game.
- download the mod release
- Copy `Death and Access.dll` and `nvdaControllerClient.dll` into the game's `Mods` folder.
- Launch the game. MellonLoader should create a commmand window with text to indicate MellonLoader and the mod have both initialized. Your screenreader should spaek this text, or TTS if no screenreader. Then the game will launch.

## Uninstall
Remove `Death and Access.dll` from the `Mods` folder.

## Controls
controller:
left analog stick: move to closest item
x/a button: choose selected item

General:
- Arrow keys: move to the nearest object or menu item.
- Enter/Return: select current item.

Grim's office (note: items cannot be used from in a closed drawer, or if they have not been purchased from the shop)
- [: open left drawer.
- ]: open right drawer.
- i: read instruction letter.
- n: read phone news.
- m: pick up/drop Marker of Death.
- 1–0: open/close the corresponding paperwork profile.
- l: mark live on current paperwork (Marker must be held first).
- d: mark Doomed on current paperwork (Marker must be held  first)
- f: use fax machine to end shift.
- b: use Oink Bank (money box).
- e: pick up/drop Eraser.
- s: spin spinner.
- t: squeeze toy.
- a: toggle lamp.
- g: look at chaos globe.
- c: flip decision coin.
- r: use radio.
- p: strum cactus.

The Elevator:
- s: Mortimer's emporium.
- f: Fate's office.
- g: Grim's office.
- d: dressing room.
- q: quarters.
- b: Cerberus den.
- 1–0: select elevator floor

anywhere Outside the office:
- m: speak current money.
- 1–0: select dialog option.

## Nothing happens when I launch the game!

The intro scene doesn't work yet. For now just press enter to select "skip intro," then enter again to confirm.

## I can't do anything in the elevator!:
- You can only go to Grim's office at the start of a day.
- You must visit Fate's office before ending the day in your quarters.
- The Cerberus Den can only be visitted on weekends.
- The elevator takes a few seconds to move from floor to floor.

## issues to fix
- Options menu sliders do not announce their current value when moved with left/right arrows.
- The focus in the options menu is screwy in general
- Profiles occasionally don't read when they are selected
- Confirm dialogs don't always read the text first
- focus gets stuck sometimes on the elevator button for Fate's office
- Eraser doesn't work with profile number row shortcuts, use arrows+enter instead

## Long term goals:
- Investigate cases where items can be accessed when they shouldn't be available
- fix intro scene
- Full controller support via SDL
- Narrator support
- describe cosmetics
- drag-and-drop (for moving items in and out of drawers or on the desk)