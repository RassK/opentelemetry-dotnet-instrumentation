using System;
using System.Diagnostics;

namespace Datadog.Trace
{
    /// <summary>
    /// EventArgs for a Span
    /// </summary>
    internal class SpanEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpanEventArgs"/> class.
        /// Creates a new <see cref="SpanEventArgs"/> using <paramref name="span"/>
        /// </summary>
        /// <param name="span">The <see cref="Span"/> used to initialize the <see cref="SpanEventArgs"/> object.</param>
        public SpanEventArgs(Span span) => Span = span;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanEventArgs"/> class.
        /// Creates a new <see cref="SpanEventArgs"/> using <paramref name="activity"/>
        /// </summary>
        /// <param name="activity">The <see cref="Activity"/> used to initialize the <see cref="SpanEventArgs"/> object.</param>
        public SpanEventArgs(Activity activity)
        {
            Activity = activity;
            IsActivityBased = true;
        }

        internal Span Span { get; }

        internal Activity Activity { get; }

        internal bool IsActivityBased { get; }
    }
}
