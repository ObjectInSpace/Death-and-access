Death and Access: a mod to make Death and Taxes playable by the blind

# What this does:

* enables TTS output for game text
* detects NVDA and JAWS screenreaders. Should detect VO and Orca too but have not been tested. Narrator doesn't work yet.
* arrow keys now move the mouse pointer. Enter selects what's under it. This should also work for the left analog stick and primary action button for your controller.
* Drag-and-drop: press space to pick up the item under the cursor, arrow keys to move it, and space again to put it down.
* Keyboard shortcuts to select things without relying on the pointer

# How to use:

* Buy the game from Placeholder Gameworks (https://www.deathandtaxesgame.com) or from Steam/Itch directly
* Install MellonLoader (https://github.com/LavaGang/MelonLoader/releases ) and point it to your game
* Copy the mod DLL and NVDA controller client DLL to the mods folder of your game
* Launch Death and Taxes. If Mellonloader is working a command window will open with info about the game. You should then hear screenreader (or TTS if no screenreader is detected) that the mod has loaded.
* et voila!
* 

# keyboard shortcuts:

## everywhere:

* arrows, controller left stick: move pointer
* enter, controller action button: select

## Grim's office

* space: toggle drag and drop
* n: phone news
* i: instruction letter
* m: pick up marker of death
* 1-0: read profile
* ctrl/command+1-0: mark live
* &nbsp;- alt/option+1-0: mark  die

  * f: deus fax machina 
  * \- \[: open/close left drawer

* ]: open/close right drawer
* g: globe
* s: spinner
* c: decision coin
* b: oink bank
* r: radio
* e: eraser
* l: lamppp
* p: plant

## elevator:

* 1-0: move to elevator floor
* f: Fate's office
* s: shop
* d: dressing room
* q: your quarters
* g: Grim's office
* b: Cerberus Den

## Dressing room:

* ## h: next head
* ## shift+h: previous head
* ## : next body
* ## shift+b: previous body
* ## M: mirror (exit)

## Anywhere else:

* m: say current money
  e: elevator
* `: read last spoken dialog
* 1-0: choose dialog option

# known issues

* The intro comic doesn't work yet. For now press enter to skip and enter again to confirm.
* The focus in menus is messy
* dialog shortcuts don't work in the dressing room

# The elevator doesn't work!

* After leaving the office, you can't go to your quarters until you go to Fate's office.
* You can't go back to your office until you end the day in your quarters
* After ending the day in  your quarters, you must go to Grim's office before you can go to Fate's office.
* the dressing room is unlocked after you purchase the mirror
* The cerberus den is only open on weekends.
* It may take a few seconds for the elevator to move to your chosen location.
