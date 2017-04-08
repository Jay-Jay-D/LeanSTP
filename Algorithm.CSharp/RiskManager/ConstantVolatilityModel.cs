using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp.RiskManager
{
    public class ConstantVolatilityModel : IVolatilityModel
    {
        public ConstantVolatilityModel(decimal v)
        {
            this.Volatility = v;
        }

        public decimal Volatility { get; private set; }

        public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            return Enumerable.Empty<HistoryRequest>();
        }

        public void Update(Security security, BaseData data)
        { }
    }

}
