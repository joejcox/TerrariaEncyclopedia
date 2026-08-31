using System.Collections.Generic;
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
            var focus = new FocusHarness(new ExclusiveTextEntrySession(underlying));

            focus.Focus(ItemsOwner);
            Assert.False(focus.CanToggleWithP);

            focus.SwitchSection(NpcsOwner);
            Assert.True(focus.CanToggleWithP);

            focus.Focus(NpcsOwner);
            Assert.False(focus.CanToggleWithP);

            focus.Unfocus(NpcsOwner);
            Assert.True(focus.CanToggleWithP);
            Assert.Equal(
                new[] { "begin:" + ItemsOwner, "end:" + ItemsOwner, "begin:" + NpcsOwner, "end:" + NpcsOwner },
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

        private const string ItemsOwner = "terraria-encyclopedia-items";
        private const string NpcsOwner = "terraria-encyclopedia-npcs";

        private sealed class FocusHarness
        {
            private readonly ExclusiveTextEntrySession session;
            private string section = ItemsOwner;

            public FocusHarness(ExclusiveTextEntrySession session)
            {
                this.session = session;
            }

            public bool CanToggleWithP =>
                !session.IsOwner(ItemsOwner) && !session.IsOwner(NpcsOwner);

            public void Focus(string nextOwner) => session.Begin(nextOwner);

            public void SwitchSection(string nextSection)
            {
                if (section == nextSection)
                    return;

                session.End(section);
                section = nextSection;
            }

            public void Unfocus(string endingOwner) => session.End(endingOwner);
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
