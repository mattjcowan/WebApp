namespace WebApp.Actions
{
    public static class Priorities
    {
        internal const int DbAction = int.MinValue;
        internal const int ApiActions = -2;
        internal const int AppActions = -1;
        internal const int AspNetStartupActions = 0;     
        internal const int ServiceStackAction = 10;   
        internal const int FallbackServiceAction = int.MaxValue;
    }
}