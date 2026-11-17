# Sahuarita Smash! (Sauce Edition)

A family platform fighter from Sahuarita, Arizona, for Windows and Android. Up to four players (people or the
computer) knock each other off the stage; nobody gets hurt: fighters get bonked and twinkle away.

- **Eleven fighters:** Bloom, Specimen, Trooper and Juniper from Classic Games; from Arizona Dusty the roadrunner,
  Saguaro Sam and Gila Gus the Gila monster; Teddy Roosevelt and Ben Franklin; the Firewall Knight; Paul Bunyan. Each
  has 13 attacks of its own and four specials.
- **Six stages** with surprises that always warn first: the mesa (monsoon wind, a javelina stampede), the copper mine
  (mine carts), the server room (fans), the moon (meteors), the lab (a sweeping laser) and Sahuarita's pecan orchard
  (the tree shaker: pecans fall where their shadows show).
- **The ladder:** six matches against the computer, harder each round; the last one is against a giant. Climbing it
  unlocks a second colour for your fighter (on Hard, a third).
- **Practice:** try your moves on a dummy that stands still, walks, jumps, shields or fights back; nobody loses.
- **Play online:** on your home network, or over the internet with a 4-letter code (like Race Track Mayhem). After a
  match the host can start a rematch with the same fighters or go back to the lobby.

It started as a game in App Hub's Classic Games and is moving here step by step (see [docs/PLAN.md](docs/PLAN.md)).
Right now the whole game is here, with its music and sounds, and plays on Windows with the keyboard, mouse and
controllers, and on Android with on-screen touch controls.

## Building

- Checks: `dotnet run --project tests/SahuaritaSmash.Tests -c Release`
- Windows: `dotnet run --project src/SahuaritaSmash.Desktop`
- Android: built by GitHub Actions (`build.yml` makes a test copy on every push; Actions > Android makes a signed one).

Releases are made only by the Release workflow on GitHub (see CLAUDE.md), which signs the Android app with the
repository's own key and publishes both files for SauceStore.

Not affiliated with Nintendo or any other game's makers.
