# FocusHeure — DESIGN.md

> Mémoire design du projet. L'IA doit lire ce fichier avant toute modification UI.  
> Template : `KNOW-PAT-144` / knowledge-core.

## Identité

- **Audience :** utilisateur Windows 11 voulant des créneaux de concentration imposés.
- **Ton :** utilitaire discret, zéro marketing dans l'outil.
- **En une phrase :** Écran blanc plein écran multi-moniteur pendant les créneaux configurés, avec Chrome seul autorisé au-dessus.

## Interdits (anti-slop)

- Pas de hero / gradient / cards dans l'overlay.
- Pas de modales superflues.
- Pas de rafraîchissement visuel ou de `LockStateChanged` inutile.
- Pas de perte silencieuse de données utilisateur.

## Tokens

| Token | Valeur | Usage |
|-------|--------|-------|
| `OverlayBackground` | `#FFFFFF` | Fond plein écran |
| `OverlayText` | `#111111` | Heure, message |
| `OverlayMuted` | `#666666` / `#999999` | Sous-textes |
| `Accent` | `#111111` | Bouton Chrome |
| `Surface` | `#FAFAFA` | Fenêtre paramètres |

## Typographie

- **Overlay :** BebasNeue (display heure).
- **UI :** Segoe UI / Segoe UI Light.

## Stack

- .NET 9 WPF + Windows Forms (`NotifyIcon`, `Screen`).
- `System.Drawing` pour l'icône tray.
- P/Invoke `user32.dll` / `shcore.dll` pour positionnement multi-écran et élévation Chrome.
- Persistance JSON : `%AppData%\FocusHeure\settings.json` + `stats.json`.

## Architecture

```
┌─────────────────────────────────────┐
│  Tray (NotifyIcon)                  │
├─────────────────────────────────────┤
│  App                                │
│  ├─ FocusSessionController          │
│  │  ├─ RestrictionSchedule          │
│  │  ├─ OverlayManager               │
│  │  ├─ PauseHotkeyService           │
│  │  └─ ChromeLauncher / Elevator    │
│  ├─ SettingsService (singleton)     │
│  └─ StatsService (singleton)        │
└─────────────────────────────────────┘
```

## Composants

- `OverlayWindow` : fenêtre plein écran par moniteur.
- `SettingsWindow` : paramètres horaires, Chrome, PIN, démarrage.
- `PinDialog` : saisie PIN pour quitter.
- `TrayIconFactory` : génération icône 32×32 dynamique.

## Raccourcis

- `Échap + Entrée` : pause / reprise (uniquement en restriction).
- `Alt + F4` : bloqué sur l'overlay.

## Références

- Microsoft Windows app design : https://learn.microsoft.com/windows/apps/design/
- Mobbin — apps de focus / productivité : https://mobbin.com/browse/apps
- Pastel — overlay annotation : https://pastel.software

## Quality gate

Avant commit :

```powershell
dotnet build -c Release
python "C:\Users\Gaming.9G2R0OVE\Desktop\knowledge-core\meta\validators\project-excellence.py" "C:\Users\Gaming.9G2R0OVE\CascadeProjects\FocusHeure" --strict
python "C:\Users\Gaming.9G2R0OVE\Desktop\knowledge-core\meta\validators\quality-gate.py" "C:\Users\Gaming.9G2R0OVE\CascadeProjects\FocusHeure" --strict
```
