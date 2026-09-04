# ThunderCrate

a subscribe button for bonelab mods. you're on a mod page on thunderstore, you hit subscribe, and it just shows up in your mods folder. no mod manager, no unzipping, no dragging dlls around.

it's two little pieces that talk to each other:

- **the app** is a tiny thing that sits in your tray and does the actual downloading and dropping mods where they go
- **the extension** adds the subscribe button to thunderstore, made to look like it was always part of the page

they talk over localhost (127.0.0.1), nothing ever leaves your pc.

**[get the extension on firefox add-ons](https://addons.mozilla.org/en-US/firefox/addon/thundercrate/)**

## setting it up

### the app

**[download the latest release](https://github.com/SpaceyF/ThunderCrate/releases/latest)**, unzip it anywhere, run `ThunderCrate.exe`. it's self contained so there's no .net to install and nothing to set up. windows smartscreen might warn you the first time since it isn't code signed, more info then run anyway.

you get a little lightning icon in your tray. first time it runs it finds your bonelab mods folder on its own, and if it guesses wrong just right click the tray icon and set it.

if you'd rather build it yourself:

```
cd app
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

right click the tray for:

- open / set mods folder
- install dependencies (on by default, grabs stuff like bonelib if a mod needs it)
- run at startup
- a status window with a log of what got installed

### the extension (zen / firefox)

it's signed and up on the firefox add-on store now, so it's just a normal install:

**[addons.mozilla.org/firefox/addon/thundercrate](https://addons.mozilla.org/en-US/firefox/addon/thundercrate/)**

zen works too, it's a firefox fork. open that link in zen and hit add to firefox like you would with anything else. no more signature flags, no about:debugging.

if you wanna run it from source instead, go to `about:debugging#/runtime/this-firefox`, load temporary add-on, pick `extension/manifest.json`. that version disappears when you restart your browser.

## using it

1. app running in your tray
2. open any bonelab mod on thunderstore
3. hit subscribe. it downloads and lands in your mods folder, launch bonelab and it's there.

## how the install actually works

- pulls the latest version straight from thunderstore's api
- mods that ship a `Mods/` (or UserData / UserLibs / Plugins) folder get dropped into the right spot
- mods that are just a loose dll go into `Mods/`
- never touches melonloader, you install that yourself
- grabs dependencies on its own if the toggle's on

## notes

- the app only listens on localhost and only does two things: tell the extension it's alive, and install a mod you clicked. nothing runs on its own.
- port is 48752. if something else is already using it, change `Port` in `%AppData%\ThunderCrate\config.json`
- pairs nicely with modcrate if you wanna manage what's already installed

made by nontendo.
