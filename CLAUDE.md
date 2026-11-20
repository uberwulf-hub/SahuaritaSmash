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
  main, then run the `Release` workflow with `vX.Y.Z` on main. **Signing is GitHub's:** `android.yml` signs the apk with, first found, the `ANDROID_KEYSTORE_*` secrets, or
  `keys/sahuaritasmash-release.keystore.enc` (the key the first build made, locked with the `BACKUP_PASSPHRASE` secret
  by `store-android-key.yml`, which also deleted the cached copy; it is the backup too), or, only while the repository
  is private, the Actions cache. The repository is public since 2026-11-14 (free Actions minutes): never cache, upload
  or commit an unlocked key, and never change the key (phones would have to uninstall). The Windows zip is unsigned. `build.yml` runs the
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
- Tests never touch real data, never open windows (Avalonia headless), never play audio, never use the network
  beyond this computer (127.0.0.1).
- Plain words for families. Touch first on Android: buttons at least 44 px, nothing hover-only.

## How it is built

- `src\SahuaritaSmash.Engine` (net10.0, no UI): copied from App Hub with the namespace `ClassicGames` kept so the
  files can be compared. The match: `Brawl\BrawlSim.cs`, `BrawlData.cs` (+ `BrawlInfo.GameName`, the name the music
  uses), `BrawlBot.cs`, `BrawlSignatures.cs` (its data half). Sound: `Audio\Audio.cs` (App Hub's mixer and synth
  without Hive's industrial instruments and other games' sounds; the device is an `IAudioOut` the platform gives
  `Audio.Start` through `App.MakeAudioOut`: `Desktop\WaveOut.cs`, Android's `AndroidSound.cs` `TrackOut`), `Music.cs`
  (only this game's tunes and `GameTunes`), `BrawlSfx.cs`, `BrawlMusic.cs` (recorded songs from an `IMusicSource`, all decoded at start by `PreloadAll`, menu first,
  and all kept, loops trimmed at their end: App Hub kept 3, which made screens start with the chiptune; 
  Windows `FolderMusic` = `Folder` beside the exe + Media Foundation, Android `AssetMusic` = assets + MediaCodec). The
  songs are `Music\Brawl` at the repository's root (Andrew's Suno Pro songs + `music.txt`; credited in saucestore.json). Saves: `ScoreBook.cs` (+ `AddKept`),
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
  `brawl-keys.txt`, read only). `App.Testing` skips the start-up work (no sound); `App.EnsureSound` starts the sound once `App.MakeAudioOut` is known (Android's
  `Application.cs` sets it before the app starts: in 0.1.0 the activity set it too late and Android was silent; the
  activity routes the volume buttons to media, and since 0.3.1 turns them into the music volume itself in `DispatchKeyEvent`, because Avalonia's view took them as keys; `GameView.SystemKey` leaves volume / media keys alone too); `App.QuitApp` is Android's way out; the Windows window is full screen without a title bar (F11 / Alt+Enter: a
  window), Android hides the system bars (`MainActivity.FullScreen`); `App.GoneToBackground`
  (Android's OnPause) pauses the game. `Views\TouchControls.cs`: the on-screen stick (up jumps) and five buttons (Attack, Shield, Special,
  Strong, Grab; no Jump since 0.1.1, Andrew; Shield on the right because Shield + stick rolls and dodges), controller 1 to
  the game (`KartPads.Fake[0]`, set by `GameView.SyncTouch` each frame) only while `GameView.TouchActive` (play, not
  paused, nothing clickable drawn), on Android from the start and on Windows once the screen is touched; sizes in
  screen units (every button 44+ across: `TouchChecks`).
- **Controllers on Android (2026-11-17)**: `Wpf\PadInput.cs` `KartPadInput` (`KartPads.Input`) turns controller
  events into `KartPad`s with XInput's meaning (left stick X / Y up = +, dead zone 0.25 like XInput's 8000; right stick
  = Android's Z / RZ; triggers LTRIGGER / BRAKE and RTRIGGER / GAS, or the L2 / R2 keys; d-pad keys or the HAT axes;
  A B X Y, L1 R1, Start Select). It takes Android's own key codes and axis numbers as plain ints, so the checks drive it
  with pretend events. A controller gets the lowest free number 0-3 the first time it's used (a button, or a stick /
  trigger past 0.5), keeps it until it goes away (`Gone`, or `Exists` = `InputDevice.GetDevice(id) != null` asked
  about once a second from `Read`), and "Controller N connected / disconnected" is shown through `GameView.SyncTouch`
  -> `Game.Toast`. `MainActivity` passes only events whose source is a gamepad or joystick (`DispatchKeyEvent` for the
  controller key codes, after the volume buttons; `DispatchGenericMotionEvent` for every axis in `KartPadInput.Axes`)
  and returns true, so the phone's own keys and Back are untouched. `KartPads.Read(i)`: touch (`Fake[0]`) and a
  controller in the same slot are merged (`KartPadInput.Merge`: either one's buttons, the stick pushed further), so
  touch keeps working beside controller 1; with no controller it's touch exactly as before; Windows never feeds
  `Input`, so XInput is unchanged. Checks: group `pads` (`PadChecks.cs`).
- **Playing on a phone (2026-11-17, Andrew plays on an Android phone):** `Brawl\BrawlTouchGuide.cs` (partial BrawlGame) and `Services\TouchSettings.cs`. While `Game.TouchMode`, the pause panel's right side is `DrawTouchPauseGuide` (the touch layout drawn small by `DrawTouchLayout`, this person's specials and moves the touch way, text 24+ game units) instead of the key table; the title's Controls item is "Touch controls" (`OpenControls`, `Screen.Touch`): button size Small / Normal / Large (`TouchControls.Scale` 0.85 / 1 / 1.25, shrunk to fit a narrow screen, never under 44 across), Left-handed (`TouchControls.LeftHanded`: buttons mirrored, the stick takes the right half), Vibration, the guide again, Keyboard keys (not on Android). Saved per device in `touch.txt` (version 1, key=value lines, unknown lines kept, never written over when unreadable; fixture `Fixtures\touch.txt`); `GameView.SyncTouch` hands size and hand to the controls. The first-time guide (`GuideCheck` at the start of a fight by touch, four cards over the paused fight, `PausedKey`, `guide=done`) never shows in the checks unless `BrawlGame.GuideInTests`. Haptics: `App.Haptic(ms)` (Android: `MainActivity.Buzz`, VIBRATE permission; null elsewhere), called by `BrawlGame.Feel` after each step from the match's `hit` / `ko` events (victim `S`, attacker = the victim's `Last`) for the touch person's fighter only (`TouchSlot`), at most one every 120 ms; never from inside BrawlSim. Checks: group `touch-phone` (every size x hand x screen, the file, the page by taps, the guide, the buzz); pictures TouchPause-phone, TouchPage-phone, Guide-1..4-phone, TouchLefty-phone, TouchLarge-phone.
- **Play online (2026-11-15, Andrew: "like Race Track Mayhem")**: lockstep. `Engine\Net\NetMatch.cs`: devices send only
  their people's buttons (`NetProtocol.Pack`: stick in 255 steps, used on the sending device too), a frame is stepped
  only when everyone's are in (input delay 4 frames at home, 7 online), checksums every 120 frames to the host (a
  mismatch ends the match with a message), a leaver's fighter goes to the computer from a frame all can reach. This
  needs the match to compute the same on every device: **only `+ - * /`, `Math.Sqrt`, `Round`, `Floor` and `DMath.Sin`
  / `Cos` in `BrawlSim` and `BrawlBot`, never `Math.Sin` / `Cos` / `Pow` / `Exp` / `Atan2` (they may differ between
  Windows and Android), nothing from the clock or `System.Random`** (`NetChecks` replays and compares).
  `NetSession.cs` is the lobby (Hello with `NetProtocol.Game` and `Version`: bump it when messages or match rules change;
  Refuse in plain words; Lobby, Pick, Start, End). Transports (`INetTransport`): `LanTransport` (TCP 47871-47879, the
  host passes messages on; "who's hosting?" on UDP 47870, private addresses only; Android holds a multicast lock while
  hosting, `HoldBroadcasts`) and `RelayTransport` (Race Track Mayhem's relay on Cloudflare, `DefaultRelay`; its rooms and
  4-letter codes; it calls games races, so its messages are reworded). `Brawl\BrawlOnline.cs`: the screens (Play
  online, Join with a letter pad, Lobby) and the hooks into `Brawl.cs` (marked "online"); `Wpf\NetLink.cs` makes the
  game base keep going behind pause and leave to Play online. Checks: `net` (pretend network), `net-lobby` (and real TCP
  on 127.0.0.1), `net-relay` (only with `SMASH_RELAY_URL`, e.g. Race Track Mayhem's `server/relay/local.mjs`), `online`
  (two whole games through the menus). Off-screen pictures (`DrawingVisual`) must be disposed: piles of them crashed
  Skia in the checks; old background layers are disposed when a stage changes.
- **New fighters and stages (since 0.3.0, Andrew: "keep going"):** Gila Gus (`gila`, Arizona) and the Pecan Orchard
  (`orchard`, surprise `pecans`: the tree shaker warns `HazWarn` frames with every nut's spot (`BrawlSim.Nuts`) already
  shadowed, then one lands every `NutGap` frames, `NutDrop` after it falls; small bonks). **Add new fighters and stages
  at the end of their lists:** saved matches (`UpdateResume`) and the online lobby keep them by number; and bump
  `NetProtocol.Version` whenever the lists change. A new fighter needs its signature table, `BodyStyle`, `MotionStyle`,
  `Dress`/`HeadOf` art; a new stage its `BrawlScenery` cases (layers, sky, light, textures, platform looks under / over),
  a built-in `Music.BrawlSong` tune and a `BrawlMusic.Roles` entry (a recorded Suno song can be added later), and the
  menus fit up to 15 fighters (three rows of five). Checks and snapshots (`Fight-6`, `Moves-11`, `Pecans-1|2`) cover them.
- **Practice, colours and the online rematch (2026-11-17):** `Brawl\BrawlPractice.cs` is the title's Practice (setup
  screen `Screen.Practice`, rows 78 game units tall so they tap on a phone; a match of you and a dummy with
  `BrawlSim.Practice`: no life is lost, a fall comes back after 30 frames, the match never ends; `BrawlSim.Practice` is
  false everywhere else and leaves normal matches exactly as before: `PracticeChecks` compares three matches' checksums
  with numbers taken from the code before it). The dummy's buttons (`DummyInput`: stands / walks / jumps / shields /
  `BrawlBot` level 1) come from the match only. Its pause menu (`PausedKey`, `DrawPracticePause`) changes the dummy, puts
  damage back to 0%, turns surprises on or off and shows the moves list (`DrawMovesList`, `BrawlSignatures.HowTo`); a
  short list is drawn at the top right while playing. Practice never saves anything (no scores, not kept for an update).
  Colours: `Engine\Brawl\BrawlUnlocks.cs` is `brawl-unlocks.txt` next to the scores (version line, then player, fighter
  id, extra colours 1 or 2; unknown lines and fields kept; through `SafeFile`; an unreadable file is never written;
  fixture `Fixtures\brawl-unlocks.txt`). Climbing the ladder unlocks colour 2, on Hard colour 3 too (`UnlockForLadder`,
  shown over the ladder's end by `DrawOver`). `Brawl\BrawlColours.cs` has each fighter's two extra palettes by id (Main,
  Second, Trim, Dark, and Skin only for fighters whose skin is a colour of their own; people's skin is never changed) and
  turns an unlisted fighter's colours round the colour wheel, so **a new fighter should get a table entry**; the coloured
  copy of a `BFighterDef` is swapped into `f.D` only while drawing (`BrawlView.DrawFighterIn`, `View.Colours` by slot),
  so `BrawlSim` never sees it. `Brawl\BrawlColourPick.cs`: the chips (80 units square) under the fighter on the fight,
  ladder and practice screens, C on the keyboard, `MatchColours` (the same fighter twice gets different unlocked
  colours). Online uses the usual colours. Rematch: `Brawl\BrawlRematch.cs` (the host's Rematch / Back to the lobby,
  the guest's Leave, 80 units tall) and `NetSession.Rematch`: a new Start with the same seats (people who left are left
  out), a new seed and the next `NetMatchSetup.Round`; Inputs, Check and Drop carry the round first and `NetMatch` drops
  another round's (`Stale`); `NetProtocol.Version` 3. Checks: groups `practice`, `colours`, `rematch` (`ModesChecks.cs`;
  scenes `Practice`, `PracticeFight`, `PracticePause`, `SelectColours`, `LadderColours`, `LadderUnlock`, `Colours -Level
  1-3`, wide and phone-sized).
- **Pecos Bill and Monsoon Wash (NetProtocol.Version 3):** Pecos Bill (`pecos`, Legends; the public-domain folk tale
  only, never a film's version: a lanky cowboy, wide hat, bandana, vest, chaps, cowboy boots `Foot = "cowboy"`, and
  arms style `rodeo`, the free hand up and waving). Specials: Lasso (`lash`, extra `rope`), Lasso pull (cmdgrab, `rope`),
  Ride the twister (rise with a multi-hit, `twister`), Coyote howl (anim `howl`, a push on both sides, `howl`); his
  pictures are in `Brawl\BrawlPecos.cs` (`PecosTorso`, `PecosHead`, `PecosExtra`, `PecosSig` for `crack` / `gust`).
  Monsoon Wash (`wash`, platforms `log`, `bridge`, `culvert`; textures `washsand`, `concrete`, `cottonwood`; its sky,
  layers, lightning and platforms in `Brawl\BrawlWash.cs`) and its surprise `flood`: warned `HazWarn` frames (thunder,
  the sign, a trickle), then `BrawlSim.StepFlood`: the front (`HazX`) runs `FloodSpeed` along the ground, gives anyone
  standing in it one light bonk per wave, and the water `FloodLen` behind it carries anyone standing in it
  `FloodCarry` a frame but never past 30 px from the edge; the air, platforms and the ledge are safe;
  `BrawlBot.FloodComing` jumps it. The water's surface is drawn after the platforms and behind the fighters, its front
  face in front of them. Checks: group `brawl-cast` (`BrawlCastChecks.cs`: his four specials, the flood warned, bonking,
  carrying, missing the bridge and a jumper, bots jumping it); snapshots `Fight-7`, `Moves-12`, `Flood-1|2`.
- `src\SahuaritaSmash.Desktop` (Windows, `SahuaritaSmashSE.exe`), `src\SahuaritaSmash.Android`
  (`com.uberwulf.sahuaritasmashse`, landscape).
- `tests\SahuaritaSmash.Tests`: `dotnet run --project tests/SahuaritaSmash.Tests -c Release [-Only brawl,brawl-saves,
  brawl-snapshots,sound,touch,touch-phone,pads,net,net-lobby,net-relay,online] [-Snapshot dir]`: App Hub's Brawl test files copied whole (`BrawlTests.cs`, `BrawlKeysTests.cs`,
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
**Gentler Easy and Normal (0.4.1, 2026-11-20, Andrew: "I still had a hard time with easy"; measured: Easy dealt about as much
damage a minute as Champion)**: `BrawlBot.Aggression` (openings taken: Easy 12, Normal 34, then 30 + 7 x level as before),
`Breather` (frames rested after an attack: 40 / 16 / 6 at level 4), `Pace` (Easy walks 0.45, Normal 0.75), `Finisher`
(strong finishing hits 10 / 25 / 55), `Daydream` (Easy and Normal sometimes stand still), and no random strong hits at
level 3 and below. Levels 5 and up play exactly as before (`PracticeChecks` replays levels 5-9 against checksums taken
before the change). The ladder: Easy stays level 1 all the way, Normal 2-3, Hard 5-9. Check: "Easy is gentle" in
`BrawlKeysTests` (damage a minute against the same level-3 opponent: Easy well under half of Champion's, each level
above the last, Easy wins at most 2 of 12).
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
crossfade); the game waits for a recorded song still decoding (`BrawlMusic.Coming`, `WaitingForSong`) and plays the built-in tune only after `SongWaitSeconds` (8) or when it's missing or unreadable (`PlayMusic` / `MusicCatchUp`; since 0.3.1: before, the built-in tune played first and the song swapped in). Tests and
snapshots set `BrawlMusic.Enabled = false` and never decode the files (`BrawlMusicTests.cs` uses made-up PCM). Never
add a song made on a free Suno plan, and never a song by anyone else.
Snapshots `-Game Brawl -Show Title|Select|Help|Ladder|Fight|Fight2|Cast|Countdown|Results|Ladder1|Giant -Level 1-5`.
Not yet tried: everything on a real screen with real keys and controllers, two people on one keyboard, fairness
between fighters, music and sounds by ear; not on the network yet.
