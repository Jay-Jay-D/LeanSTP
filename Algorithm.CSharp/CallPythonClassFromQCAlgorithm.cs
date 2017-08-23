using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Python.Runtime;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    class CallPythonClassFromQCAlgorithm : QCAlgorithm
    {
        private Symbol _spy = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        private dynamic _pd;
        private dynamic _np;
       

        public override void Initialize()
        {
            SetStartDate(2013, 10, 07);  //Set Start Date		
            SetEndDate(2013, 10, 11);    //Set End Date		
            SetCash(100000);             //Set Strategy Cash		
                                         // Find more symbols here: http://quantconnect.com/data		
            AddEquity("SPY", Resolution.Second);

            using (Py.GIL())
            {
                _pd = Py.Import("pandas");
                _np = Py.Import("numpy");
                dynamic rnd = _np.random.normal(0, 1, 100);
            }

            


        }

        public override void OnData(Slice data)
        {
            if (!Portfolio.Invested)
            {
                SetHoldings(_spy, 1);
            }
        }
    }
}
