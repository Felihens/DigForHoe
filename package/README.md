# DigForHoe

**[English](#english) · [Português](#português)**

---

## English

The hoe can level and raise the ground, but to dig you have to switch to the pickaxe and swing at your feet. This mod adds a **Dig** option to the hoe menu, right after *Raise ground*: it lowers the ground exactly like a pickaxe hit, at the spot you aim at, with the hoe's placement preview.

### Features

- **Same dig as the pickaxe:** same depth, same size, same rules as a pickaxe hit on the ground. Hit the same spot again to dig deeper.
- **No cost:** Dig needs no materials, like digging with a pickaxe.
- **No workbench needed:** dig anywhere, far from any workbench, like with a pickaxe.
- **Other players don't need the mod.** Terrain changes are shared over the network by name, and Dig sends the name of the pickaxe's own dig. Whoever handles that patch of ground applies a normal pickaxe dig, with or without this mod.
- Wards and no-build areas work as usual.
- **Level ground that really levels.** The game levels to the height of the spot you click, so on uneven ground each stroke ends at a different height. Here every stroke keeps one height: that of the **level ground right beside it**, else of your last stroke nearby, else of your feet. Hold **Shift** (alternative place) to start a new flat at your feet. The preview shows at that height and a label under the crosshair says how much it raises or digs (`Level +0.4 m`). A stroke that would raise or dig more than 2 m anywhere under it is refused. The game's edge smoothing is kept, so the flat still blends into the ground around it. This also works for other players without the mod: the stroke is sent with its height.

### Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)

### Installation

**r2modman / Thunderstore Mod Manager:** click *Install*. To install a downloaded zip, go to Settings → *Import local mod*.

**Nexus Mods / Vortex:** also on [Nexus Mods](https://www.nexusmods.com/valheim/mods/4899).

**Manual:**
1. Extract the zip.
2. Copy the `plugins/DigForHoe` folder into `Valheim/BepInEx/plugins/`.
   Result: `Valheim/BepInEx/plugins/DigForHoe/DigForHoe.dll`.
3. Launch the game.

To update, delete the old `DigForHoe` folder before copying the new one.

### Configuration

`BepInEx/config/felih.DigForHoe.cfg`:

- `SmartLevel` (on): the level ground above. Off = the game's own levelling.
- `MaxChange` (2 m): how much a level stroke may raise or dig at most.
- `DetailedLog` (off): writes details to the BepInEx log for bug reports.

### Notes

- Works well with **Build Tweaks** and **Gather Tweaks** (same author).

---

## Português

A enxada nivela e levanta o chão, mas para cavar você precisa trocar para a picareta e bater no pé. Este mod adiciona a opção **Dig** no menu da enxada, logo depois de *Raise ground*: ela abaixa o chão exatamente como um golpe de picareta, no ponto em que você mira, com a prévia de posicionamento da enxada.

### Recursos

- **A mesma escavação da picareta:** mesma profundidade, mesmo tamanho e mesmas regras de um golpe de picareta no chão. Use de novo no mesmo ponto para cavar mais fundo.
- **Sem custo:** o Dig não precisa de materiais, assim como cavar com a picareta.
- **Sem bancada:** cave em qualquer lugar, longe de qualquer bancada, assim como com a picareta.
- **Os outros jogadores não precisam do mod.** As mudanças no terreno são enviadas pela rede pelo nome, e o Dig envia o nome da escavação da própria picareta. Quem cuida daquele pedaço de chão aplica uma escavação normal de picareta, com ou sem este mod.
- Áreas de proteção (wards) e áreas onde não se pode construir funcionam como no jogo normal.
- **Nivelar que nivela de verdade.** O jogo nivela pela altura do ponto em que você clica, então em terreno irregular cada golpe termina numa altura diferente. Aqui todo golpe mantém uma altura só: a do **chão já nivelado ao lado**, senão a do seu último golpe por perto, senão a dos seus pés. Segure **Shift** (posicionamento alternativo) para começar um plano novo na altura dos seus pés. A prévia aparece nessa altura e um texto abaixo da mira diz quanto ele sobe ou cava (`Level +0.4 m`). Um golpe que precisaria subir ou cavar mais de 2 m em algum ponto é recusado. A suavização das bordas do jogo continua, então o plano ainda se junta ao chão em volta. Funciona também para outros jogadores sem o mod: o golpe vai com a altura.

### Requisitos

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)

### Instalação

**r2modman / Thunderstore Mod Manager:** clique em *Install*. Para instalar um zip baixado, use Configurações → *Import local mod*.

**Nexus Mods / Vortex:** também está no [Nexus Mods](https://www.nexusmods.com/valheim/mods/4899).

**À mão:**
1. Extraia o zip.
2. Copie a pasta `plugins/DigForHoe` para dentro de `Valheim/BepInEx/plugins/`.
   Resultado: `Valheim/BepInEx/plugins/DigForHoe/DigForHoe.dll`.
3. Abra o jogo.

Para atualizar, apague a pasta `DigForHoe` antiga antes de copiar a nova.

### Configuração

`BepInEx/config/felih.DigForHoe.cfg`:

- `SmartLevel` (ligado): o nivelamento acima. Desligado = o nivelamento do próprio jogo.
- `MaxChange` (2 m): quanto um golpe de nivelar pode subir ou cavar no máximo.
- `DetailedLog` (desligado): escreve detalhes no log do BepInEx para relatar bugs.

### Observações

- Combina com o **Build Tweaks** e o **Gather Tweaks** (mesmo autor).
