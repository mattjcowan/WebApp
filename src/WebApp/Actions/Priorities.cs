namespace WebApp.Actions
{
  public static class Priorities
  {
    internal const int LicenseAction = int.MinValue;
    internal const int DbAction = LicenseAction + 1;
    internal const int HeadersAction = StaticFilesAction - 1;
    internal const int ServiceWorkerAction = StaticFilesAction - 1;
    internal const int StaticFilesAction = DefaultAction - 1;
    internal const int DefaultAction = 0;
    internal const int ServiceStackAction = DefaultAction + 100;
    internal const int FallbackServiceAction = int.MaxValue;
  }
}
