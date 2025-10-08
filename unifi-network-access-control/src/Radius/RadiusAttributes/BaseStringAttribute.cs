using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    public abstract class BaseStringAttribute : BaseRadiusAttribute
    {
        public string Value
        {
            get
            {
                return Encoding.UTF8.GetString(Raw.Value);
            }

            set
            {
                Raw.Value = Encoding.UTF8.GetBytes(value);
            }
        }

        [SetsRequiredMembers]
        public BaseStringAttribute(RadiusAttributeType type, string value)
        {
            Raw = new()
            {
                Type = type,
                Value = []
            };

            this.Value = value;
        }

        protected BaseStringAttribute() { }
    }
}
