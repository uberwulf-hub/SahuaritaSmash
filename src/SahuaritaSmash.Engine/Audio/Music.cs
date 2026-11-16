// Sahuarita Smash's music (from App Hub's Classic Games, Engine\Music.cs: the game's own parts): original chiptune
// pieces, one per stage, the quiet tunes in between and the menu theme and stings. Notation: see Song in Audio.cs.
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicGames
{
    public static class Music
    {
        static Song S(string name, double bpm, bool loop, string lead, string harmony, string bass, string drums, double duty1, double duty2)
        {
            var s = new Song(name, bpm, loop, lead, harmony, bass, drums);
            s.Duty1 = duty1; s.Duty2 = duty2;
            return s;
        }

        /// <summary>Every song, for the tests.</summary>
        public static IList<Song> All()
        {
            var all = new List<Song>();
            GameTunes t = For("Brawl");
            foreach (Song s in new[] { t.Theme, t.GameOver, t.HighScore, t.LevelClear, t.LifeLost, t.Extra, t.Extra2 }) if (s != null) all.Add(s);
            for (int i = 0; i < 6; i++) all.Add(BrawlSong(i));
            for (int i = 0; i < 4; i++) all.Add(BrawlFill(i));
            return all;
        }

        // ------------------------------------------------------------------ Sahuarita Smash: a tune for every stage
        // All original, eight bars each: a twangy A minor gallop for the mesa, E minor rock for the copper pit, C minor
        // synth arpeggios for the server room, a slow Lydian float for the moon, a tense D minor pulse for the lab, and a
        // bright G major picking tune for the pecan orchard.
        static readonly Song[] brawlSongs = new Song[6];
        public static Song BrawlSong(int stage)
        {
            stage = Math.Max(0, Math.Min(5, stage));
            lock (brawlSongs)
            {
                if (brawlSongs[stage] != null) return brawlSongs[stage];
                Song s;
                switch (stage)
                {
                    case 0:
                        s = S("Sahuarita Smash: Sonoran Mesa", 128, true,
                            "A4:4 E5:4 D5:2 C5:2 B4:2 C5:2 | A4:6 .:2 G4:2 A4:2 C5:4 | D5:4 F5:4 E5:2 D5:2 C5:2 D5:2 | E5:12 .:4 |" +
                            "A5:4 G5:2 E5:2 G5:4 E5:2 D5:2 | C5:4 D5:2 E5:2 A4:8 | F5:4 E5:2 D5:2 C5:2 B4:2 G#4:4 | A4:12 .:4",
                            "[.:2 C5:2]x4 [.:2 C5:2]x4 [.:2 F4:2]x4 [.:2 G#4:2]x4 [.:2 C5:2]x4 [.:2 E4:2]x4 [.:2 F4:2]x2 [.:2 G#4:2]x2 [.:2 C5:2]x4",
                            "[A1:4 E2:4]x2 [A1:4 E2:4]x2 [D2:4 A2:4]x2 [E2:4 B1:4]x2 [A1:4 E2:4]x2 [A1:4 E2:4]x2 D2:4 A2:4 E2:4 B1:4 [A1:4 E2:4]x2",
                            "k:4 h:2 h:2 s:4 h:2 k:2", 0.25, 0.5);
                        break;
                    case 1:
                        s = S("Sahuarita Smash: Copper Pit", 160, true,
                            "E5:2 E5:2 G5:2 E5:2 A5:2 G5:2 E5:2 D5:2 | E5:4 B4:4 D5:4 B4:4 | E5:2 E5:2 G5:2 E5:2 B5:2 A5:2 G5:2 A5:2 | B5:8 A5:4 F#5:4 |" +
                            "G5:2 G5:2 B5:2 G5:2 C6:2 B5:2 A5:2 G5:2 | F#5:4 D5:4 A5:4 F#5:4 | E5:2 G5:2 B5:2 E6:2 D6:2 B5:2 A5:2 F#5:2 | E5:12 .:4",
                            "[B4:1 .:1]x8 [B4:1 .:1]x8 [B4:1 .:1]x8 [D5:1 .:1]x8 [E5:1 .:1]x8 [D5:1 .:1]x8 [B4:1 .:1]x8 [B4:1 .:1]x8",
                            "[E2:1 E3:1]x8 [E2:1 E3:1]x8 [E2:1 E3:1]x8 [B1:1 B2:1]x8 [C2:1 C3:1]x8 [D2:1 D3:1]x8 [E2:1 E3:1]x8 [E2:1 E3:1]x8",
                            "k:2 h:2 s:2 h:2 k:1 k:1 h:2 s:2 h:2", 0.5, 0.25);
                        s.Gain = 0.85;
                        break;
                    case 2:
                        s = S("Sahuarita Smash: Server Room", 140, true,
                            "C5:2 Eb5:2 G5:2 C6:2 Bb5:4 G5:4 | Ab5:4 G5:2 F5:2 Eb5:4 D5:4 | C5:2 Eb5:2 G5:2 C6:2 D6:4 Eb6:4 | D6:8 B5:8 |" +
                            "Eb6:4 D6:2 C6:2 Bb5:4 G5:4 | Ab5:4 Bb5:2 C6:2 G5:8 | F5:2 G5:2 Ab5:2 Bb5:2 C6:2 D6:2 Eb6:2 F6:2 | G6:8 .:8",
                            "[C4:1 Eb4:1 G4:1 C5:1]x4 [Ab3:1 C4:1 Eb4:1 Ab4:1]x4 [C4:1 Eb4:1 G4:1 C5:1]x4 [G3:1 B3:1 D4:1 G4:1]x4 [Eb4:1 G4:1 Bb4:1 Eb5:1]x4 [Ab3:1 C4:1 Eb4:1 Ab4:1]x4 [F3:1 Ab3:1 C4:1 F4:1]x4 [G3:1 B3:1 D4:1 G4:1]x4",
                            "[C2:2 .:2]x4 [Ab1:2 .:2]x4 [C2:2 .:2]x4 [G1:2 .:2]x4 [Eb2:2 .:2]x4 [Ab1:2 .:2]x4 [F1:2 .:2]x4 [G1:2 .:2]x4",
                            "k:2 h:1 h:1 s:2 h:2 k:2 h:2 s:2 h:1 h:1", 0.125, 0.5);
                        s.Gain = 0.85;
                        break;
                    case 3:
                        s = S("Sahuarita Smash: Pebblemoon", 104, true,
                            "F5:6 G5:2 A5:4 C6:4 | B5:8 A5:4 G5:4 | A5:6 G5:2 F5:4 E5:4 | G5:12 .:4 |" +
                            "C6:6 B5:2 A5:4 G5:4 | A5:6 B5:2 C6:4 E6:4 | D6:4 C6:4 B5:4 G5:4 | F5:12 .:4",
                            "[F4:2 A4:2 C5:2 A4:2]x2 [G4:2 B4:2 D5:2 B4:2]x2 [F4:2 A4:2 C5:2 A4:2]x2 [E4:2 G4:2 C5:2 G4:2]x2 [F4:2 A4:2 C5:2 A4:2]x2 [G4:2 B4:2 D5:2 B4:2]x2 [G4:2 B4:2 D5:2 B4:2]x2 [F4:2 A4:2 C5:2 A4:2]x2",
                            "F2:16 G2:16 F2:16 C2:16 F2:16 G2:16 G2:16 F2:16",
                            "k:4 .:4 s:4 .:2 h:2", 0.5, 0.5);
                        s.Gain = 0.8;
                        break;
                    case 4:
                        s = S("Sahuarita Smash: The Lab", 150, true,
                            "D5:2 .:2 D5:2 F5:2 E5:2 .:2 C#5:4 | D5:2 .:2 A4:2 Bb4:2 A4:8 | D5:2 .:2 D5:2 F5:2 G5:2 .:2 A5:4 | Bb5:4 A5:4 G5:4 E5:4 |" +
                            "F5:2 .:2 F5:2 A5:2 G5:2 .:2 E5:4 | F5:2 .:2 D5:2 E5:2 C#5:8 | D5:2 F5:2 A5:2 D6:2 C#6:2 A5:2 E5:2 G5:2 | D5:12 .:4",
                            "[D4:2 .:2]x4 [D4:2 .:2]x4 [F4:2 .:2]x4 [G4:2 .:2]x4 [F4:2 .:2]x4 [E4:2 .:2]x4 [F4:2 .:2]x2 [E4:2 .:2]x2 [D4:2 .:2]x4",
                            "[D2:1 D2:1 D3:2]x4 [D2:1 D2:1 D3:2]x4 [Bb1:1 Bb1:1 Bb2:2]x4 [G1:1 G1:1 G2:2]x4 [F1:1 F1:1 F2:2]x4 [A1:1 A1:1 A2:2]x4 [Bb1:1 Bb1:1 Bb2:2]x2 [A1:1 A1:1 A2:2]x2 [D2:1 D2:1 D3:2]x4",
                            "k:2 h:2 s:2 h:2 k:2 h:1 k:1 s:2 h:2", 0.25, 0.125);
                        s.Gain = 0.85;
                        break;
                    default:
                        s = S("Sahuarita Smash: Pecan Orchard", 132, true,
                            "G4:2 B4:2 D5:2 G5:2 E5:4 D5:4 | C5:2 E5:2 G5:2 E5:2 D5:8 | B4:2 D5:2 G5:2 B5:2 A5:4 G5:4 | E5:4 F#5:4 G5:8 |" +
                            "C6:4 B5:2 A5:2 G5:4 E5:4 | D5:4 E5:2 G5:2 A5:8 | B5:2 A5:2 G5:2 E5:2 D5:2 E5:2 F#5:4 | G5:12 .:4",
                            "[B4:2 D5:2]x4 [C5:2 E5:2]x4 [B4:2 D5:2]x4 [A4:2 F#4:2]x4 [C5:2 E5:2]x4 [A4:2 D5:2]x4 [B4:2 D5:2]x2 [A4:2 C5:2]x2 [B4:2 D5:2]x4",
                            "[G2:4 D2:4]x2 [C2:4 G2:4]x2 [G2:4 D2:4]x2 [D2:4 A2:4]x2 [C2:4 G2:4]x2 [D2:4 A2:4]x2 G2:4 D2:4 D2:4 A2:4 [G2:4 D2:4]x2",
                            "k:4 h:2 s:2 h:4 s:4", 0.125, 0.25);
                        s.Gain = 0.85;
                        break;
                }
                brawlSongs[stage] = s;
                return s;
            }
        }

        // ------------------------------------------------------------------ Sahuarita Smash: quiet tunes in between
        // (Andrew, 2026-10-31: "in between the menu, level, victory or defeat soundtracks ... some subtle similar sound
        // tracks to fill the gap for now"). In the menu's D major and feel, softer: 0 = choosing fighters and the ladder
        // (an airy arpeggio groove), 1 = the results after the fanfare (a slow, warm loop), 3 = hold music while paused (Andrew: "like elevator
        // music": a soft bossa in F with seventh chords), 2 = when the computer wins (a
        // short, kind "aww" that still ends in D major). Each can be replaced by a recorded song (BrawlMusic roles select,
        // results, defeat, hold).
        static readonly Song[] brawlFill = new Song[4];
        public static Song BrawlFill(int which)
        {
            which = Math.Max(0, Math.Min(3, which));
            lock (brawlFill)
            {
                if (brawlFill[which] != null) return brawlFill[which];
                Song s;
                switch (which)
                {
                    case 0:
                        s = S("Sahuarita Smash: choosing", 150, true,
                            "F#5:6 E5:2 D5:8 | .:16 | D5:4 E5:4 F#5:4 A5:4 | E5:12 .:4 | F#5:6 A5:2 B5:8 | .:16 | G5:4 F#5:4 E5:4 C#5:4 | D5:12 .:4",
                            "[D4:2 F#4:2 A4:2 F#4:2]x2 [B3:2 D4:2 F#4:2 D4:2]x2 [G3:2 B3:2 D4:2 B3:2]x2 [A3:2 C#4:2 E4:2 C#4:2]x2 [D4:2 F#4:2 A4:2 F#4:2]x2 [B3:2 D4:2 F#4:2 D4:2]x2 [G3:2 B3:2 D4:2 B3:2]x2 [A3:2 C#4:2 E4:2 C#4:2]x2",
                            "D2:8 A2:8 B1:8 F#2:8 G1:8 D2:8 A1:8 E2:8 D2:8 A2:8 B1:8 F#2:8 G1:8 D2:8 A1:8 E2:8",
                            "k:4 h:4 s:4 h:4", 0.5, 0.5);
                        s.Gain = 0.6;
                        break;
                    case 1:
                        s = S("Sahuarita Smash: results", 96, true,
                            "A5:8 F#5:8 | G5:8 B5:8 | G5:6 F#5:2 E5:8 | C#5:8 E5:8 | A5:8 F#5:4 A5:4 | B5:8 D6:8 | B5:4 A5:4 G5:4 E5:4 | D5:16",
                            "[D4:4 F#4:4]x2 [G3:4 B3:4]x2 [E4:4 G4:4]x2 [C#4:4 E4:4]x2 [D4:4 F#4:4]x2 [G3:4 B3:4]x2 [E4:4 G4:4]x2 [C#4:4 E4:4]x2",
                            "D2:16 G1:16 E2:16 A1:16 D2:16 G1:16 E2:16 A1:16",
                            "k:8 h:4 h:4", 0.5, 0.5);
                        s.Gain = 0.55;
                        break;
                    case 3:
                        s = S("Sahuarita Smash: on hold", 100, true,
                            "A5:6 G5:2 F5:8 | D5:4 F5:4 A5:8 | Bb5:6 A5:2 G5:8 | E5:4 G5:4 C6:8 | A5:4 C6:4 A5:4 F5:4 | D5:8 F5:8 | G5:4 Bb5:4 A5:4 G5:4 | F5:16",
                            "[F4:2 A4:2 C5:2 E5:2]x2 [D4:2 F4:2 A4:2 C5:2]x2 [G4:2 Bb4:2 D5:2 F5:2]x2 [C4:2 E4:2 G4:2 Bb4:2]x2 [F4:2 A4:2 C5:2 E5:2]x2 [D4:2 F4:2 A4:2 C5:2]x2 [G4:2 Bb4:2 D5:2 F5:2]x2 [C4:2 E4:2 G4:2 Bb4:2]x2",
                            "F2:6 C3:2 C3:8 D2:6 A2:2 A2:8 G2:6 D3:2 D3:8 C2:6 G2:2 G2:8 F2:6 C3:2 C3:8 D2:6 A2:2 A2:8 G2:6 D3:2 D3:8 C2:6 G2:2 G2:8",
                            "k:6 h:2 s:4 h:4", 0.5, 0.5);
                        s.Gain = 0.5;
                        break;
                    default:
                        s = S("Sahuarita Smash: so close", 110, false,
                            "F#5:2 E5:2 D5:2 B4:6 A4:4 B4:4 D5:12",
                            "B3:12 G3:8 F#4:12",
                            "B1:12 G1:8 D2:12",
                            "k:12 k:8 o:12", 0.5, 0.5);
                        s.Gain = 0.75;
                        break;
                }
                brawlFill[which] = s;
                return s;
            }
        }

        public static GameTunes For(string game)
        {
            var t = new GameTunes();
            switch (game)
            {
                case "Brawl":
                    // Sahuarita Smash: an upbeat D major menu theme and its own short stings, all original
                    t.Theme = S(BrawlInfo.GameName + " theme", 150, true,
                        "D5:2 F#5:2 A5:2 D6:4 C#6:2 B5:2 A5:2 | G5:4 B5:2 A5:2 F#5:4 E5:4 | D5:2 F#5:2 A5:2 B5:4 A5:2 F#5:2 A5:2 | E5:8 .:4 E5:2 F#5:2 |" +
                        "G5:2 B5:2 D6:2 G6:4 F#6:2 E6:2 D6:2 | C#6:4 A5:2 B5:2 C#6:4 E6:4 | D6:2 C#6:2 B5:2 A5:2 G5:2 F#5:2 E5:2 C#5:2 | D5:8 A4:4 D5:4",
                        "[.:2 F#4:2]x4 [.:2 B4:2]x4 [.:2 F#4:2]x4 [.:2 E4:2]x4 [.:2 B4:2]x4 [.:2 C#5:2]x4 [.:2 G4:2]x2 [.:2 A4:2]x2 [.:2 F#4:2]x4",
                        "[D2:2 D3:2]x4 [G1:2 G2:2]x4 [D2:2 D3:2]x4 [A1:2 A2:2]x4 [G1:2 G2:2]x4 [A1:2 A2:2]x4 [G1:2 G2:2]x2 [A1:2 A2:2]x2 [D2:2 D3:2]x4",
                        "k:2 h:2 s:2 h:2 k:2 k:2 s:2 h:2", 0.25, 0.5);
                    t.Theme.Gain = 0.85;
                    t.LevelClear = S(BrawlInfo.GameName + " game", 150, false, "D5:2 F#5:2 A5:2 D6:6", "A4:12", "D3:12", "k:2 s:2 k:2 o:6", 0.25, 0.5);
                    t.HighScore = S(BrawlInfo.GameName + " winner", 140, false, "D5:2 D5:1 D5:1 F#5:2 A5:2 G5:2 B5:2 D6:8", "F#4:8 G4:4 A4:8", "D2:8 G2:4 D2:8", "k:2 s:2 k:2 s:2 k:2 s:2 o:8", 0.25, 0.5);
                    t.GameOver = S(BrawlInfo.GameName + " off the ladder", 110, false, "A4:4 G4:4 F4:4 E4:12", "C4:12 B3:12", "F2:12 E2:12", "k:12 k:12", 0.25, 0.5);
                    t.Extra2 = S(BrawlInfo.GameName + " ladder climbed", 140, false, "D5:2 F#5:2 A5:2 D6:4 C#6:2 D6:2 E6:2 F#6:8", "A4:8 B4:8 A4:8", "D3:8 G2:8 D3:8", "k:4 s:4 k:4 s:4 o:8", 0.25, 0.5);
                    foreach (Song x in new[] { t.LevelClear, t.HighScore, t.GameOver, t.Extra2 }) x.Gain = 0.85;
                    break;
            }
            return t;
        }
    }

    public class GameTunes
    {
        public Song Theme, GameOver, HighScore, LevelClear, LifeLost, Extra, Extra2;
    }
}
