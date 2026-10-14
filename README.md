# duran

CLI para instalar, cambiar y controlar shells de [Quickshell](https://quickshell.org) en Hyprland.
Cada shell describe lo que necesita en un `shell.toml`; duran lo lee y hace el resto.

Hecho en C# (.NET 10) y compilado con NativeAOT: un único binario, sin runtime de .NET instalado.

## Instalación

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
| `duran uninstall <shell>` | Lo para, ejecuta su hook de desinstalación y borra el clon (si es remoto) |
| `duran update [shell]` | `git pull` de los shells remotos y reinicia los activos |
| `duran switch <shell>` | Para el shell actual y arranca el elegido |
| `duran start \| stop \| restart [shell]` | Sobre el shell activo (o el indicado) |
| `duran status` | Shell activo y si está corriendo |
| `duran ipc [acción] [args]` | Ejecuta una acción `[ipc]` del shell activo; sin acción, las lista |
| `duran new <nombre> [--dir carpeta]` | Crea un shell mínimo (`shell.toml`, `shell.qml`, hooks). Con `--dir` lo crea ahí y lo enlaza |
| `duran completions fish` | Imprime el autocompletado para fish |

Opciones de `install`: `--no-packages`, `--no-hook`, `--switch`. De `uninstall`: `--no-hook`, `--yes`.

### Autocompletado (fish)

```sh
duran completions fish > ~/.config/fish/completions/duran.fish
```

### Dónde busca los shells

- **Locales:** `~/.config/quickshell/<nombre>/shell.toml` (p. ej. enlazados con stow desde tus dotfiles).
- **Remotos:** `~/.local/share/duran/shells/<repo>`, enlazados en `~/.config/quickshell/<nombre>`.

El shell activo se guarda en `~/.local/state/duran/active`.

## Hyprland independiente del shell

Usa duran en el autostart y en los atajos; así funcionan con cualquier shell activo:

```lua
hl.exec_cmd("~/.local/bin/duran start")                                         -- autostart
create_bind("SUPER + SUPER_L", hl.dsp.exec_cmd("~/.local/bin/duran ipc launcher"))
create_bind("XF86AudioRaiseVolume", hl.dsp.exec_cmd("~/.local/bin/duran ipc volume_up"))
```

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
```

El resto de secciones (`[fonts]`, `[system]`, `[dirs]`…) duran las ignora de momento; puedes
usarlas como documentación del shell.