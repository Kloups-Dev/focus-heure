# Focus Heure

Application Windows 11 — écran blanc plein écran pendant vos créneaux de concentration (multi-moniteur, tray, faible CPU).

## Fonctions

| Fonction | Description |
|----------|-------------|
| Créneaux horaires | Configurables dans **Paramètres** (JSON sauvegardé) |
| Compte à rebours | Temps restant avant la fin du créneau |
| Chrome | Reste en mode focus : seules les fenêtres Chrome passent **au-dessus** de l'écran blanc ; les autres apps restent bloquées dessous |
| Pause | **Échap + Entrée** (durée configurable) |
| Alerte tray | Notification X min avant un créneau |
| Week-end libre | Option dans les paramètres |
| Jour libre | Menu tray — pas de restriction jusqu'à demain |
| Mode forcé | « Activer le mode maintenant » |
| Test | Aperçu de l'écran 30 secondes |
| PIN | Code pour quitter pendant un créneau actif |
| Statistiques | Sessions et minutes de focus enregistrées |
| Démarrage auto | Registre Windows + option dans les paramètres |

Les créneaux peuvent traverser minuit, par exemple `22:00 → 02:00`.

## Créneaux par défaut

| Période |
|--------|
| 00:00 → 08:00 |
| 12:00 → 13:00 |
| 17:00 → 18:00 |
| 19:30 → 20:30 |

## Fichiers config

`%AppData%\FocusHeure\`

- `settings.json` — horaires, pause, PIN, Chrome, etc.
- `stats.json` — historique de focus

## Compilation

```powershell
cd "C:\Users\Gaming.9G2R0OVE\Desktop\cursor project\FocusHeure"
dotnet build -c Release
dotnet run -c Release
```

Exécutable : `bin\Release\net9.0-windows\FocusHeure.exe`

Une compilation Windows est vérifiée automatiquement par GitHub Actions à chaque
push et chaque pull request vers `main`. L'action produit également un exécutable
portable `win-x64` téléchargeable depuis l'onglet **Actions**.

## Tray

Clic droit sur l’icône **F** (noir = actif, gris = libre). Double-clic = paramètres.
