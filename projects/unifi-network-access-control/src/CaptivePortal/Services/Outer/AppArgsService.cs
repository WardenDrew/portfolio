namespace CaptivePortal.Services.Outer
{
    public class AppArgsService
    {
        public string[] Args { get; private set; }

        public AppArgsService(string[] args)
        {
            Args = args;
        }
    }
}
