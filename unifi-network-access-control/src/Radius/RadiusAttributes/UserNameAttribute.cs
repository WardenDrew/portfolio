using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.USER_NAME)]
    public class UserNameAttribute : BaseStringAttribute
    {
        [SetsRequiredMembers]
        public UserNameAttribute(string value)
            : base(RadiusAttributeType.USER_NAME, value)
        {}

        private UserNameAttribute() { }
    }
}
