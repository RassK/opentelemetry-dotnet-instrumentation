using System.Diagnostics;
using System.Globalization;

namespace Datadog.Trace.Util
{
    internal static class ActivityUtils
    {
        public static TraceId GetTraceId(this Activity activity)
        {
            return TraceId.CreateFromString(activity.TraceId.ToString());
        }

        public static ulong GetSpanId(this Activity activity)
        {
            ulong.TryParse(activity.SpanId.ToHexString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong spanId);
            return spanId;
        }
    }
}
