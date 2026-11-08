# DigForHoe

A BepInEx mod for Valheim: adds a Dig option to the hoe that lowers the ground exactly like a pickaxe hit, wherever you aim. Other players don't need the mod.

Mod para Valheim (BepInEx): adiciona a opção Dig na enxada, que abaixa o chão exatamente como um golpe de picareta, onde você mira. Os outros jogadores não precisam do mod.

For players, see [package/README.md](package/README.md). That file is also the Thunderstore page.
Para jogadores, veja [package/README.md](package/README.md).

Download: [Thunderstore](https://thunderstore.io/c/valheim/p/DJAventuras/DigForHoe/) · [Nexus Mods](https://www.nexusmods.com/valheim/mods/4899) · [GitHub Releases](https://github.com/Felihens/DigForHoe/releases)

Sister mods / mods irmãos: [Gather Tweaks](https://github.com/Felihens/GatherTweaks), [Build Tweaks](https://github.com/Felihens/BuildTweaks), [DigForHoe](https://github.com/Felihens/DigForHoe), [Resource Scanner](https://github.com/Felihens/ResourceScanner), [Ore Weak Spots](https://github.com/Felihens/OreWeakSpots), [Tree Weak Spots](https://github.com/Felihens/TreeWeakSpots).

## Building

`sh build.sh` compiles `src/*.cs` against the game's assemblies and installs the dll into `BepInEx/plugins/DigForHoe/` (`sh build.sh nocopy` only compiles). Then `powershell -File pack.ps1` builds `dist/DigForHoe-<version>.zip` from `package/`.

## License

MIT
