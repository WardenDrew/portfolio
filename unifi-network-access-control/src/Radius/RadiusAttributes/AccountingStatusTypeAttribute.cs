using System.Diagnostics.CodeAnalysis;

namespace Radius.RadiusAttributes
{
    [RadiusAttribute(RadiusAttributeType.ACCT_STATUS_TYPE)]
    public class AccountingStatusTypeAttribute : BaseRadiusAttribute
    {
        public enum StatusTypes : int
        {
            START = 1,
            STOP = 2,
            INTERIM_UPDATE = 3,
            ACCOUNTING_ON = 7,
            ACCOUNTING_OFF = 8,
        }

        public StatusTypes StatusType
        {
            get
            {
                if (Raw.Value.Length != 4)
                    throw new InvalidOperationException();

                byte[] buffer = [0, 0, 0, 0];
                Array.Copy(Raw.Value, 0, buffer, 0, 4);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }

                int value = BitConverter.ToInt32(buffer, 0);

                if (!Enum.IsDefined(typeof(StatusTypes), value))
                    throw new IndexOutOfRangeException(nameof(StatusType));

                return (StatusTypes)value;
            }

            set
            {
                if (Raw.Value.Length != 4)
                    throw new InvalidOperationException();

                int intValue = (int)value;

                byte[] buffer = BitConverter.GetBytes(intValue);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(buffer);
                }

                Array.Copy(buffer, 0, Raw.Value, 0, 4);
            }
        }

        [SetsRequiredMembers]
        public AccountingStatusTypeAttribute(StatusTypes statusType)
        {
            Raw = new()
            {
                Type = RadiusAttributeType.ACCT_STATUS_TYPE,
                Value = []
            };

            this.StatusType = statusType;
        }

        private AccountingStatusTypeAttribute() { }
    }
}
