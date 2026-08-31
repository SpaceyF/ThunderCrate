# ThunderCrate

a subscribe button for bonelab mods. you're on a mod page on thunderstore, you hit subscribe, and it just shows up in your mods folder. no mod manager, no unzipping, no dragging dlls around.

it's two little pieces that talk to each other:

- **the app** is a tiny thing that sits in your tray and does the actual downloading and dropping mods where they go
- **the extension** adds the subscribe button to thunderstore, made to look like it was always part of the page

they talk over localhost (127.0.0.1), nothing ever leaves your pc.

## setting it up

### the app

build it once:

```
cd app
dotnet build -c Release
```

then run `app/bin/Release/net10.0-windows/ThunderCrate.exe`. you get a little lightning icon in your tray. first time it runs it finds your bonelab mods folder on its own, and if it guesses wrong just right click the tray icon and set it.

right click the tray for:

- open / set mods folder
- install dependencies (on by default, grabs stuff like bonelib if a mod needs it)
- run at startup
- a status window with a log of what got installed

### the extension (zen / firefox)

not on the firefox store yet, so for now:

1. go to `about:debugging#/runtime/this-firefox`
2. load temporary add-on
3. pick `extension/manifest.json`

that sticks around until you restart your browser. once it's signed on AMO you'll be able to install it for real.

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
