using System;
using System.Collections.Generic;
using TerrariaEncyclopedia.Hosting;
using TerrariaEncyclopedia.Runtime.UI.Input;
using TerrariaEncyclopedia.UI;
using Xunit;

namespace TerrariaEncyclopedia.Tests
{
    public sealed class TextEntryFocusBehaviourTests
    {
        [Fact]
        public void SectionSwitchReleasesOutgoingSearchBeforeToggleEligibilityIsChecked()
        {
            var underlying = new RecordingTextEntrySession();
            var session = new ExclusiveTextEntrySession(underlying);
            var section = new EncyclopediaSectionSelection(
                () => session.End(ItemsOwner),
                () => session.End(NpcsOwner),
                () => session.End(EventsOwner));

            session.Begin(ItemsOwner);
            Assert.False(CanToggleWithP(session));

            section.SwitchTo(EncyclopediaSection.Npcs);
            Assert.True(CanToggleWithP(session));

            session.Begin(NpcsOwner);
            Assert.False(CanToggleWithP(session));

            section.SwitchTo(EncyclopediaSection.Events);
            Assert.True(CanToggleWithP(session));

            session.Begin(EventsOwner);
            Assert.False(CanToggleWithP(session));

            section.SwitchTo(EncyclopediaSection.Items);
            Assert.True(CanToggleWithP(session));
            Assert.Equal(
                new[]
                {
                    "begin:" + ItemsOwner,
                    "end:" + ItemsOwner,
                    "begin:" + NpcsOwner,
                    "end:" + NpcsOwner,
                    "begin:" + EventsOwner,
                    "end:" + EventsOwner
                },
                underlying.Events);
        }

        [Fact]
        public void MovingFocusEndsThePreviousOwnerBeforeActivatingTheNext()
        {
            var underlying = new RecordingTextEntrySession();
            var session = new ExclusiveTextEntrySession(underlying);

            session.Begin(ItemsOwner);
            session.Begin(NpcsOwner);

            Assert.False(session.IsOwner(ItemsOwner));
            Assert.True(session.IsOwner(NpcsOwner));
            Assert.Equal(
                new[] { "begin:" + ItemsOwner, "end:" + ItemsOwner, "begin:" + NpcsOwner },
                underlying.Events);
        }

        [Fact]
        public void FeatureFailureReleasesTextEntryAndInputWhenUnloadThrows()
        {
            var events = new List<string>();
            var cleanup = new FeatureFailureCleanup(
                () =>
                {
                    events.Add("unload");
                    throw new InvalidOperationException("simulated feature failure");
                },
                () => events.Add("release-text-entry"),
                () => events.Add("restore-input-block"));

            Assert.Throws<InvalidOperationException>(() => cleanup.Run());
            Assert.Equal(
                new[] { "unload", "release-text-entry", "restore-input-block" },
                events);
        }

        private const string ItemsOwner = "terraria-encyclopedia-items";
        private const string NpcsOwner = "terraria-encyclopedia-npcs";
        private const string EventsOwner = "terraria-encyclopedia-events";

        private static bool CanToggleWithP(ExclusiveTextEntrySession session)
        {
            return !session.IsOwner(ItemsOwner)
                && !session.IsOwner(NpcsOwner)
                && !session.IsOwner(EventsOwner);
        }

        private sealed class RecordingTextEntrySession : ITextEntrySession
        {
            public List<string> Events { get; } = new List<string>();

            public void Begin(string ownerId) => Events.Add("begin:" + ownerId);

            public string Read(string currentText, int maxLength) => currentText;

            public void End(string ownerId) => Events.Add("end:" + ownerId);
        }
    }
}
