# Death and Access: a mod to make Death and Taxes playable by the blind

## What this does:

* enables TTS output for game text
* outputs text to detected screenreaders.
* Adds keyboard support, including drag and drop and full keyboard shortcuts
* Adds basic controller support

## How to use:

* Buy the game from Placeholder Gameworks (https://www.deathandtaxesgame.com) Available on Steam, Itch or GOG.
* Install MellonLoader (https://github.com/LavaGang/MelonLoader/releases ) and point it to your game
* Copy the mod DLL and NVDA controller client DLL to the mods folder of your game
* Launch Death and Taxes. If Mellonloader is working, a command window will open with info about the game. You should then hear screenreader (or TTS if no screenreader is detected) that the mod has loaded.
* et voila!

## keyboard shortcuts:

### everywhere:

* arrows, controller left stick: move pointer
* enter, controller action button: select
* y: select yes in confirm dialogs
* n: select no in confirm dialogs

### Intro/end comics:

* left/right arrows: scrolls comic
*  `: reads comic text
*  enter: skips to the end of the comic.

### Grim office

* space: toggle drag and drop
* n: phone news
* i: instruction letter
* m: pick up marker of death
* 1-0: read profile
* ctrl/command+1-0: mark live
*  - alt/option+1-0: mark  die
  * f: deus fax machina
  * [: open/close left drawer
* ]: open/close right drawer
* g: globe
* s: spinner
* c: decision coin
* b: oink bank
* r: radio
* e: eraser
* l: lamppp
* p: plant

### elevator:

* 1-0: move to elevator floor
* f: Fate's office
* s: shop
* d: dressing room
* q: your quarters
* g: Grim's office
* b: Cerberus Den

### Dressing room:

*  h: next head
*  shift+h: previous head
* b: next body
* shift+b: previous body
* M: mirror (exit)

### Mortimer's Emporium:

* 1-3: hover shop item, read its name and description

### Anywhere else:
* m: say current money
* e: elevator
* `: read last spoken dialog
* 1-0: choose dialog option

## The elevator doesn't work!

* After leaving the office, you can't go to your quarters until you go to Fate's office.
* You can't go back to your office until you end the day in your quarters
* After ending the day in  your quarters, you must go to Grim's office before you can go to Fate's office.
* the dressing room is unlocked after you purchase the mirror
* The cerberus den is only open on weekends.
* The elevator moves to your chosen location, which takes a few seconds.

## known issues/todos:

* menu focus is messy
* dressing room conversation choices just say "speech bubble" and aren't being read when hovered
* The eraser doesn't work with a number row shortcut, use arrow keys instead.
* No support for hats!
* broken Oink bank shortcut
* No shortcut for spirit detection
* Narrator isn't detected
* No proper controller support (needs SDL)
* Untested compatibility on OSX and Linux
* Mobile+console ports are out of scope, due to no MellonLoader

If your issue isn't listed, feel free to open one.

Built by dotnet 4.72 in VSCode with OpenAI Codex.

## Follow me!

### twitch:
https://twitch.tv/object_inspace
### Mastodon:
https://infosec.exchange/@prism
### Say thanks!:
https://paypal.me/justsendyourcash

Have fun!!
