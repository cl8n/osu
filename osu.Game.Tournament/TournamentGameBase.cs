// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API.Requests;
using osu.Game.Rulesets;
using osu.Game.Tournament.Screens.Ladder.Components;

namespace osu.Game.Tournament
{
    public abstract class TournamentGameBase : OsuGameBase
    {
        private const string bracket_filename = "bracket.json";

        protected LadderInfo Ladder;
        private Storage storage;

        private DependencyContainer dependencies;

        [Cached]
        private readonly Bindable<RulesetInfo> ruleset = new Bindable<RulesetInfo>();

        [Cached]
        private readonly Bindable<MatchPairing> currentMatch = new Bindable<MatchPairing>();

        private Bindable<Size> windowSize;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            return dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
        }

        [BackgroundDependencyLoader]
        private void load(Storage storage, FrameworkConfigManager frameworkConfig)
        {
            this.storage = storage;

            windowSize = frameworkConfig.GetBindable<Size>(FrameworkSetting.WindowedSize);

            string content = null;
            if (storage.Exists(bracket_filename))
                using (Stream stream = storage.GetStream(bracket_filename, FileAccess.Read, FileMode.Open))
                using (var sr = new StreamReader(stream))
                {
                    content = sr.ReadToEnd();
                }

            Ladder = content != null ? JsonConvert.DeserializeObject<LadderInfo>(content) : new LadderInfo();

            dependencies.Cache(Ladder);

            bool addedInfo = false;

            // assign teams
            foreach (var pairing in Ladder.Pairings)
            {
                pairing.Team1.Value = Ladder.Teams.FirstOrDefault(t => t.Acronym == pairing.Team1Acronym);
                pairing.Team2.Value = Ladder.Teams.FirstOrDefault(t => t.Acronym == pairing.Team2Acronym);
            }

            // assign progressions
            foreach (var pair in Ladder.Progressions)
            {
                var src = Ladder.Pairings.FirstOrDefault(p => p.ID == pair.Item1);
                var dest = Ladder.Pairings.FirstOrDefault(p => p.ID == pair.Item2);

                if (src == null) throw new InvalidOperationException();

                if (dest != null)
                {
                    if (pair.Losers)
                        src.LosersProgression.Value = dest;
                    else
                        src.Progression.Value = dest;
                }
            }

            foreach (var group in Ladder.Groupings)
            foreach (var id in group.Pairings)
                Ladder.Pairings.Single(p => p.ID == id).Grouping.Value = group;

            foreach (var g in Ladder.Groupings)
            foreach (var b in g.Beatmaps)
                if (b.BeatmapInfo == null)
                {
                    var req = new GetBeatmapRequest(new BeatmapInfo { OnlineBeatmapID = b.ID });
                    req.Success += i => b.BeatmapInfo = i.ToBeatmap(RulesetStore);
                    req.Perform(API);

                    addedInfo = true;
                }

            //todo: temp
            currentMatch.Value = Ladder.Pairings.FirstOrDefault();

            if (addedInfo)
                SaveChanges();

            Add(new OsuButton
            {
                Text = "Save Changes",
                Width = 140,
                Height = 50,
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Padding = new MarginPadding(10),
                Action = SaveChanges,
            });
        }

        protected override void LoadComplete()
        {
            MenuCursorContainer.Cursor.Alpha = 0;
        }

        protected override void Update()
        {

            base.Update();
            var minWidth = (int)(windowSize.Value.Height / 9f * 16 + 400);
            if (windowSize.Value.Width < minWidth)
            {
                // todo: can be removed after ppy/osu-framework#1975
                windowSize.Value = Host.Window.ClientSize = new Size(minWidth, windowSize.Value.Height);
            }
        }

        protected virtual void SaveChanges()
        {
            using (var stream = storage.GetStream(bracket_filename, FileAccess.Write, FileMode.Create))
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(JsonConvert.SerializeObject(Ladder,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    }));
            }
        }
    }
}
