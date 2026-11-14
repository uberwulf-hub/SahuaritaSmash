# The move from App Hub: plan

Sahuarita Smash! leaves App Hub's Classic Games (PowerShell 5.1 + WPF, Windows only) for its own app on Windows and
Android (.NET 10 + Avalonia 12), built and signed by GitHub Actions and installed from SauceStore. Source in App Hub:
`Apps\ClassicGames\Engine\Brawl*.cs` (about 8,000 lines), `Music\Brawl\` (Andrew's Suno songs), `Brawl.ico`.

## Steps

1. **Done (2026-11-14): the repository and the match.** Projects, the five GitHub workflows (checks and test builds on
   every push, Android signing, release by hand), the store file. The engine: `BrawlSim`, `BrawlData`, `BrawlBot`, the
   signature attacks' data, unchanged. The checks that need only the engine (data, physics, knockback, recovery,
   mechanics, whole matches, hazards, stages). A stand-in screen (replaced in step 2).
2. **Done (2026-11-14): the pictures.** `BrawlArt.cs`, `BrawlBodies.cs`, `BrawlMotion.cs`, `BrawlScenery.cs` and the effects half of
   `BrawlSignatures.cs`, through a small layer with WPF's drawing names (`DrawingContext` with `Pop()`,
   `StreamGeometry` with WPF's figure calls, `Gfx`) over Avalonia's, so the files stay close to App Hub's. The draw
   and motion checks pass, and `-Snapshot` saves a fight on every stage. The app shows four computer players with
   the real pictures. Still to come with the game (step 3): the menus' pictures and the Lineup, Moves, Swings and
   Stampede scenes (they live in `Brawl.cs`).
3. **Done (2026-11-14): the game.** `Brawl.cs` (title, choosing fighters and the stage, the ladder, help, pause, results) on a small
   `Game` base like Classic Games' (keys held, taps, mouse / touch hit areas, menu buttons), `BrawlKeys` (changeable
   keys, `brawl-keys.txt`), the scorebook (`ladder-<difficulty>`), the game checks and the buffer checks. Saves through
   App Hub's safe-save helper; App Hub's old keys and ladder scores read once on Windows.
   Brought along with it: the mixer, the built-in tunes and the sounds made in code (`Audio.cs`, `Music.cs`,
   `BrawlSfx.cs`, `BrawlMusic.cs`), so the game's music logic and its checks run unchanged; they stay silent until
   step 4 gives them a sound output (`IAudioOut`). All 112 checks pass (App Hub's game, keys, buffer, music, draw,
   motion, stage and update checks, the keys fixture, bringing App Hub's saves across, 24 snapshot scenes). The
   title's "Back to the games" is "Quit" here.
4. **Sound.** An `IAudioOut` on each platform (Windows: waveOut, as App Hub; Android: AudioTrack), then Andrew's recorded songs
   (`Music\Brawl\*.mp3` + `music.txt`, decoded by the platform: Media Foundation on Windows, MediaCodec on Android),
   with a `credits` line for them.
5. **Controls.** Windows: both keyboard sets and up to four XInput controllers. Android: on-screen stick and buttons
   (attack, strong, special, shield, grab, jump; at least 44 px, thumbs not covering the fighters), Bluetooth game
   pads through Avalonia's key events, pause on Back.
6. **Release 0.1.0** (only when Andrew asks), then run *Back up the Android key* once, and retire the game from
   Classic Games if Andrew agrees (App Hub's CLAUDE.md, "Retired apps"; for a game inside Classic Games: remove its
   `$Games` entry and engine files, keep its saves, a CHANGELOG line saying where it went).

## Later

- Network play (the match is already deterministic; App Hub's LAN code is `Engine\Net*.cs` there).
