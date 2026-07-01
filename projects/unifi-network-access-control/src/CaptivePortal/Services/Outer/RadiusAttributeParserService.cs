using Radius;

namespace CaptivePortal.Services.Outer
{
    public class RadiusAttributeParserService
    {
        public RadiusAttributeParser Parser { get; set; } = new();

        public RadiusAttributeParserService()
        {
            Parser.AddDefault();
        }
    }
}
