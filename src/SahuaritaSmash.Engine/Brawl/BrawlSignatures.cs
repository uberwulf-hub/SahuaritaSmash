// Classic Games - Sahuarita Smash!, the fighters' own attacks (Andrew, 2026-10-31: "up, down, left, right combos for each
// of the attacks like in super smash bros ... each unique per character type"). Every fighter starts with the ordinary
// moves (BrawlData.Normals); this gives each of its 13 attacks its own name, picture (Anim), effect (Extra, drawn by
// BrawlView.SigFront) and feel: a flurry, a shock, a launcher, a trip, a spike, a wider or heavier swing. The strong
// side attack keeps its power (the tests check every fighter wins a life with it at 60-200%), and every attack still
// winds up for 3+ frames. Names are our own words.
using System;
using System.Collections.Generic;

namespace ClassicGames
{
    public static class BrawlSignatures
    {
        /// <summary>The 13 attacks in the order the pause panel lists them.</summary>
        public static readonly string[] AttackIds = { "jab", "ftilt", "utilt", "dtilt", "dash", "fstrong", "ustrong", "dstrong", "nair", "fair", "bair", "uair", "dair" };
        /// <summary>How each is done, for people (arrows stand for the direction keys).</summary>
        public static string HowTo(string id)
        {
            string L = ((char)0x2190).ToString(), R = ((char)0x2192).ToString(), U = ((char)0x2191).ToString(), D = ((char)0x2193).ToString();
            switch (id)
            {
                case "jab": return "Attack";
                case "ftilt": return L + " / " + R + " + Attack";
                case "utilt": return U + " + Attack";
                case "dtilt": return D + " + Attack";
                case "dash": return "Attack while running";
                case "fstrong": return L + " / " + R + " + Strong";
                case "ustrong": return U + " + Strong";
                case "dstrong": return D + " + Strong";
                case "nair": return "Attack in the air";
                case "fair": return "Air: forward + Attack";
                case "bair": return "Air: back + Attack";
                case "uair": return "Air: " + U + " + Attack";
                default: return "Air: " + D + " + Attack";
            }
        }

        // each line: move, name, picture, effect (null = keep), how it hits (comma list, see Tweak)
        static readonly Dictionary<string, string[][]> table = new Dictionary<string, string[][]>
        {
            { "bloom", new[] {
                new[] { "jab", "Petal pat", "jab", "petals", "rapid" },
                new[] { "ftilt", "Twinkle poke", "punch", "stars", "far" },
                new[] { "utilt", "Sprout up", "upswing", "petals", "launch" },
                new[] { "dtilt", "Seed skip", "sweep", "petals", "trip" },
                new[] { "dash", "Cartwheel", "twirl", "stars", "" },
                new[] { "fstrong", "Star burst", "bigpunch", "stars", "" },
                new[] { "ustrong", "Shooting star", "uppercut", "stars", "" },
                new[] { "dstrong", "Petal ring", "splits", "petals", "" },
                new[] { "nair", "Pinwheel", "twirl", "petals", "spin" },
                new[] { "fair", "Wish kick", "kick", "stars", "quick" },
                new[] { "bair", "Blossom bump", "backkick", "petals", "" },
                new[] { "uair", "Star flip", "flip", "stars", "" },
                new[] { "dair", "Daisy drop", "stomp", "petals", "spike" } } },
            { "specimen", new[] {
                new[] { "jab", "Flick", "lash", null, "rapid" },
                new[] { "ftilt", "Tendril whip", "lash", null, "far" },
                new[] { "utilt", "Reach up", "upswing", null, "launch,far" },
                new[] { "dtilt", "Creep", "sweep", null, "trip,far" },
                new[] { "dash", "Lunge bite", "headbutt", "chomp", "" },
                new[] { "fstrong", "Big lash", "bigswing", null, "" },
                new[] { "ustrong", "Geyser", "uppercut", null, "" },
                new[] { "dstrong", "Spread", "splits", null, "wide" },
                new[] { "nair", "Tangle", "twirl", null, "spin" },
                new[] { "fair", "Swipe", "lash", null, "far" },
                new[] { "bair", "Tail snap", "backkick", null, "" },
                new[] { "uair", "Fountain", "flip", null, "" },
                new[] { "dair", "Drip", "stomp", null, "spike" } } },
            { "trooper", new[] {
                new[] { "jab", "Rifle butt", "poke", "", "quick" },
                new[] { "ftilt", "Boot kick", "kick", "", "" },
                new[] { "utilt", "Flare", "upswing", "muzzle", "launch" },
                new[] { "dtilt", "Prone sweep", "sweep", "", "trip" },
                new[] { "dash", "Tackle", "shoulder", "", "heavy" },
                new[] { "fstrong", "Point-blank", "aim", "muzzle", "" },
                new[] { "ustrong", "Flak burst", "uppercut", "muzzle", "" },
                new[] { "dstrong", "Mine pop", "slam", "boom", "wide" },
                new[] { "nair", "Jet spin", "twirl", "jet", "spin" },
                new[] { "fair", "Knee strike", "kick", "", "quick" },
                new[] { "bair", "Back kick", "backkick", "", "" },
                new[] { "uair", "Jet flip", "flip", "jet", "" },
                new[] { "dair", "Boot stomp", "stomp", "jet", "spike" } } },
            { "juniper", new[] {
                new[] { "jab", "Drill tap", "jab", "drillfx", "rapid" },
                new[] { "ftilt", "Drill jab", "punch", "drillfx", "rapid" },
                new[] { "utilt", "Ore toss", "upswing", "ore", "launch" },
                new[] { "dtilt", "Survey sweep", "sweep", "", "trip" },
                new[] { "dash", "Jet dash", "dash", "jet", "" },
                new[] { "fstrong", "Mega drill", "bigpunch", "drillfx", "" },
                new[] { "ustrong", "Blast off", "uppercut", "jet", "" },
                new[] { "dstrong", "Core sample", "slam", "drillfx", "" },
                new[] { "nair", "Spin cycle", "twirl", "jet", "spin" },
                new[] { "fair", "Thruster kick", "kick", "jet", "" },
                new[] { "bair", "Retro rocket", "backkick", "jet", "heavy" },
                new[] { "uair", "Antenna zap", "flip", "spark", "zap" },
                new[] { "dair", "Moon boot", "stomp", "", "spike" } } },
            { "dusty", new[] {
                new[] { "jab", "Peck peck", "peck", "", "rapid" },
                new[] { "ftilt", "Talon kick", "kick", "dust", "quick" },
                new[] { "utilt", "Crest flick", "headbutt", "feathers", "launch" },
                new[] { "dtilt", "Dust kick", "lowkick", "dust", "trip" },
                new[] { "dash", "Beep beep", "dash", "dust", "quick" },
                new[] { "fstrong", "Big peck", "headbutt", "feathers", "" },
                new[] { "ustrong", "Tail fan", "uppercut", "feathers", "" },
                new[] { "dstrong", "Scratch", "splits", "dust", "" },
                new[] { "nair", "Whirlwind", "twirl", "dust", "spin" },
                new[] { "fair", "Drop kick", "kick", "", "" },
                new[] { "bair", "Tail whip", "backkick", "feathers", "quick" },
                new[] { "uair", "Feather flip", "flip", "feathers", "" },
                new[] { "dair", "Talon stomp", "stomp", "dust", "spike" } } },
            { "sam", new[] {
                new[] { "jab", "Arm slap", "jab", "", "heavy" },
                new[] { "ftilt", "Needle palm", "punch", "needles", "far" },
                new[] { "utilt", "Bloom up", "upswing", "petals", "launch" },
                new[] { "dtilt", "Root trip", "sweep", "dust", "trip" },
                new[] { "dash", "Tumbleweed", "twirl", "dust", "" },
                new[] { "fstrong", "Prickly clap", "clap", "needles", "" },
                new[] { "ustrong", "Monsoon rise", "uppercut", "needles", "" },
                new[] { "dstrong", "Root slam", "slam", "dust", "wide" },
                new[] { "nair", "Spine ring", "twirl", "needles", "spin" },
                new[] { "fair", "Arm club", "overhead", "", "heavy" },
                new[] { "bair", "Backside spines", "backkick", "needles", "" },
                new[] { "uair", "Flower crown", "flip", "petals", "" },
                new[] { "dair", "Seed drop", "stomp", "", "spike" } } },
            { "gila", new[] {
                new[] { "jab", "Claw scratch", "jab", "", "rapid" },
                new[] { "ftilt", "Beaded slap", "punch", "", "heavy" },
                new[] { "utilt", "Snout toss", "headbutt", "dust", "launch" },
                new[] { "dtilt", "Tail trip", "sweep", "dust", "trip,far" },
                new[] { "dash", "Belly slide", "shoulder", "dust", "heavy" },
                new[] { "fstrong", "Big chomp", "bigpunch", "chomp", "" },
                new[] { "ustrong", "Sunbeam stretch", "uppercut", "", "" },
                new[] { "dstrong", "Wash flood", "slam", "dust", "wide" },
                new[] { "nair", "Rolling lizard", "twirl", "dust", "spin" },
                new[] { "fair", "Snap", "headbutt", "chomp", "quick" },
                new[] { "bair", "Tail swat", "backkick", "", "heavy" },
                new[] { "uair", "Tongue up", "flip", "", "far" },
                new[] { "dair", "Lizard drop", "stomp", "dust", "spike" } } },
            { "teddy", new[] {
                new[] { "jab", "Stick poke", "poke", "", "quick" },
                new[] { "ftilt", "Rough Rider swing", "swing", "", "far" },
                new[] { "utilt", "Hat tip", "upswing", "bully", "launch" },
                new[] { "dtilt", "Low sweep", "sweep", "", "trip,far" },
                new[] { "dash", "Charge up the hill", "shoulder", "dust", "heavy" },
                new[] { "fstrong", "The big stick", "bigswing", "bully", "" },
                new[] { "ustrong", "Bully uppercut", "uppercut", "bully", "" },
                new[] { "dstrong", "Stick sweep", "splits", "", "wide" },
                new[] { "nair", "Lasso spin", "twirl", "lasso", "spin" },
                new[] { "fair", "Overhead chop", "overhead", "", "" },
                new[] { "bair", "Spur kick", "backkick", "", "quick" },
                new[] { "uair", "Hat trick", "flip", "", "" },
                new[] { "dair", "Boot stomp", "stomp", "dust", "spike" } } },
            { "ben", new[] {
                new[] { "jab", "Cane tap", "poke", "", "quick" },
                new[] { "ftilt", "Key zap", "swing", "spark", "zap" },
                new[] { "utilt", "Lightning rod", "upswing", "spark", "zap,launch" },
                new[] { "dtilt", "Penny sweep", "sweep", "", "trip" },
                new[] { "dash", "Printing press", "shoulder", "pages", "" },
                new[] { "fstrong", "Thunderclap", "clap", "spark", "zap" },
                new[] { "ustrong", "Kite strike", "uppercut", "spark", "zap" },
                new[] { "dstrong", "Leyden jar", "slam", "spark", "zap,wide" },
                new[] { "nair", "Almanac spin", "twirl", "pages", "spin" },
                new[] { "fair", "Cane swat", "overhead", "", "" },
                new[] { "bair", "Coat-tail flick", "backkick", "", "quick" },
                new[] { "uair", "Key on a string", "flip", "spark", "zap" },
                new[] { "dair", "Bifocal bonk", "stomp", "", "spike" } } },
            { "knight", new[] {
                new[] { "jab", "Packet jab", "poke", "bits", "rapid" },
                new[] { "ftilt", "Data slash", "swing", "bits", "far" },
                new[] { "utilt", "Upload", "upswing", "bits", "launch" },
                new[] { "dtilt", "Port scan", "sweep", "bits", "trip,far" },
                new[] { "dash", "Shield bash", "shoulder", "", "heavy" },
                new[] { "fstrong", "Firewall cleave", "bigswing", "fire", "" },
                new[] { "ustrong", "Flame pillar", "uppercut", "fire", "" },
                new[] { "dstrong", "Ground fault", "slam", "fire", "wide" },
                new[] { "nair", "Antivirus spin", "twirl", "bits", "spin" },
                new[] { "fair", "Overclock chop", "overhead", "fire", "heavy" },
                new[] { "bair", "Backup", "backkick", "", "" },
                new[] { "uair", "Ping", "flip", "bits", "" },
                new[] { "dair", "Hard drop", "stomp", "fire", "spike" } } },
            { "paul", new[] {
                new[] { "jab", "Axe handle", "poke", "", "heavy" },
                new[] { "ftilt", "Timber chop", "swing", "chips", "far" },
                new[] { "utilt", "Flapjack flip", "upswing", "chips", "launch" },
                new[] { "dtilt", "Log roll", "sweep", "chips", "trip" },
                new[] { "dash", "Shoulder charge", "shoulder", "dust", "heavy" },
                new[] { "fstrong", "TIMBER!", "bigswing", "chips", "" },
                new[] { "ustrong", "Tree toss", "uppercut", "chips", "" },
                new[] { "dstrong", "Ground split", "slam", "dust", "wide" },
                new[] { "nair", "Whirlwind axe", "twirl", "chips", "spin" },
                new[] { "fair", "Overhead chop", "overhead", "chips", "heavy" },
                new[] { "bair", "Back haul", "backkick", "", "" },
                new[] { "uair", "Sky chop", "flip", "chips", "" },
                new[] { "dair", "Boot quake", "stomp", "dust", "spike" } } },
            { "pecos", new[] {
                new[] { "jab", "Knuckle tap", "jab", "", "rapid" },
                new[] { "ftilt", "Rope crack", "swing", "crack", "far" },
                new[] { "utilt", "Hat flip", "upswing", "", "launch" },
                new[] { "dtilt", "Spur sweep", "sweep", "dust", "trip,far" },
                new[] { "dash", "Stampede", "shoulder", "dust", "heavy" },
                new[] { "fstrong", "Tall-tale haymaker", "bigpunch", "stars", "" },
                new[] { "ustrong", "Cyclone uppercut", "uppercut", "gust", "" },
                new[] { "dstrong", "Rodeo loop", "splits", "lasso", "wide" },
                new[] { "nair", "Bucking bronco", "spin", "", "spin" },
                new[] { "fair", "Rope lash", "overhead", "crack", "far" },
                new[] { "bair", "Mule kick", "backkick", "", "heavy" },
                new[] { "uair", "Rope the moon", "flip", "lasso", "far" },
                new[] { "dair", "Boot heel", "stomp", "dust", "spike" } } },
        };

        /// <summary>Called once from BrawlData (after the ordinary moves, before the hit groups are set).</summary>
        public static void Apply(IEnumerable<BFighterDef> fighters)
        {
            foreach (BFighterDef f in fighters)
            {
                string[][] rows;
                if (!table.TryGetValue(f.Id, out rows)) continue;
                foreach (string[] row in rows)
                {
                    BMove m = f.M(row[0]);
                    if (m == null) continue;
                    m.Name = row[1];
                    m.Anim = row[2];
                    if (row[3] != null) m.Extra = row[3];
                    foreach (string tw in row[4].Split(',')) if (tw.Length > 0) Tweak(m, tw.Trim());
                }
            }
        }

        static void Tweak(BMove m, string tw)
        {
            if (m.Hits.Count == 0) return;
            switch (tw)
            {
                case "rapid":
                    {
                        // a flurry: the first hit lands every 3 frames for a moment, softly, before the move ends
                        BHit h = m.Hits[0];
                        h.To = Math.Min(m.Len - 2, h.From + 8); h.Rehit = 3; h.Dmg = Math.Max(1, h.Dmg * 0.45); h.Grow *= 0.5; h.Base = Math.Min(h.Base, 12);
                        break;
                    }
                case "spin":
                    {
                        BHit h = m.Hits[0];
                        h.Rehit = 5; h.Dmg = Math.Max(1.5, h.Dmg * 0.5);
                        break;
                    }
                case "zap": foreach (BHit h in m.Hits) h.Electric = true; break;
                case "spike": foreach (BHit h in m.Hits) h.Ang = 285; break;
                case "launch": foreach (BHit h in m.Hits) { h.Ang = 85; h.Base += 6; } break;
                case "trip": foreach (BHit h in m.Hits) { h.Ang = 18; h.Base = Math.Max(10, h.Base * 0.8); } break;
                case "far": foreach (BHit h in m.Hits) { h.X *= 1.22; h.R *= 1.1; } break;
                case "heavy": foreach (BHit h in m.Hits) { h.Dmg *= 1.18; h.Grow *= 1.08; } m.Len += 5; break;
                case "quick": { int min = m.LastActive + 6; m.Len = Math.Max(min, m.Len - 4); foreach (BHit h in m.Hits) h.Dmg *= 0.9; break; }
                case "wide":
                    {
                        // a matching hit behind
                        var add = new List<BHit>();
                        foreach (BHit h in m.Hits) if (h.X > 0) add.Add(new BHit { From = h.From, To = h.To, X = -h.X, Y = h.Y, R = h.R, Dmg = h.Dmg, Ang = h.Ang == 361 ? 361 : 180 - h.Ang, Base = h.Base, Grow = h.Grow, Electric = h.Electric });
                        bool hasBack = m.Hits.Exists(h => h.X < 0);
                        if (!hasBack) m.Hits.AddRange(add);
                        break;
                    }
            }
        }
    }
}
