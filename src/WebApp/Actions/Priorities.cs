namespace WebApp.Actions
{
    public static class Priorities
    {
        internal const int DbAction = int.MinValue;
        internal const int HeadersAction = StaticFilesAction - 1;
        internal const int ServiceWorkerAction = StaticFilesAction - 1;
        internal const int StaticFilesAction = DefaultAction - 1;
        internal const int DefaultAction = 0;
        internal const int ServiceStackAction = DefaultAction + 10;
        internal const int FallbackServiceAction = int.MaxValue;
    }
}
