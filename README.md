# Sahuarita Smash! (Sauce Edition)

A family platform fighter from Sahuarita, Arizona, for Windows and Android. Up to four players (people or the
computer) knock each other off the stage; nobody gets hurt: fighters get bonked and twinkle away.

- **Ten fighters:** Bloom, Specimen, Trooper and Juniper from Classic Games; Arizona Dusty the roadrunner and Saguaro
  Sam; Teddy Roosevelt and Ben Franklin; the Firewall Knight; Paul Bunyan. Each has 13 attacks of its own and four
  specials.
- **Five stages** with surprises that always warn first: the mesa (monsoon wind, a javelina stampede), the copper mine
  (mine carts), the server room (fans), the moon (meteors) and the lab (a sweeping laser).
- **The ladder:** six matches against the computer, harder each round; the last one is against a giant.

It started as a game in App Hub's Classic Games and is moving here step by step (see [docs/PLAN.md](docs/PLAN.md)).
Right now the app shows four computer players fighting, stage after stage, with the game's own pictures.

## Building

- Checks: `dotnet run --project tests/SahuaritaSmash.Tests -c Release`
- Windows: `dotnet run --project src/SahuaritaSmash.Desktop`
- Android: built by GitHub Actions (`build.yml` makes a test copy on every push; Actions > Android makes a signed one).

Releases are made only by the Release workflow on GitHub (see CLAUDE.md), which signs the Android app with the
repository's own key and publishes both files for SauceStore.

Not affiliated with Nintendo or any other game's makers.
