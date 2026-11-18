// Sahuarita Smash! (Sauce Edition): snapshot scenes for practice and the colours (-Show Practice | PracticeFight |
// PracticePause | SelectColours | LadderColours | Colours -Level 1-3 | LadderUnlock), drawn by the checks. They use
// unlocked colours kept in memory only, never a real file.
using System;
using Avalonia.Input;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        bool unlocksInMemory;
        /// <summary>The checks and snapshots: unlocked colours in memory only (nothing is saved).</summary>
        public void UseUnlocksInMemory() { unlocks = new BrawlUnlocks(null); unlocksInMemory = true; }
        /// <summary>Snapshots only (-Show Colours -Level 1-3): the lineup in one colour.</summary>
        public int LineupColour;

        /// <summary>The scenes of this part; null when show is another one.</summary>
        string ModesScene(string show, double demo, int level)
        {
            switch (show)
            {
                case "Practice": case "PracticeFight": case "PracticePause": case "SelectColours": case "LadderColours": case "Colours": case "LadderUnlock": break;
                default: return null;
            }
            UseUnlocksInMemory();
            int gila = Math.Max(0, BrawlData.FighterIndex("gila")), bloom = Math.Max(0, BrawlData.FighterIndex("bloom"));
            Unlocks.Unlock(Player, "bloom", 2); Unlocks.Unlock(Player, "gila", 1); Unlocks.Unlock(Player, "sam", 2);
            Begin();
            switch (show)
            {
                case "Practice":
                    PracticePick = bloom; PracticeColour = 1; PracticeFoe = gila; DummyMode = 1; PracticeStage = 0; OpenPractice(); Sel = 1;
                    for (int i = 0; i < 30; i++) Update(1.0 / 60);
                    return "menu " + Menu;
                case "SelectColours":
                    Menu = Screen.Select; Who[0] = 1; Who[1] = 2; Who[2] = 0; Who[3] = 0; Pick[0] = bloom; Pick[1] = bloom; Colour[0] = 2; Sel = 0;
                    for (int i = 0; i < 30; i++) Update(1.0 / 60);
                    return "menu " + Menu;
                case "LadderColours":
                    Menu = Screen.Ladder; LadderPick = gila; LadderColour = 1; Sel = 0;
                    for (int i = 0; i < 30; i++) Update(1.0 / 60);
                    return "menu " + Menu;
                case "Colours":
                    Menu = Screen.Lineup; LineupColour = Math.Max(0, Math.Min(BrawlUnlocks.Colours - 1, level - 1));
                    for (int i = 0; i < 30; i++) Update(1.0 / 60);
                    return "menu " + Menu + " colour " + LineupColour;
                case "PracticeFight": case "PracticePause":
                    {
                        PracticePick = bloom; PracticeColour = 2; PracticeFoe = gila; DummyMode = 1; PracticeStage = Math.Max(0, Math.Min(BrawlData.Stages.Count - 1, level - 1));
                        StartPractice();
                        // you walk up to the dummy and swing at it now and then (pretend keys)
                        for (int i = 0; i < 60 * (demo > 0 ? demo : 5); i++)
                        {
                            if (i % 40 == 0) Press(Key.D); if (i % 40 == 30) Release(Key.D);
                            if (i % 25 == 10) Press(Key.F); if (i % 25 == 14) Release(Key.F);
                            Update(1.0 / 60);
                        }
                        Release(Key.D); Release(Key.F);
                        if (show == "PracticePause") Pause();
                        return "practice frame " + Sim.Frame + ", dummy " + Sim.F[1].Percent + "%";
                    }
                case "LadderUnlock":
                    {
                        Autopilot = false; Ladder = true; LadderPick = gila; LadderDiff = 2; LadderColour = 0;
                        StartLadder();
                        LadderRound = LadderRounds; LadderScore = 41250;
                        UnlockNote = null; unlocks = new BrawlUnlocks(null);   // nothing unlocked before: both colours are new
                        UnlockForLadder();
                        EndGame(LadderScore, "climbed the ladder with " + BrawlData.Fighters[gila].Name + " on " + LevelNames[LadderDiff], "ladder-" + LadderDiff, "The ladder is climbed!", null);
                        for (int i = 0; i < 60 * 7 && State != Phase.Over; i++) Update(1.0 / 60);
                        return "over: " + UnlockNote;
                    }
            }
            return null;
        }
    }
}
