using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace TerrariaEncyclopedia.Runtime.Tests
{
    public sealed class FeatureRuntimeTests
    {
        [Fact]
        public void Registered_features_receive_lifecycle_callbacks_in_order()
        {
            var calls = new List<string>();
            var first = new FakeFeature("first", calls);
            var second = new FakeFeature("second", calls);
            var runtime = CreateRuntime(new CollectingLogSink(), first, second);

            runtime.Initialise();
            runtime.Update(InputSnapshot.Empty);
            runtime.Render();
            runtime.Unload();
            runtime.Unload();

            Assert.Equal(
                new[]
                {
                    "first:initialise", "second:initialise",
                    "first:update", "second:update",
                    "first:render", "second:render",
                    "first:unload", "second:unload"
                },
                calls);
        }

        [Fact]
        public void Toggle_action_runs_once_for_each_key_press_transition()
        {
            var calls = new List<string>();
            var feature = new FakeFeature("feature", calls);
            var runtime = CreateRuntime(new CollectingLogSink(), feature);
            var toggleKey = new InputKey("F6");
            feature.OnInitialise = context =>
                context.KeyBindings.RegisterToggle(toggleKey, () => calls.Add("feature:toggle"));

            runtime.Initialise();
            runtime.Update(InputSnapshot.Empty);
            runtime.Update(new InputSnapshot(new[] { toggleKey }));
            runtime.Update(new InputSnapshot(new[] { toggleKey }));
            runtime.Update(InputSnapshot.Empty);
            runtime.Update(new InputSnapshot(new[] { toggleKey }));

            Assert.Equal(2, calls.Count(call => call == "feature:toggle"));
            Assert.Equal(
                new[] { false, true, false, false, true },
                feature.KeyPressedDuringUpdates);
        }

        [Theory]
        [InlineData(FeatureLifecycleStage.Initialise)]
        [InlineData(FeatureLifecycleStage.Update)]
        [InlineData(FeatureLifecycleStage.Render)]
        [InlineData(FeatureLifecycleStage.Unload)]
        public void Throwing_feature_is_disabled_without_stopping_healthy_features(
            FeatureLifecycleStage throwingStage)
        {
            var calls = new List<string>();
            var throwing = new FakeFeature("throwing", calls) { ThrowAt = throwingStage };
            var healthy = new FakeFeature("healthy", calls);
            var logs = new CollectingLogSink();
            var runtime = CreateRuntime(logs, throwing, healthy);

            runtime.Initialise();
            runtime.Update(InputSnapshot.Empty);
            runtime.Update(InputSnapshot.Empty);
            runtime.Render();
            runtime.Render();
            runtime.Unload();

            FeatureStatus failed = Assert.Single(runtime.Features, status => status.Id == "throwing");
            Assert.False(failed.IsEnabled);
            Assert.Equal(throwingStage, failed.Failure.Stage);
            Assert.Contains("healthy:update", calls);
            Assert.Contains("healthy:render", calls);
            Assert.Contains("healthy:unload", calls);
            Assert.Equal(2, calls.Count(call => call == "healthy:update"));
            Assert.Equal(2, calls.Count(call => call == "healthy:render"));

            FeatureLogEntry log = Assert.Single(logs.Entries);
            Assert.Equal(FeatureLogLevel.Error, log.Level);
            Assert.Equal("throwing", log.FeatureId);
            Assert.Equal(throwingStage, failed.Failure.Stage);
            Assert.IsType<InvalidOperationException>(log.Exception);

            Assert.Equal(1, calls.Count(call => call == "throwing:" +
                throwingStage.ToString().ToLowerInvariant()));
            Assert.Contains("throwing:unload", calls);
            Assert.True(
                calls.IndexOf("throwing:unload") < calls.IndexOf("healthy:unload"),
                "A later healthy feature should still unload after an earlier unload failure.");
        }

        [Fact]
        public void Throwing_toggle_disables_only_its_feature()
        {
            var key = new InputKey("F6");
            var calls = new List<string>();
            var throwing = new FakeFeature("throwing", calls);
            var healthy = new FakeFeature("healthy", calls);
            var runtime = CreateRuntime(new CollectingLogSink(), throwing, healthy);
            throwing.OnInitialise = context => context.KeyBindings.RegisterToggle(
                key,
                () => throw new InvalidOperationException("toggle failed"));

            runtime.Initialise();
            runtime.Update(new InputSnapshot(new[] { key }));
            runtime.Update(InputSnapshot.Empty);

            FeatureStatus failed = Assert.Single(runtime.Features, status => status.Id == "throwing");
            Assert.False(failed.IsEnabled);
            Assert.Equal(FeatureLifecycleStage.KeyBinding, failed.Failure.Stage);
            Assert.DoesNotContain("throwing:update", calls);
            Assert.Equal(2, calls.Count(call => call == "healthy:update"));
        }

        [Fact]
        public void Toggle_can_register_another_binding_without_escaping_the_feature_boundary()
        {
            var firstKey = new InputKey("F6");
            var secondKey = new InputKey("F7");
            var calls = new List<string>();
            var registering = new FakeFeature("registering", calls);
            var healthy = new FakeFeature("healthy", calls);
            var runtime = CreateRuntime(new CollectingLogSink(), registering, healthy);
            registering.OnInitialise = context => context.KeyBindings.RegisterToggle(
                firstKey,
                () => context.KeyBindings.RegisterToggle(
                    secondKey,
                    () => calls.Add("registering:second-toggle")));

            runtime.Initialise();
            runtime.Update(new InputSnapshot(new[] { firstKey }));
            runtime.Update(InputSnapshot.Empty);
            runtime.Update(new InputSnapshot(new[] { secondKey }));

            FeatureStatus status = Assert.Single(
                runtime.Features,
                feature => feature.Id == "registering");
            Assert.True(status.IsEnabled);
            Assert.Equal(3, calls.Count(call => call == "healthy:update"));
            Assert.Single(calls, call => call == "registering:second-toggle");
        }

        [Fact]
        public void Initialised_feature_is_offered_unload_after_an_update_failure()
        {
            var calls = new List<string>();
            var feature = new FakeFeature("feature", calls)
            {
                ThrowAt = FeatureLifecycleStage.Update
            };
            var runtime = CreateRuntime(new CollectingLogSink(), feature);

            runtime.Initialise();
            runtime.Update(InputSnapshot.Empty);
            runtime.Unload();

            Assert.Equal(
                new[] { "feature:initialise", "feature:update", "feature:unload" },
                calls);
        }

        [Fact]
        public void Partially_initialised_feature_is_offered_unload()
        {
            var calls = new List<string>();
            var feature = new FakeFeature("feature", calls);
            feature.OnInitialise = context =>
                throw new InvalidOperationException("failed after subscribing");
            var runtime = CreateRuntime(new CollectingLogSink(), feature);

            runtime.Initialise();
            runtime.Unload();

            FeatureStatus status = Assert.Single(runtime.Features);
            Assert.False(status.IsInitialised);
            Assert.False(status.IsEnabled);
            Assert.Equal(
                new[] { "feature:initialise", "feature:unload" },
                calls);
        }

        [Fact]
        public void Registry_uses_host_owned_id_for_failure_containment()
        {
            var feature = new FakeFeature("implementation-name", new List<string>())
            {
                ThrowAt = FeatureLifecycleStage.Initialise
            };
            var registry = new FirstPartyFeatureRegistry(
                new FirstPartyFeature("stable", feature));
            var runtime = new FeatureRuntime(
                registry,
                new CollectingLogSink(),
                new NullRenderer());

            runtime.Initialise();

            FeatureStatus status = Assert.Single(runtime.Features);
            Assert.Equal("stable", status.Id);
            Assert.False(status.IsEnabled);
        }

        [Fact]
        public void Feature_logging_is_tagged_with_its_registered_id()
        {
            var logs = new CollectingLogSink();
            var feature = new FakeFeature("feature", new List<string>());
            feature.OnInitialise = context => context.Log.Info("ready");
            var runtime = CreateRuntime(logs, feature);

            runtime.Initialise();

            FeatureLogEntry entry = Assert.Single(logs.Entries);
            Assert.Equal("feature", entry.FeatureId);
            Assert.Equal(FeatureLogLevel.Info, entry.Level);
            Assert.Equal("ready", entry.Message);
        }

        private static FeatureRuntime CreateRuntime(
            IFeatureLogSink logSink,
            params FakeFeature[] features)
        {
            return new FeatureRuntime(
                new FirstPartyFeatureRegistry(
                    features
                        .Select(feature => new FirstPartyFeature(
                            feature.Id,
                            feature))
                        .ToArray()),
                logSink,
                new NullRenderer());
        }

        private sealed class FakeFeature : IFeature
        {
            private readonly IList<string> calls;
            private IFeatureContext context;

            public FakeFeature(string id, IList<string> calls)
            {
                Id = id;
                this.calls = calls;
            }

            public string Id { get; }

            public FeatureLifecycleStage? ThrowAt { get; set; }

            public Action<IFeatureContext> OnInitialise { get; set; }

            public IList<bool> KeyPressedDuringUpdates { get; } = new List<bool>();

            public void Initialise(IFeatureContext featureContext)
            {
                context = featureContext;
                Record(FeatureLifecycleStage.Initialise);
                OnInitialise?.Invoke(featureContext);
            }

            public void Update()
            {
                Record(FeatureLifecycleStage.Update);
                KeyPressedDuringUpdates.Add(context.Input.WasKeyPressed(new InputKey("F6")));
            }

            public void Render()
            {
                Record(FeatureLifecycleStage.Render);
            }

            public void Unload()
            {
                Record(FeatureLifecycleStage.Unload);
            }

            private void Record(FeatureLifecycleStage stage)
            {
                calls.Add(Id + ":" + stage.ToString().ToLowerInvariant());
                if (ThrowAt == stage)
                {
                    throw new InvalidOperationException(stage + " failed");
                }
            }
        }

        private sealed class CollectingLogSink : IFeatureLogSink
        {
            public IList<FeatureLogEntry> Entries { get; } = new List<FeatureLogEntry>();

            public void Write(FeatureLogEntry entry)
            {
                Entries.Add(entry);
            }
        }

        private sealed class NullRenderer : IFeatureRenderer
        {
            public IDisposable PushClip(RenderRectangle bounds)
            {
                return NullDisposable.Instance;
            }

            public void DrawText(string text, InputPoint position, RenderColour colour)
            {
            }

            public void DrawTerrariaIcon(
                TerrariaIcon icon,
                RenderRectangle destination,
                RenderColour colour)
            {
            }

            private sealed class NullDisposable : IDisposable
            {
                public static NullDisposable Instance { get; } = new NullDisposable();

                public void Dispose()
                {
                }
            }
        }
    }
}
