/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using QuantConnect.Data;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template algorithm simply initializes the date range and cash
    /// </summary>
    public class ForexDataDownloader : QCAlgorithm
    {

        [Parameter("forex-broker")]
        private readonly string forexMarket = "fxcm";

        private readonly string[] forexTickers =
        {
            "USDCAD", "USDCHF", "USDJPY", "AUDUSD", "EURUSD", "GBPUSD"
        };

        public override void Initialize()
        {
            SetStartDate(2007, 1, 1);    //Set Start Date
            SetEndDate(2017, 04, 01);    //Set End Date
            SetCash(100000);             //Set Strategy Cash

            var brokerage = forexMarket == "fxcm" ? BrokerageName.FxcmBrokerage : BrokerageName.OandaBrokerage;
            SetBrokerageModel(brokerage);

            foreach (var ticker in forexTickers)
            {
                AddForex(ticker, Resolution.Minute, forexMarket);
            }

            History(1, Resolution.Daily);
            History(1, Resolution.Hour);

        }

        public override void OnEndOfDay()
        {
            Log(string.Format("======== EOD | {0} | ========", Time.ToLongDateString()));
        }
    }
}