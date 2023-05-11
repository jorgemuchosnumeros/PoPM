# PoPM
Can a modded video game with crappy multiplayer support convey a flavor?

## Installation/Update

### IMPORTANT: Always run the game from the steam launcher, otherwise the game will break at start

### Windows (Automatic)
Download & Drag the `PoPM_Installer(Windows_x64).bat` from [releases](https://github.com/Jorgegzz/PoPM/releases) to the game's root folder (where `Pineapple on pizza.exe` is located). Then run `PoPM_Installer(Windows_x64).bat`.

**Note: If it is the first time install, you may run the game twice, so BepInEx (the mod loader) has a chance to configure itself.**

### Windows (Manual)
This mod uses [BepInEx Version 5](https://github.com/BepInEx/BepInEx/tree/v5-lts) here is the [tutorial](https://docs.bepinex.dev/articles/user_guide/installation/index.html) on installing it.

Once with BepInEx installed on the game, download `PoPM.dll` and `PoPM.pdb` from the [releases](https://github.com/Jorgegzz/PoPM/releases) and drag both files to `Pineapple on pizza\BepInEx\plugins`.

Now, [download Steamworks.NET Standalone](https://github.com/rlabrecque/Steamworks.NET/releases/download/20.1.0/Steamworks.NET-Standalone_20.1.0.zip) and extract both`steam_api64.dll` & `Steamworks.NET.dll` from the `Windows-x64` folder.

Drag `steam_api64.dll` to the root folder (where `Pineapple on pizza.exe` is located) and Drag `Steamworks.NET.dll` to `Pineapple on pizza\BepInEx\plugins`.

### Mac and Linux
Soon™ (Use Wine or Proton in the meantime)

## Usage
Press the `M` key to close the lobby menu if you are not in a lobby.

### Hosting
Click the **Host** button.

![Host Button](http://scr.antonioma.com/DScq)

Share the Lobby ID to your friends, we added a <b>Copy ID</b> button for your convenience.

![Empty Lobby](http://scr.antonioma.com/2Z83)

Once a guest joins, they will appear on the players list.

![People Joining to the Lobby](http://scr.antonioma.com/tfEL)

Once everyone is ready, start the game from the normal menu.

![Play](http://scr.antonioma.com/huJ0)

### Joining 
Click the **Join** button

![Join Button](http://scr.antonioma.com/enpX)

Type or paste the lobby ID on the field above the <b>Join</b> button and the click it.

![The other join button](http://scr.antonioma.com/MiD7)

You joined a lobby, now wait for the host to start.

![Joining to the Lobby](http://scr.antonioma.com/NbZw)

## Building
**Note: You'll need the a several amount of dependencies to build the project, create a `libs/` folder on the project to hold them.**

- Rider or Visual Studio is recommended for building
- .NET Framework 4.6 is necessary

### The following dependencies are found on `Pineapple on pizza\Pineapple on pizza_Data\Managed`:
- Assembly_CSharp.dll
- netstandard.dll
- Unity.TextMeshPro.dll
- UnityEngine.UI.dll

You will also need `Steamworks.NET.dll` found [here](https://github.com/rlabrecque/Steamworks.NET/releases/download/20.1.0/Steamworks.NET-Standalone_20.1.0.zip)

## Special thanks
- To [ABigPickle](https://github.com/iliadsh) for letting me repurpose part of [RavenM](https://github.com/iliadsh/RavenM) for this project
- To [5ro4](https://github.com/5ro4) for making such a fine game 🧐🍷
- To [AntonioMA](https://github.com/4nt0n10M4) and [Radsi889](https://github.com/radsi) for their contributions
- And to rednes_, imPeko, CrimsonScarf and eltete4 for offering to test the mod