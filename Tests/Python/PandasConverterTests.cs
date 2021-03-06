﻿/*
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
 *
*/

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Python;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture, Ignore]
    public class PandasConverterTests
    {
        [Test]
        public void HandlesEmptyEnumerable()
        {
            var converter = new PandasConverter();
            var rawBars = Enumerable.Empty<TradeBar>().ToArray();

            // GetDataFrame with argument of type IEnumerable<TradeBar> 
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsTrue(dataFrame.empty.AsManagedObject(typeof(bool)));
            }

            // GetDataFrame with argument of type IEnumerable<TradeBar> 
            var history = GetHistory(Symbols.SPY, Resolution.Minute, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsTrue(dataFrame.empty.AsManagedObject(typeof(bool)));
            }
        }

        [Test]
        public void HandlesTradeBars()
        {
            var converter = new PandasConverter();
            var symbol = Symbols.SPY;

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i => new TradeBar(DateTime.UtcNow.AddMinutes(i), symbol, i + 101m, i + 102m, i + 100m, i + 101m, 0m))
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<TradeBar> 
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].close.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Close, close);
                }
            }

            // GetDataFrame with argument of type IEnumerable<TradeBar> 
            var history = GetHistory(symbol, Resolution.Minute, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].close.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Close, close);
                }
            }
        }

        [Test]
        public void HandlesQuoteBars()
        {
            var converter = new PandasConverter();
            var symbol = Symbols.EURUSD;

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i => new QuoteBar(DateTime.UtcNow.AddMinutes(i), symbol, new Bar(i + 1.01m, i + 1.02m, i + 1.00m, i + 1.01m), 0m, new Bar(i + 1.01m, i + 1.02m, i + 1.00m, i + 1.01m), 0m))
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<QuoteBar> 
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].close.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Close, close);
                }
            }

            // GetDataFrame with argument of type IEnumerable<QuoteBar> 
            var history = GetHistory(symbol, Resolution.Minute, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var close = subDataFrame.loc[index].askclose.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Ask.Close, close);
                }
            }
        }

        [Test]
        public void HandlesCustomDataBars()
        {
            var converter = new PandasConverter();
            var symbol = Symbols.LTCUSD;

            var config = GetSubscriptionDataConfig<Quandl>(symbol, Resolution.Daily);
            var quandl = new Quandl();
            quandl.Reader(config, "date,open,high,low,close,settle", DateTime.UtcNow, false);

            var rawBars = Enumerable
                .Range(0, 10)
                .Select(i =>
                {
                    var line = $"{DateTime.UtcNow.AddDays(i).ToString("yyyy-MM-dd")},{i + 101},{i + 102},{i + 100},{i + 101},{i + 101}";
                    return quandl.Reader(config, line, DateTime.UtcNow.AddDays(i), false);
                })
                .ToArray();

            // GetDataFrame with argument of type IEnumerable<BaseData> 
            dynamic dataFrame = converter.GetDataFrame(rawBars);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].value.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Value, value);
                    var settle = subDataFrame.loc[index].settle.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(((DynamicData)rawBars[i]).GetProperty("settle"), settle);
                }
            }

            // GetDataFrame with argument of type IEnumerable<BaseData> 
            var history = GetHistory(symbol, Resolution.Daily, rawBars);
            dataFrame = converter.GetDataFrame(history);

            using (Py.GIL())
            {
                Assert.IsFalse(dataFrame.empty.AsManagedObject(typeof(bool)));

                var subDataFrame = dataFrame.loc[symbol];
                Assert.IsFalse(subDataFrame.empty.AsManagedObject(typeof(bool)));

                var count = subDataFrame.__len__().AsManagedObject(typeof(int));
                Assert.AreEqual(count, 10);

                for (var i = 0; i < count; i++)
                {
                    var index = subDataFrame.index[i];
                    var value = subDataFrame.loc[index].value.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(rawBars[i].Value, value);
                    var settle = subDataFrame.loc[index].settle.AsManagedObject(typeof(decimal));
                    Assert.AreEqual(((DynamicData)rawBars[i]).GetProperty("settle"), settle);
                }
            }
        }

        public IEnumerable<Slice> GetHistory<T>(Symbol symbol, Resolution resolution, IEnumerable<T> data)
            where T : IBaseData
        {
            var subscriptionDataConfig = GetSubscriptionDataConfig<T>(symbol, resolution);
            var security = GetSecurity(subscriptionDataConfig);

            return data.Select(t => TimeSlice.Create(
               t.Time,
               TimeZones.Utc,
               new CashBook(),
               new List<DataFeedPacket> { new DataFeedPacket(security, subscriptionDataConfig, new List<BaseData>() { t as BaseData }) },
               new SecurityChanges(Enumerable.Empty<Security>(), Enumerable.Empty<Security>())).Slice);
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }

        private Security GetSecurity(SubscriptionDataConfig subscriptionDataConfig)
        {
            return new Security(
                SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                subscriptionDataConfig,
                new Cash(CashBook.AccountCurrency, 0, 1m),
                SymbolProperties.GetDefault(CashBook.AccountCurrency));
        }
    }
}