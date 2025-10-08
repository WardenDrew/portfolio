using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.ACCT_SESSION_ID)]
    public class AccountingSessionIdAttribute : BaseStringAttribute
    {
        [SetsRequiredMembers]
        public AccountingSessionIdAttribute(string value)
            : base(RadiusAttributeType.ACCT_SESSION_ID, value)
        {}

        private AccountingSessionIdAttribute() { }
    }
}
