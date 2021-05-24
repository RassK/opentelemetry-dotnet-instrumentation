using System;
using System.Diagnostics;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    internal abstract class ScopeManagerBase : IScopeManager, IScopeRawAccess
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(ScopeManagerBase));

        public event EventHandler<SpanEventArgs> TraceStarted;

        public event EventHandler<SpanEventArgs> SpanOpened;

        public event EventHandler<SpanEventArgs> SpanActivated;

        public event EventHandler<SpanEventArgs> SpanDeactivated;

        public event EventHandler<SpanEventArgs> SpanClosed;

        public event EventHandler<SpanEventArgs> TraceEnded;

        public abstract Scope Active { get; protected set; }

        Scope IScopeRawAccess.Active
        {
            get => Active;
            set => Active = value;
        }

        public Scope Activate(Span span, bool finishOnClose)
        {
            var newParent = Active;
            var scope = new Scope(newParent, span, this, finishOnClose);
            var scopeOpenedArgs = new SpanEventArgs(span);

            return Activate(scope, newParent, scopeOpenedArgs);
        }

        public Scope Activate(Activity activity, bool finishOnClose)
        {
            var newParent = Active;

            var scope = new Scope(newParent, activity, this, finishOnClose);
            var scopeOpenedArgs = new SpanEventArgs(activity);

            return Activate(scope, newParent, scopeOpenedArgs);
        }

        public void Close(Scope scope)
        {
            var current = Active;
            var isRootSpan = scope.Parent == null;

            if (current == null || current != scope)
            {
                // This is not the current scope for this context, bail out
                SpanClosed?.Invoke(this, CreateEventArgs(scope));
                return;
            }

            // if the scope that was just closed was the active scope,
            // set its parent as the new active scope
            Active = scope.Parent;
            SpanDeactivated?.Invoke(this, CreateEventArgs(scope));

            if (!isRootSpan)
            {
                SpanActivated?.Invoke(this, CreateEventArgs(scope.Parent));
            }

            SpanClosed?.Invoke(this, CreateEventArgs(scope));

            if (isRootSpan)
            {
                TraceEnded?.Invoke(this, CreateEventArgs(scope));
            }
        }

        private Scope Activate(Scope scope, Scope newParent, SpanEventArgs scopeOpenedArgs)
        {
            if (newParent == null)
            {
                TraceStarted?.Invoke(this, scopeOpenedArgs);
            }

            SpanOpened?.Invoke(this, scopeOpenedArgs);

            Active = scope;

            if (newParent != null)
            {
                SpanDeactivated?.Invoke(this, CreateEventArgs(newParent));
            }

            SpanActivated?.Invoke(this, scopeOpenedArgs);

            return scope;
        }

        private SpanEventArgs CreateEventArgs(Scope scope)
        {
            return scope.IsActivityBased
                ? new SpanEventArgs(scope.Activity)
                : new SpanEventArgs(scope.Span);
        }
    }
}
