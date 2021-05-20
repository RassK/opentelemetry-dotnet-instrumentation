using System.Diagnostics;
using Datadog.Trace.Abstractions;

namespace Datadog.Trace
{
    /// <summary>
    /// A scope is a handle used to manage the concept of an active span.
    /// Meaning that at a given time at most one span is considered active and
    /// all newly created spans that are not created with the ignoreActiveSpan
    /// parameter will be automatically children of the active span.
    /// </summary>
    public class Scope : IScope
    {
        private readonly IScopeManager _scopeManager;
        private readonly bool _finishOnClose;
        private readonly bool _isActivityBased;

        internal Scope(Scope parent, Span span, IScopeManager scopeManager, bool finishOnClose)
        {
            Parent = parent;
            Span = span;
            _scopeManager = scopeManager;
            _finishOnClose = finishOnClose;
        }

        internal Scope(Scope parent, Activity activity, IScopeManager scopeManager, bool finishOnClose)
        {
            Parent = parent;
            Activity = activity;
            _scopeManager = scopeManager;
            _finishOnClose = finishOnClose;
            _isActivityBased = true;
        }

        /// <summary>
        /// Gets the active span wrapped in this scope (Obsolete)
        /// </summary>
        public Span Span { get; }

        /// <summary>
        /// Gets the active span wrapped in this scope
        /// </summary>
        public Activity Activity { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="Scope"/> is <see cref="Activity"/> based.
        /// </summary>
        public bool IsActivityBased => _isActivityBased;

        /// <summary>
        /// Gets the active span wrapped in this scope
        /// Proxy to Span without concrete return value
        /// </summary>
        ISpan IScope.Span => Span;

        internal Scope Parent { get; }

        /// <summary>
        /// Closes the current scope and makes its parent scope active
        /// </summary>
        public void Close()
        {
            _scopeManager.Close(this);

            if (_finishOnClose)
            {
                if (_isActivityBased)
                {
                    Activity.Dispose();
                }
                else
                {
                    Span.Finish();
                }
            }
        }

        /// <summary>
        /// Closes the current scope and makes its parent scope active
        /// </summary>
        public void Dispose()
        {
            try
            {
                Close();
            }
            catch
            {
                // Ignore disposal exceptions here...
                // TODO: Log? only in test/debug? How should Close() concerns be handled (i.e. independent?)
            }
        }
    }
}
