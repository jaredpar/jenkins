using System;

namespace Dashboard.Azure
{
    /// <summary>
    /// This type specifically doesn't implement equality.  It's possible for two instances of this type
    /// to have different time values (minutes).  Equality is defined by the <see cref="EntityKey"/> they
    /// map to.
    /// </summary>
    public struct CounterData
    {
        public DateTimeOffset DateTime { get; }
        public string EntityWriterId { get; }
        public bool IsJenkins { get; }

        public EntityKey EntityKey => CounterUtil.GetEntityKey(this);
        public long TimeOfDayTicks => CounterUtil.GetTimeOfDayTicks(DateTime);

        public CounterData(DateTimeOffset dateTime, string entityWriterId, bool isJenkins)
        {
            DateTime = dateTime;
            EntityWriterId = entityWriterId;
            IsJenkins = isJenkins;
        }

        public CounterData(string entityWriterId, bool isJenkins) : this(DateTimeOffset.UtcNow, entityWriterId, isJenkins)
        {

        }

        public override string ToString() => CounterUtil.GetEntityKey(this).ToString();
    }
}
