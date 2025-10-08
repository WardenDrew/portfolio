using Radius.RadiusAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Radius
{
    public class RadiusAttributeParser
    {
        public Dictionary<RadiusAttributeType, Type> Types { get; set; } = new();

        public void ScanAssemblies(params Assembly[] assemblies)
        {
            foreach (Assembly assembly in assemblies)
            {
                ScanAssembly(assembly);
            }
        }

        public void ScanAssembly(Assembly assembly)
        {
            Dictionary<RadiusAttributeType, Type> discoveredTypes = assembly.GetTypes()
                .Select(type => (type, type.GetCustomAttribute<RadiusAttributeAttribute>()))
                .Where(tuple => tuple.Item2 != null)
                .Where(tuple => tuple.type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null)
                .Select(tuple => new KeyValuePair<RadiusAttributeType, Type>(tuple.Item2!.Type, tuple.type))
                .ToDictionary();

            Types = Types.Union(discoveredTypes).ToDictionary();
        }

        public void AddDefault()
        {
            ScanAssembly(typeof(BaseRadiusAttribute).Assembly);
        }

        public IRadiusAttribute Parse(IRadiusAttribute attribute)
        {
            if (!Types.TryGetValue(attribute.Raw.Type, out Type? type))
            {
                return attribute;
            }

            IRadiusAttribute? instance = Activator.CreateInstance(type, true) as IRadiusAttribute;
            if (instance is null) return attribute;

            type.GetProperty(nameof(IRadiusAttribute.Raw))?.SetValue(instance, attribute.Raw);

            return instance;
        }
    }
}
