using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.LayoutRenderers.Wrappers;
using System.Globalization;
using System.Threading;

namespace MapAssist
{
    [LayoutRenderer("InvariantCulture")]
    [ThreadAgnostic]
    public sealed class InvariantCultureLayoutRendererWrapper : WrapperLayoutRendererBase
    {
        protected override string Transform(string text)
        {
            return text;
        }

        protected override string RenderInner(LogEventInfo logEvent)
        {
            var currentCulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                var value = base.RenderInner(logEvent);
                return value;
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = currentCulture;
            }
        }
    }
}
