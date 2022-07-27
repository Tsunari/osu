// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Collections;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets;
using osu.Game.Screens.Select;
using osu.Game.Tests.Resources;
using osuTK.Input;

namespace osu.Game.Tests.Visual.SongSelect
{
    public class TestSceneFilterControl : OsuManualInputManagerTestScene
    {
        protected override Container<Drawable> Content { get; } = new Container { RelativeSizeAxes = Axes.Both };

        private RulesetStore rulesets = null!;
        private BeatmapManager beatmapManager = null!;
        private FilterControl control = null!;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            Dependencies.Cache(rulesets = new RealmRulesetStore(Realm));
            Dependencies.Cache(beatmapManager = new BeatmapManager(LocalStorage, Realm, rulesets, null, Audio, Resources, host, Beatmap.Default));
            Dependencies.Cache(Realm);

            beatmapManager.Import(TestResources.GetQuickTestBeatmapForImport()).WaitSafely();

            base.Content.AddRange(new Drawable[]
            {
                Content
            });
        }

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Realm.Write(r => r.RemoveAll<RealmBeatmapCollection>());

            Child = control = new FilterControl
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.X,
                Height = FilterControl.HEIGHT,
            };
        });

        [Test]
        public void TestEmptyCollectionFilterContainsAllBeatmaps()
        {
            assertCollectionDropdownContains("All beatmaps");
            assertCollectionHeaderDisplays("All beatmaps");
        }

        [Test]
        public void TestCollectionAddedToDropdown()
        {
            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "1"))));
            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "2"))));
            assertCollectionDropdownContains("1");
            assertCollectionDropdownContains("2");
        }

        [Test]
        public void TestCollectionRemovedFromDropdown()
        {
            var first = new RealmBeatmapCollection(name: "1");

            AddStep("add collection", () => Realm.Write(r => r.Add(first)));
            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "2"))));
            AddStep("remove collection", () => Realm.Write(r => r.Remove(first)));

            assertCollectionDropdownContains("1", false);
            assertCollectionDropdownContains("2");
        }

        [Test]
        public void TestCollectionRenamed()
        {
            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "1"))));
            AddStep("select collection", () =>
            {
                var dropdown = control.ChildrenOfType<CollectionFilterDropdown>().Single();
                dropdown.Current.Value = dropdown.ItemSource.ElementAt(1);
            });

            addExpandHeaderStep();

            AddStep("change name", () => Realm.Write(_ => getFirstCollection().Name = "First"));

            assertCollectionDropdownContains("First");
            assertCollectionHeaderDisplays("First");
        }

        [Test]
        public void TestAllBeatmapFilterDoesNotHaveAddButton()
        {
            addExpandHeaderStep();
            AddStep("hover all beatmaps", () => InputManager.MoveMouseTo(getAddOrRemoveButton(0)));
            AddAssert("'All beatmaps' filter does not have add button", () => !getAddOrRemoveButton(0).IsPresent);
        }

        [Test]
        public void TestCollectionFilterHasAddButton()
        {
            addExpandHeaderStep();
            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "1"))));
            AddStep("hover collection", () => InputManager.MoveMouseTo(getAddOrRemoveButton(1)));
            AddAssert("collection has add button", () => getAddOrRemoveButton(1).IsPresent);
        }

        [Test]
        public void TestButtonDisabledAndEnabledWithBeatmapChanges()
        {
            addExpandHeaderStep();

            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "1"))));

            AddStep("select available beatmap", () => Beatmap.Value = beatmapManager.GetWorkingBeatmap(beatmapManager.GetAllUsableBeatmapSets().First().Beatmaps[0]));
            AddAssert("button enabled", () => getAddOrRemoveButton(1).Enabled.Value);

            AddStep("set dummy beatmap", () => Beatmap.SetDefault());
            AddAssert("button disabled", () => !getAddOrRemoveButton(1).Enabled.Value);
        }

        [Test]
        public void TestButtonChangesWhenAddedAndRemovedFromCollection()
        {
            addExpandHeaderStep();

            AddStep("select available beatmap", () => Beatmap.Value = beatmapManager.GetWorkingBeatmap(beatmapManager.GetAllUsableBeatmapSets().First().Beatmaps[0]));

            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "1"))));
            AddAssert("button is plus", () => getAddOrRemoveButton(1).Icon.Equals(FontAwesome.Solid.PlusSquare));

            AddStep("add beatmap to collection", () => getFirstCollection().BeatmapMD5Hashes.Add(Beatmap.Value.BeatmapInfo.MD5Hash));
            AddAssert("button is minus", () => getAddOrRemoveButton(1).Icon.Equals(FontAwesome.Solid.MinusSquare));

            AddStep("remove beatmap from collection", () => getFirstCollection().BeatmapMD5Hashes.Clear());
            AddAssert("button is plus", () => getAddOrRemoveButton(1).Icon.Equals(FontAwesome.Solid.PlusSquare));
        }

        [Test]
        public void TestButtonAddsAndRemovesBeatmap()
        {
            addExpandHeaderStep();

            AddStep("select available beatmap", () => Beatmap.Value = beatmapManager.GetWorkingBeatmap(beatmapManager.GetAllUsableBeatmapSets().First().Beatmaps[0]));

            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "1"))));
            AddAssert("button is plus", () => getAddOrRemoveButton(1).Icon.Equals(FontAwesome.Solid.PlusSquare));

            addClickAddOrRemoveButtonStep(1);
            AddAssert("collection contains beatmap", () => getFirstCollection().BeatmapMD5Hashes.Contains(Beatmap.Value.BeatmapInfo.MD5Hash));
            AddAssert("button is minus", () => getAddOrRemoveButton(1).Icon.Equals(FontAwesome.Solid.MinusSquare));

            addClickAddOrRemoveButtonStep(1);
            AddAssert("collection does not contain beatmap", () => !getFirstCollection().BeatmapMD5Hashes.Contains(Beatmap.Value.BeatmapInfo.MD5Hash));
            AddAssert("button is plus", () => getAddOrRemoveButton(1).Icon.Equals(FontAwesome.Solid.PlusSquare));
        }

        [Test]
        public void TestManageCollectionsFilterIsNotSelected()
        {
            addExpandHeaderStep();

            AddStep("add collection", () => Realm.Write(r => r.Add(new RealmBeatmapCollection(name: "1"))));
            AddStep("select collection", () =>
            {
                InputManager.MoveMouseTo(getCollectionDropdownItems().ElementAt(1));
                InputManager.Click(MouseButton.Left);
            });

            addExpandHeaderStep();

            AddStep("click manage collections filter", () =>
            {
                InputManager.MoveMouseTo(getCollectionDropdownItems().Last());
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("collection filter still selected", () => control.CreateCriteria().Collection?.Name.Value == "1");
        }

        private RealmBeatmapCollection getFirstCollection() => Realm.Run(r => r.All<RealmBeatmapCollection>().First());

        private void assertCollectionHeaderDisplays(string collectionName, bool shouldDisplay = true)
            => AddAssert($"collection dropdown header displays '{collectionName}'",
                () => shouldDisplay == (control.ChildrenOfType<CollectionFilterDropdown.CollectionDropdownHeader>().Single().ChildrenOfType<SpriteText>().First().Text == collectionName));

        private void assertCollectionDropdownContains(string collectionName, bool shouldContain = true) =>
            AddAssert($"collection dropdown {(shouldContain ? "contains" : "does not contain")} '{collectionName}'",
                // A bit of a roundabout way of going about this, see: https://github.com/ppy/osu-framework/issues/3871 + https://github.com/ppy/osu-framework/issues/3872
                () => shouldContain == (getCollectionDropdownItems().Any(i => i.ChildrenOfType<CompositeDrawable>().OfType<IHasText>().First().Text == collectionName)));

        private IconButton getAddOrRemoveButton(int index)
            => getCollectionDropdownItems().ElementAt(index).ChildrenOfType<IconButton>().Single();

        private void addExpandHeaderStep() => AddStep("expand header", () =>
        {
            InputManager.MoveMouseTo(control.ChildrenOfType<CollectionFilterDropdown.CollectionDropdownHeader>().Single());
            InputManager.Click(MouseButton.Left);
        });

        private void addClickAddOrRemoveButtonStep(int index) => AddStep("click add or remove button", () =>
        {
            InputManager.MoveMouseTo(getAddOrRemoveButton(index));
            InputManager.Click(MouseButton.Left);
        });

        private IEnumerable<Dropdown<CollectionFilterMenuItem>.DropdownMenu.DrawableDropdownMenuItem> getCollectionDropdownItems()
            => control.ChildrenOfType<CollectionFilterDropdown>().Single().ChildrenOfType<Dropdown<CollectionFilterMenuItem>.DropdownMenu.DrawableDropdownMenuItem>();
    }
}
