using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNS
{
    public interface IDnsResourceRecord
    {
        public RawDnsResourceRecord Raw { get; }

    }
}
