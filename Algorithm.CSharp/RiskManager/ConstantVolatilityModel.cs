using QuantConnect.Data;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    public class ConstantVolatilityModel : IVolatilityModel
    {
        private decimal volatility;

        public ConstantVolatilityModel(decimal v)
        {
            this.volatility = v;
        }

        public decimal Volatility
        {
            get
            {
                return volatility;
            }
        }

        public IEnumerable<HistoryRequest> GetHistoryRequirements(Security security, DateTime utcTime)
        {
            return Enumerable.Empty<HistoryRequest>();
        }

        public void Update(Security security, BaseData data)
        { }
    }

}
