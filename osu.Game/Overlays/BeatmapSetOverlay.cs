﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Containers;
using osu.Game.Online.API.Requests;
using osu.Game.Overlays.BeatmapSet;
using osu.Game.Overlays.BeatmapSet.Scores;
using osu.Game.Overlays.Comments;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays
{
    public class BeatmapSetOverlay : FullscreenOverlay
    {
        public const float X_PADDING = 40;
        public const float Y_PADDING = 25;
        public const float RIGHT_WIDTH = 275;
        protected readonly Header Header;

        [Resolved]
        private RulesetStore rulesets { get; set; }

        private readonly Bindable<BeatmapSetInfo> beatmapSet = new Bindable<BeatmapSetInfo>();

        // receive input outside our bounds so we can trigger a close event on ourselves.
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        private readonly Box background;

        public BeatmapSetOverlay()
            : base(OverlayColourScheme.Blue)
        {
            OsuScrollContainer scroll;
            Info info;
            BeatmapSetCommentsContainer comments;

            Children = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                scroll = new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarVisible = false,
                    Child = new ReverseChildIDFillFlowContainer<Section>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 20),
                        Children = new[]
                        {
                            new Section
                            {
                                Child = new ReverseChildIDFillFlowContainer<Drawable>
                                {
                                    AutoSizeAxes = Axes.Y,
                                    RelativeSizeAxes = Axes.X,
                                    Direction = FillDirection.Vertical,
                                    Children = new Drawable[]
                                    {
                                        Header = new Header(),
                                        info = new Info()
                                    }
                                },
                            },
                            new Section
                            {
                                Child = new ScoresContainer
                                {
                                    Beatmap = { BindTarget = Header.Picker.Beatmap }
                                }
                            },
                            new Section
                            {
                                Child = comments = new BeatmapSetCommentsContainer()
                            }
                        },
                    },
                },
            };

            Header.BeatmapSet.BindTo(beatmapSet);
            info.BeatmapSet.BindTo(beatmapSet);
            comments.BeatmapSet.BindTo(beatmapSet);

            Header.Picker.Beatmap.ValueChanged += b =>
            {
                info.Beatmap = b.NewValue;

                scroll.ScrollToStart();
            };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            background.Colour = ColourProvider.Background6;
        }

        protected override void PopOutComplete()
        {
            base.PopOutComplete();
            beatmapSet.Value = null;
        }

        protected override bool OnClick(ClickEvent e)
        {
            Hide();
            return true;
        }

        public void FetchAndShowBeatmap(int beatmapId)
        {
            beatmapSet.Value = null;

            var req = new GetBeatmapSetRequest(beatmapId, BeatmapSetLookupType.BeatmapId);
            req.Success += res =>
            {
                beatmapSet.Value = res.ToBeatmapSet(rulesets);
                Header.Picker.Beatmap.Value = Header.BeatmapSet.Value.Beatmaps.First(b => b.OnlineBeatmapID == beatmapId);
            };
            API.Queue(req);

            Show();
        }

        public void FetchAndShowBeatmapSet(int beatmapSetId)
        {
            beatmapSet.Value = null;

            var req = new GetBeatmapSetRequest(beatmapSetId);
            req.Success += res => beatmapSet.Value = res.ToBeatmapSet(rulesets);
            API.Queue(req);

            Show();
        }

        /// <summary>
        /// Show an already fully-populated beatmap set.
        /// </summary>
        /// <param name="set">The set to show.</param>
        public void ShowBeatmapSet(BeatmapSetInfo set)
        {
            beatmapSet.Value = set;
            Show();
        }

        private class Section : Container
        {
            public Section()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Masking = true;
                EdgeEffect = new EdgeEffectParameters
                {
                    Colour = Color4.Black.Opacity(0.25f),
                    Type = EdgeEffectType.Shadow,
                    Radius = 3,
                    Offset = new Vector2(0f, 1f),
                };
            }
        }

        private class BeatmapSetCommentsContainer : CommentsContainer
        {
            public readonly Bindable<BeatmapSetInfo> BeatmapSet = new Bindable<BeatmapSetInfo>();

            public BeatmapSetCommentsContainer()
            {
                BeatmapSet.BindValueChanged(beatmapSet =>
                {
                    if (beatmapSet.NewValue?.OnlineBeatmapSetID.HasValue != true)
                    {
                        Hide();
                    }
                    else
                    {
                        Show();
                        ShowComments(CommentableType.Beatmapset, beatmapSet.NewValue.OnlineBeatmapSetID.Value);
                    }
                }, true);
            }
        }
    }
}
