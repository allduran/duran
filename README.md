# duran

CLI para instalar, cambiar y controlar shells de [Quickshell](https://quickshell.org) en Hyprland.
Cada shell describe lo que necesita en un `shell.toml`; duran lo lee y hace el resto:
paquetes, hooks, arranque, acciones IPC y su parte de la config de Hyprland.

Hecho en C# (.NET 10) y compilado con NativeAOT: un único binario, sin runtime de .NET instalado.

## Instalación

**Release** (binario listo):

```sh
curl -L https://github.com/allduran/duran/releases/latest/download/duran-$(curl -s https://api.github.com/repos/allduran/duran/releases/latest | grep -oP '"tag_name": "\K[^"]+')-linux-x64.tar.gz | tar -xz -C /tmp
install -Dm755 /tmp/duran ~/.local/bin/duran
duran completions fish > ~/.config/fish/completions/duran.fish
```

**Desde el código:**

```sh
sudo pacman -S --needed dotnet-sdk clang
git clone https://github.com/allduran/duran && cd duran
dotnet publish src/Duran -c Release -o out
install -Dm755 out/duran ~/.local/bin/duran
```

## Comandos

| Comando | Qué hace |
|---|---|
| `duran list` | Shells disponibles (● = corriendo) |
| `duran info <shell>` | Lo que declara su `shell.toml` |
| `duran install <shell \| url \| usuario/repo>` | Instala paquetes y ejecuta su hook. Con URL o `usuario/repo` lo clona primero |
| `duran uninstall <shell>` | Lo para, ejecuta su hook de desinstalación, quita su config de Hyprland y borra el clon (si es remoto) |
| `duran update [shell]` | `git pull` de los shells remotos y reinicia los activos |
| `duran switch <shell>` | Para el shell actual, enlaza la config de Hyprland del nuevo y lo arranca |
| `duran start \| stop \| restart [shell]` | Sobre el shell activo (o el indicado) |
| `duran status` | Shell activo y si está corriendo |
| `duran ipc [acción] [args]` | Ejecuta una acción `[ipc]` del shell activo; sin acción, las lista |
| `duran new <nombre> [--dir carpeta]` | Crea un shell mínimo. Con `--dir` lo crea ahí y lo enlaza |
| `duran completions fish` | Imprime el autocompletado para fish |

Opciones de `install`: `--no-packages`, `--no-hook`, `--switch`. De `uninstall`: `--no-hook`, `--yes`.

### Dónde busca los shells

- **Locales:** `~/.config/quickshell/<nombre>/shell.toml` (carpetas propias o enlaces, p. ej. de stow).
- **Remotos:** `~/.local/share/duran/shells/<repo>`, enlazados en `~/.config/quickshell/<nombre>`.

El shell activo se guarda en `~/.local/state/duran/active`.

## Hyprland

### Atajos y autostart independientes del shell

Llama a duran en vez de al shell; así funcionan con cualquier shell activo que implemente la acción:

```lua
hl.exec_cmd("duran start")                                          -- autostart
create_bind("SUPER + SUPER_L", hl.dsp.exec_cmd("duran ipc launcher"))
create_bind("XF86AudioRaiseVolume", hl.dsp.exec_cmd("duran ipc volume_up"))
```

Usar los mismos nombres de acción en todos tus shells (`launcher`, `settings`, `lock`, `volume_up`…)
hace que los atajos no cambien al cambiar de shell.

### Config de Hyprland de cada shell

Un shell puede traer su parte de Hyprland (colores, huecos, efectos, layer rules…) en un lua:

```toml
[hyprland]
config = "hypr/shell.lua"
```

Al hacer `duran switch`, duran enlaza ese archivo en `~/.config/hypr/shell.lua` y ejecuta `hyprctl reload`.
Si el shell no declara ninguno, quita el enlace. Un `shell.lua` que sea un archivo normal (no un enlace) no se toca.

Tu config de Hyprland lo carga si existe, por ejemplo así:

```lua
-- ~/.config/hypr/theme.lua
local defaults = { accent = "888888", inactive = "111111", gapsIn = 5, gapsOut = 10 }
local ok, shell = pcall(require, "shell")
local theme = ok and type(shell) == "table" and shell.theme or {}
return setmetatable(theme, { __index = defaults })
```

```lua
-- donde lo uses
local t = require("theme")
hl.config({ general = { gaps_in = t.gapsIn, gaps_out = t.gapsOut } })
```

El lua del shell puede aplicar sus reglas directamente y devolver lo que tu config lea:

```lua
-- <shell>/hypr/shell.lua
hl.layer_rule({ match = { namespace = "mishell-.*" }, no_anim = true })
return { theme = { accent = "c0caf5", inactive = "16161e", gapsIn = 5, gapsOut = 10 } }
```

> Si usas stow y `~/.config/hypr` es un enlace a tus dotfiles, añade `shell.lua` a su `.gitignore`.

## Crear un shell

```sh
duran new mishell --dir ~/Projects      # crea ~/Projects/mishell y lo enlaza en ~/.config/quickshell/mishell
cd ~/Projects/mishell
git init && git add -A && git commit -m "mishell: base"
duran switch mishell                    # pruébalo; `duran restart` recarga tras cada cambio
```

Lo que genera:

| Archivo | Para qué |
|---|---|
| `shell.toml` | Nombre, paquetes, acciones `[ipc]`, conflictos, hooks y `[hyprland]` |
| `shell.qml` | Una barra mínima por pantalla con una acción IPC (`duran ipc bar`) |
| `hypr/shell.lua` | Su parte de Hyprland (tema y layer rules) |
| `hooks/install.sh`, `hooks/uninstall.sh` | Lo que no son paquetes: servicios, grupos, carpetas de estado… |

Publicado en GitHub, cualquiera lo instala con `duran install usuario/mishell --switch`.

## Formato de `shell.toml`

Todo es opcional salvo `[shell].name`. Las rutas son relativas a la carpeta del shell.

```toml
[shell]
name = "velvet"
description = "Shell estilo Persona para Hyprland"
version = "0.2.0"
engine = "quickshell"
start = "qs -c velvet -d"          # por defecto: qs -c <name> -d
stop = "qs -c velvet kill"         # por defecto: qs -c <name> kill

[packages]
repo = ["quickshell", "grim", "cliphist"]
aur = []

[ipc]                              # acciones para `duran ipc <clave>`
launcher = "qs -c velvet ipc call launcher toggle"
volume_up = "qs -c velvet ipc call osd volumeUp"
mode_set = "qs -c velvet ipc call mode set <modo>"   # <marcador> = argumentos de duran ipc

[conflicts]
shells = ["caelestia"]             # se paran al hacer `duran switch` a este shell

[hooks]
install = "hooks/install.sh"       # lo que no son paquetes: servicios, grupos, carpetas…
uninstall = "hooks/uninstall.sh"

[hyprland]
config = "hypr/shell.lua"          # se enlaza en ~/.config/hypr/shell.lua al hacer switch
```

El resto de secciones y claves (`[fonts]`, `[system]`, otras claves de `[hyprland]`…) duran las ignora;
puedes usarlas como documentación del shell.

## Licencia

MIT