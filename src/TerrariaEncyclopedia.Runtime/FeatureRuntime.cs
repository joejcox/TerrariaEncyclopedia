using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrariaEncyclopedia.Runtime
{
    public sealed class FeatureRuntime
    {
        private readonly IFeatureLogSink logSink;
        private readonly FeatureInput input = new FeatureInput();
        private readonly FeatureRegistration[] registrations;
        private bool initialised;
        private bool unloaded;

        public FeatureRuntime(
            FirstPartyFeatureRegistry registry,
            IFeatureLogSink logSink,
            IFeatureRenderer renderer)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            this.logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
            if (renderer == null)
            {
                throw new ArgumentNullException(nameof(renderer));
            }

            registrations = registry.Features
                .Select(feature => new FeatureRegistration(
                    feature.Id,
                    feature.Implementation,
                    input,
                    renderer,
                    this.logSink))
                .ToArray();
        }

        public IReadOnlyList<FeatureStatus> Features => registrations
            .Select(registration => registration.Status)
            .ToArray();

        public void Initialise()
        {
            if (initialised || unloaded)
            {
                return;
            }

            initialised = true;
            foreach (FeatureRegistration registration in registrations)
            {
                registration.InitialisationStarted = true;
                TryInvoke(registration, FeatureLifecycleStage.Initialise, () =>
                {
                    registration.Feature.Initialise(registration.Context);
                    registration.Status.IsInitialised = true;
                });
            }
        }

        public void Update(InputSnapshot snapshot)
        {
            if (!initialised || unloaded)
            {
                return;
            }

            input.Advance(snapshot ?? throw new ArgumentNullException(nameof(snapshot)));

            foreach (FeatureRegistration registration in registrations)
            {
                if (registration.Status.IsEnabled)
                {
                    registration.DispatchKeyBindings(
                        action => TryInvoke(registration, FeatureLifecycleStage.KeyBinding, action));
                }
            }

            foreach (FeatureRegistration registration in registrations)
            {
                TryInvoke(registration, FeatureLifecycleStage.Update, registration.Feature.Update);
            }
        }

        public void Render()
        {
            if (!initialised || unloaded)
            {
                return;
            }

            foreach (FeatureRegistration registration in registrations)
            {
                TryInvoke(registration, FeatureLifecycleStage.Render, registration.Feature.Render);
            }
        }

        public void Unload()
        {
            if (!initialised || unloaded)
            {
                return;
            }

            unloaded = true;
            foreach (FeatureRegistration registration in registrations)
            {
                if (registration.InitialisationStarted)
                {
                    TryInvoke(
                        registration,
                        FeatureLifecycleStage.Unload,
                        registration.Feature.Unload,
                        invokeWhenDisabled: true);
                }
            }
        }

        private void TryInvoke(
            FeatureRegistration registration,
            FeatureLifecycleStage stage,
            Action action,
            bool invokeWhenDisabled = false)
        {
            if (!registration.Status.IsEnabled && !invokeWhenDisabled)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                registration.Status.IsEnabled = false;
                if (registration.Status.Failure == null)
                {
                    registration.Status.Failure = new FeatureFailure(
                        registration.Status.Id,
                        stage,
                        exception);
                }

                try
                {
                    logSink.Write(new FeatureLogEntry(
                        FeatureLogLevel.Error,
                        registration.Status.Id,
                        "Feature disabled after an exception during " + stage + ".",
                        exception));
                }
                catch
                {
                    // Logging must never turn a contained feature failure into a game failure.
                }
            }
        }

        private sealed class FeatureRegistration
        {
            private readonly FeatureInput input;
            private readonly List<KeyBinding> keyBindings = new List<KeyBinding>();

            public FeatureRegistration(
                string id,
                IFeature feature,
                FeatureInput input,
                IFeatureRenderer renderer,
                IFeatureLogSink logSink)
            {
                Feature = feature;
                this.input = input;
                Status = new FeatureStatus(id);
                Context = new FeatureContext(
                    new BoundFeatureLogger(id, logSink),
                    new BoundKeyBindings(keyBindings),
                    input,
                    renderer);
            }

            public IFeature Feature { get; }

            public FeatureStatus Status { get; }

            public bool InitialisationStarted { get; set; }

            public IFeatureContext Context { get; }

            public void DispatchKeyBindings(Action<Action> invoke)
            {
                int bindingCount = keyBindings.Count;
                for (int index = 0; index < bindingCount; index++)
                {
                    if (!Status.IsEnabled)
                    {
                        break;
                    }

                    KeyBinding binding = keyBindings[index];
                    if (input.WasKeyPressed(binding.Key))
                    {
                        invoke(binding.Action);
                    }
                }
            }

            private sealed class BoundKeyBindings : IKeyBindings
            {
                private readonly List<KeyBinding> bindings;

                public BoundKeyBindings(List<KeyBinding> bindings)
                {
                    this.bindings = bindings;
                }

                public void RegisterToggle(InputKey key, Action action)
                {
                    if (action == null)
                    {
                        throw new ArgumentNullException(nameof(action));
                    }

                    bindings.Add(new KeyBinding(key, action));
                }
            }

            private sealed class KeyBinding
            {
                public KeyBinding(InputKey key, Action action)
                {
                    Key = key;
                    Action = action;
                }

                public InputKey Key { get; }

                public Action Action { get; }
            }
        }

        private sealed class FeatureContext : IFeatureContext
        {
            public FeatureContext(
                IFeatureLogger log,
                IKeyBindings keyBindings,
                IFeatureInput input,
                IFeatureRenderer renderer)
            {
                Log = log;
                KeyBindings = keyBindings;
                Input = input;
                Renderer = renderer;
            }

            public IFeatureLogger Log { get; }

            public IKeyBindings KeyBindings { get; }

            public IFeatureInput Input { get; }

            public IFeatureRenderer Renderer { get; }
        }

        private sealed class BoundFeatureLogger : IFeatureLogger
        {
            private readonly string featureId;
            private readonly IFeatureLogSink sink;

            public BoundFeatureLogger(string featureId, IFeatureLogSink sink)
            {
                this.featureId = featureId;
                this.sink = sink;
            }

            public void Debug(string message)
            {
                Write(FeatureLogLevel.Debug, message, null);
            }

            public void Info(string message)
            {
                Write(FeatureLogLevel.Info, message, null);
            }

            public void Warning(string message)
            {
                Write(FeatureLogLevel.Warning, message, null);
            }

            public void Error(string message, Exception exception)
            {
                Write(FeatureLogLevel.Error, message, exception);
            }

            private void Write(FeatureLogLevel level, string message, Exception exception)
            {
                sink.Write(new FeatureLogEntry(level, featureId, message, exception));
            }
        }
    }
}
