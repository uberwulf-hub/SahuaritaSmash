# Sahuarita Smash! (Sauce Edition): notes for Claude

Read [docs/PLAN.md](docs/PLAN.md) and [README.md](README.md) first. Decided with Andrew on 2026-11-14: Sahuarita Smash!
moves out of App Hub's Classic Games (`C:\Users\andre\Repos\AppHub`, GitHub `uberwulf-hub/ADocker`, also
`uberwulf-hub/AppHub`; the game was `Apps\ClassicGames\Engine\Brawl*.cs` there) into this repository, for Windows and
Android, the same way Learning Lab (Sauce Edition) (`uberwulf-hub/LearningLab-SauceEdition`) was made. App Hub's
skill `.claude/skills/sauce-edition-port` describes the recipe. Once this version is released, the game leaves
Classic Games (only if Andrew agrees; App Hub's CLAUDE.md, "Retired apps").

## Rules

- **Releases reach everyone who installed the app: never run the Release workflow or push a tag unless Andrew asks
  for that release.** The repository is `uberwulf-hub/SahuaritaSmash` (private), listed in SauceStore through its
  latest GitHub release (`saucestore.json`: Windows zip + Android APK; keep `credits` complete for every third-party
  file, library, font or recording).
- **Releasing: GitHub Actions only, like Learning Lab and Bored Games.** Set the version in `Directory.Build.props` and
  `src\SahuaritaSmash.Android\SahuaritaSmash.Android.csproj` (`ApplicationVersion` = major*10000 + minor*100 + patch,
  `ApplicationDisplayVersion` = X.Y.Z), move CHANGELOG.md's "Unreleased" lines under `## [X.Y.Z] - date`, push to
  main, then run the `Release` workflow with `vX.Y.Z` on main. **Signing is GitHub's:** the apk is signed in
  `android.yml` with the key from the `ANDROID_KEYSTORE_*` secrets or, until they exist, the key the first build made
  and keeps in the Actions cache (`keep-android-key.yml` keeps it alive; `backup-android-key.yml` turns it into the
  secrets). Never sign with another key and never commit a keystore. The Windows zip is unsigned. `build.yml` runs the
  checks and builds test copies on every push; it never releases.
- Every rule from App Hub's section on the game (below) still applies: **never Nintendo's names, characters, stages,
  items, music or logo; no living people, actors or look-alikes; nobody is hurt (bonks, a twinkle), no blood; every
  attack winds up 3+ frames; every stage surprise is warned 90 frames first; only Andrew's own Suno songs made on his
  Pro plan, never a free-plan song or anyone else's.**
- `BrawlSim.Step` stays the only place a match changes, from `BrInput`s, with its own seeded randomness: the same seed
  and buttons give the same match (`Checksum`), for network play later. The engine project has no UI types.
- Saved data (keys, scores, a kept match) persists until the person deletes it; every format change loads all older
  versions; never overwrite a save that failed to load (App Hub's safe-save rules). On Windows, read App Hub's saves
  once on first start (`%APPDATA%\ClassicGames`: the scorebook's `ladder-*` lines, `brawl-keys.txt`) and keep our own.
- Tests never touch real data, never open windows (Avalonia headless), never play audio, never use the network.
- Plain words for families. Touch first on Android: buttons at least 44 px, nothing hover-only.

## How it is built

- `src\SahuaritaSmash.Engine` (net10.0, no UI): copied from App Hub with the namespace `ClassicGames` kept so the
  files can be compared. The match: `Brawl\BrawlSim.cs`, `BrawlData.cs` (+ `BrawlInfo.GameName`, the name the music
  uses), `BrawlBot.cs`, `BrawlSignatures.cs` (its data half). Sound: `Audio\Audio.cs` (App Hub's mixer and synth
  without Hive's industrial instruments and other games' sounds; the device is an `IAudioOut` the platform gives
  `Audio.Start`, none yet: silent), `Music.cs` (only this game's tunes and `GameTunes`), `BrawlSfx.cs`, `BrawlMusic.cs`
  (recorded songs; Media Foundation, so Windows only, off until step 4). Saves: `ScoreBook.cs` (+ `AddKept`),
  `SafeSave.cs` (App Hub's `AppHubSaves.SafeFile`, internal, visible to the app and the checks), `UpdateResume.cs`
  (a match kept when the app closes), `CrashLog.cs` (just `Write`).
- `src\SahuaritaSmash` (Avalonia 12): screens shared by Windows and Android. `Brawl\` holds App Hub's pictures
  (`BrawlArt.cs`, `BrawlBodies.cs`, `BrawlMotion.cs`, `BrawlScenery.cs`, `BrawlSignaturesView.cs` = the drawing half of
  `BrawlSignatures.cs`) with only their `using` lines changed, plus a few marked spots in `BrawlScenery.cs` (bitmaps,
  gradients, pens). They draw through `Wpf\`: WPF's names on Avalonia (`DrawingContext` with `Push...` / `Pop()`,
  `StreamGeometry` with WPF's figure calls, the transforms, `DrawingVisual` for off-screen pictures, `Freeze()`,
  `Pics.FromBgra`), and `Wpf\Gfx.cs` (Classic Games' `Gfx`, fonts Inter). Points, rects, matrices, brushes and pens
  are Avalonia's. Keep new drawing code in that WPF style so files can still be compared with App Hub's.
  The game is `Brawl\Brawl.cs` + `BrawlKeys.cs` (App Hub's, only `using` lines changed; the title's last item says
  Quit) on `Wpf\Game.cs` (App Hub's `Game` base: pause, game over, scores, held / tapped keys, mouse hit areas, which
  are also how touch works), `Wpf\Pads.cs` (XInput controllers, none off Windows; `KartPads.Fake` for tests and
  touch) and `Wpf\NetLink.cs` (a stand-in: no network play yet, `Game.Net` stays null). Keys are Avalonia's `Key`
  (same names as WPF's, so `brawl-keys.txt` is shared with App Hub). `Views\GameView.cs` hosts a Game (App Hub's
  GameSurface: loop per frame, picture scaled to fit, keys with repeats, mouse and touch in game coordinates, errors
  to the log and back to the title, Back = Esc except on the title); `Views\MainView.cs` puts BrawlGame in it.
  `Services\Data.cs`: the folder (`%APPDATA%\SahuaritaSmashSE`, Android's files folder; `UseTestFolder` for checks)
  and `BringAcrossFromAppHub` (once, on Windows, only when we have nothing yet: App Hub's Brawl ladder scores and
  `brawl-keys.txt`, read only). `App.Testing` skips the start-up work; `App.QuitApp` is Android's way out.
- `src\SahuaritaSmash.Desktop` (Windows, `SahuaritaSmashSE.exe`), `src\SahuaritaSmash.Android`
  (`com.uberwulf.sahuaritasmashse`, landscape).
- `tests\SahuaritaSmash.Tests`: `dotnet run --project tests/SahuaritaSmash.Tests -c Release [-Only brawl,brawl-saves,
  brawl-snapshots] [-Snapshot dir]`: App Hub's Brawl test files copied whole (`BrawlTests.cs`, `BrawlKeysTests.cs`,
  `BrawlMusicTests.cs`, only `using` lines changed), the saves (`BrawlSaveChecks.cs`, fixtures in `Fixtures\`) and
  App Hub's snapshot scenes (`SnapshotScene`, drawn by Avalonia's headless platform with Skia); the exit code is the number of failures.
- Building in a cloud session: if dot.net's download host is blocked, `apt-get install dotnet-sdk-10.0` works; the
  Desktop project builds on Linux (`EnableWindowsTargeting`); the Android workload doesn't, so `build.yml` checks it.

## The game, as App Hub's CLAUDE.md describes it (2026-10-29 .. 2026-11-01)

Sahuarita Smash! (`Engine\Brawl*.cs`, 2026-10-29; Id `Brawl`, name only in `BrawlGame.GameName`; Andrew: "like super smash
bros, but with unique characters ... unique to me and my life", then "a mix of all of them") is the platform fighter.
It was Mesa Mayhem until Andrew renamed it on 2026-10-31 (his choice, "Sahuarita" being the family's town; scores and
keys are by Id, so nothing moved). **Never Nintendo's names, characters, stages, items (no "Super Smash Bros.", no
"Smash Bros.", no Smash Ball), music or logo; no living people, no actors, no look-alikes of anyone's characters.** The cast is data in `BrawlData.cs` (ten: Classic Games All-Stars
Bloom, Specimen, Trooper, Juniper; Arizona Dusty the roadrunner, Saguaro Sam; History Teddy Roosevelt, Ben Franklin,
long dead, respectful caricatures like Jose Cart 67's (no estates that license images: no Einstein, Elvis...); the
server room's Firewall Knight; the legend Paul Bunyan); a new fighter needs all of `BrawlMoveIds` and four named
specials. Tone: nobody is hurt (bonks, a twinkle), no blood. `BrawlSim.Step` (60 Hz, own xorshift `Rand`) is the only
place a match changes, from `BrInput`s (people and `BrawlBot` alike), so the same seed and buttons give the same match
(`Checksum`; kept that way for network play later). Knockback = Smash-style formula (`BrawlSim.Knockback`) x
`KbSpeed`; `-Test` keeps every fighter's strong side attack winning a life at 60-200% (now ~75-105%) and every
fighter recovering from twelve spots off the stage, so a new fighter, move or stage must keep those passing. Every
attack winds up 3+ frames; every stage surprise (`Hazard`: wind, cart, fans, meteor, laser) is warned `HazWarn` (90)
frames first. Hit groups (`BHit.Group`, set in `BrawlData`'s constructor) make overlapping boxes land once. Pictures
are puppets (`BrawlArt.cs`: `BPose` per state / move anim, `Build` rig, per-fighter `Dress*` / `HeadOf` / `Held`).
Keys: left A D W S + F G H R T (Space jumps), right arrows + K L ; O I (number pad 1 2 3 0 5); alone both sets plus
Z X C V; up to four XInput pads through `KartPads` (right stick = strong; `KartPad.RX/RY` added for it). Ladder scores
`ladder-<difficulty>` in the scorebook; `SaveForUpdate` keeps a match or the ladder (`brawl.*`). **Keys can be changed
(2026-10-29)**: Controls on the title (`BrawlKeys.cs`: one key per action per set, a taken key swaps, `Allowed` keeps
Esc / P / M / N / Enter / F-keys for the game, extras work only while unused), saved per player in `brawl-keys.txt`
next to the scores (version 1, unknown fields kept, fixture `Tests\saves\brawl-keys.txt`); the pause panel lists the
person's own keys and specials (`-Show Pause|Pause2|Keys`). **Feel (2026-10-29, "slow to react", Normal "pretty
tough")**: `BrawlSim.Buffer` keeps a press for `BufferFrames` (9) steps while busy; bots decide once per attack
whether to shield (`BrawlBot.ShieldChance`); `BotLevels` 1 / 3 / 6 / 9; the ladder climbs half as fast below Hard.
**Combos (2026-10-31, "smash through combos")**: once an attack has `Connected` and passed `BMove.LastActive`, a
buffered button cancels it into the next move (`BrawlSim.Chains`: ordinary, dash and lunge attacks; at most
`MaxChain` 4 cancels, reset when hit; event `combo` draws "N hits!"). **Bodies (2026-10-31, "they all look too much
alike")**: `BrawlBodies.cs` `BodyStyle` per fighter (proportions, tapered and swelling limbs `Seg` / `Limb`, armour
`Plates`, suit `Rings`, `BodyShape` barrel / pear / V, `Skull` with jaw / chin / nose / ear, `Hand` and `FootOf`
kinds, `Real` = life-like proportions, thinner outlines, `FaceEyes`); `-Show Lineup` draws all ten large. **Music in
between**: `Music.BrawlFill` 0 choosing (Select / Ladder screens, `MenuMusic`), 1 results loop after the fanfare
(`resultsMusicAt`), 2 "so close" when the computer beats the people (`ResultsJingle`), 3 hold music while paused
(`OnPauseChanged`, "like elevator music"); roles `select` / `results` / `defeat` / `hold` in `BrawlMusic.Roles` take a recorded song when one is added. Out of lives shows "Smashed!". **Signature attacks
(2026-10-31, "up, down, left, right ... each unique per character")**: `BrawlSignatures.cs` gives each fighter's 13
attacks (`AttackIds`) a name, an anim (new poses headbutt, shoulder, clap, slam, sweep, twirl) and an effect
(`SigFront`: petals, stars, chomp, muzzle, boom, drillfx, ore, feathers, needles, bully, lasso, pages, bits, fire,
chips) plus tweaks (rapid, spin, zap, spike, launch, trip, far, heavy, quick, wide); fstrong keeps its power. The
pause panel lists one person's attacks; `-Show Moves -Level 1-10` draws a fighter's sheet. Repeat hits (`Rehit`)
count the move's own frames (`HitAt` = MF), so a hit's pause can't freeze a flurry.
**Hitting movements (2026-10-31, Andrew: "make each character's hitting method unique ... rather their hitting movements")**:
`BrawlMotion.cs` `MotionStyle` per fighter reshapes every attack's wind-up and strike (`StyledPoses`: Amp, the free arm's
`Arms` guard / two / fencer / dancer / tucked / flung, Elbow, Cactus, Wide, Low, Lunge, Peck) and the timing (`Snap`,
`Over`) and adds life (`Alive`: Writhe, Whip, Weave, Scurry, Bounce, Wobble, Flourish). Pictures only: hits, frames and
balance are unchanged. `-Test` checks every two fighters' strikes differ and every attack frame is a sane pose; a new
fighter needs its own style. `-Show Swings -Level 1-13` draws all ten doing one attack (wind-up faint, strike in front).
**Stages (2026-11-01, Andrew: platforms "unique to each level, following the theme, and not just one big platform and 2
small platforms"; a javelina stampede, "2 adult javelin and 3 babies, making snorting sounds")**: each soft platform has a
`BPlat.Look` (mesa ledge / arch / saguaro, copper terrace / lift / bucket, server rack / tray, moon rock / crystal, lab
tank / catwalk / gantry; `Moving` gives MoveX / MoveY), drawn by `DrawUnder` / `DrawOver` in `BrawlScenery.cs` (pictures
only; looks never shared between stages). `BStageDef.Hazard` may list several surprises split by spaces, picked at
random each time (mesa: `wind javelina`). The herd (`BrawlSim.StepHerd`, `HerdBack` / `HerdAdult` / `HerdSpeed`) runs
along the ground, hits anyone standing in its way (grown-up harder, again only after 50 frames), misses anyone in the
air; sounds `brawl-snort` / `brawl-squeal` / `brawl-hooves`; `BrawlBot.HerdComing` jumps it. `-Test` (`BrawlStageChecks`)
checks three+ platforms of each stage's own kinds, the herd's makeup, both mesa surprises, and that standing gets bowled
while jumping clears it; `-Show Stampede -Level 1|2` draws it running / the warning.
Looks (2026-10-29): `BrawlScenery.cs` (textures `BrawlTex`, three cached background layers per stage drawn as
cropped parts, platforms in one-point perspective toward `VP`, `Sphere` / `Cylinder` / `Glow` brushes, grade and
vignette; `Lite` when RenderCapability tier < 2); movement in `BrawlArt.cs` (`AnimState` per fighter: blended poses,
stride from real speed, double-jump flip, squash / stretch, swoosh from `TipOf`; pictures only, never read by the sim).
**Recorded music (2026-10-29, Andrew's Suno songs; his plan is Pro, so they're his to use): the second exception to
"music made in code", for Sahuarita Smash! only.** `Music\Brawl\<role>.mp3` + `music.txt` (role file loopStart loopEnd
gain), brought in only by `tools\Import-BrawlMusic.ps1` (finds the beat near each prompt's tempo, loops whole bars,
picks the better of Suno's two versions, `-Preview` writes seam WAVs). `BrawlMusic.cs` decodes with Media Foundation
on a background thread (`MfDecoder`), keeps 3; `Song.FromPcm` / `SongPlayer.NextRecorded` play it (60 ms seam
crossfade); the built-in tune plays until it's ready or when it's missing (`PlayMusic` / `MusicCatchUp`). Tests and
snapshots set `BrawlMusic.Enabled = false` and never decode the files (`BrawlMusicTests.cs` uses made-up PCM). Never
add a song made on a free Suno plan, and never a song by anyone else.
Snapshots `-Game Brawl -Show Title|Select|Help|Ladder|Fight|Fight2|Cast|Countdown|Results|Ladder1|Giant -Level 1-5`.
Not yet tried: everything on a real screen with real keys and controllers, two people on one keyboard, fairness
between fighters, music and sounds by ear; not on the network yet.
