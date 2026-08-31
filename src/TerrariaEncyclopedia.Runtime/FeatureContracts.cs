using System;

namespace TerrariaEncyclopedia.Runtime
{
    public interface IFeature
    {
        void Initialise(IFeatureContext context);

        void Update();

        void Render();

        void Unload();
    }

    public interface IFeatureContext
    {
        IFeatureLogger Log { get; }

        IKeyBindings KeyBindings { get; }

        IFeatureInput Input { get; }

        IFeatureRenderer Renderer { get; }
    }

    public interface IFeatureLogger
    {
        void Debug(string message);

        void Info(string message);

        void Warning(string message);

        void Error(string message, Exception exception);
    }

    public enum FeatureLogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public sealed class FeatureLogEntry
    {
        public FeatureLogEntry(
            FeatureLogLevel level,
            string featureId,
            string message,
            Exception exception = null)
        {
            Level = level;
            FeatureId = featureId ?? throw new ArgumentNullException(nameof(featureId));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
        }

        public FeatureLogLevel Level { get; }

        public string FeatureId { get; }

        public string Message { get; }

        public Exception Exception { get; }
    }

    public interface IFeatureLogSink
    {
        void Write(FeatureLogEntry entry);
    }

    public enum FeatureLifecycleStage
    {
        Initialise,
        KeyBinding,
        Update,
        Render,
        Unload
    }

    public sealed class FeatureFailure
    {
        public FeatureFailure(string featureId, FeatureLifecycleStage stage, Exception exception)
        {
            FeatureId = featureId ?? throw new ArgumentNullException(nameof(featureId));
            Stage = stage;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public string FeatureId { get; }

        public FeatureLifecycleStage Stage { get; }

        public Exception Exception { get; }
    }

    public sealed class FeatureStatus
    {
        internal FeatureStatus(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public bool IsInitialised { get; internal set; }

        public bool IsEnabled { get; internal set; } = true;

        public FeatureFailure Failure { get; internal set; }
    }
}
