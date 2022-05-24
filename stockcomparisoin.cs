public List<FinancialRecordDetailModel> GetCompaniesComparision(int fiscalYearId, int sectorId, int quaterId, string indicators)
        {
            List<FinancialRecordDetailModel> resultList = new List<FinancialRecordDetailModel>();
            var financialRecord = _context.FinancialRecords.Where(x => x.FiscalYearId == fiscalYearId && x.SectorId == sectorId && x.QuarterId == quaterId).FirstOrDefault();
            if (financialRecord != null)
            {
                var todayShareMarket = GetTodayPrice();
                var records = //(from frd in _context.FinancialRecordDetails
                                   financialRecord.FinancialRecordDetails.Select(x => new FinancialRecordDetailModel
                                   {
                                       Stock = x.Stock.Trim().ToUpper(),
                                       Value = x.Value,
                                       IndicatorId = x.IndicatorId,
                                       IndicatorName = x.Indicator.Name
                                   }).ToList();
                List<int> searchedIndicators = new List<int>();
                var searchedIndicatorsString = indicators.Split(',');
                foreach (var indicator in searchedIndicatorsString)
                {
                    searchedIndicators.Add(Int32.Parse(indicator));
                }

                var EPSrecords = records.Where(x => x.IndicatorName.ToUpper() == "EPS").ToList();
                var BVrecords = records.Where(x => x.IndicatorName.ToUpper() == "BV").ToList();
                //indicator loop
                foreach (var indicator in searchedIndicators)
                {
                    var indicatorRow = _context.Indicators.Where(x => x.IndicatorId == indicator).FirstOrDefault();
                    var isAsc = indicatorRow.IsAsc;
                    var weightage = indicatorRow.Weightage;
                    var indicatorWiseList = records.Where(x => x.IndicatorId == indicator).ToList();

                    if (indicatorRow.Name.ToUpper() == "LTP")
                    {

                        indicatorWiseList = (from iwl in indicatorWiseList
                                             join tsm in todayShareMarket on iwl.Stock equals tsm.Stock
                                             select new FinancialRecordDetailModel()
                                             {
                                                 Stock = iwl.Stock,
                                                 Value = Convert.ToDecimal(tsm.ClosingPrice),
                                                 IndicatorId = iwl.IndicatorId,
                                                 IndicatorName = iwl.IndicatorName
                                             }

                            ).ToList();
                    }

                    if (indicatorRow.Name.ToUpper() == "PE")
                    {
                        indicatorWiseList = (//from iwl in indicatorWiseList
                                             from eps in EPSrecords //on iwl.Stock equals eps.Stock
                                             join tsm in todayShareMarket on eps.Stock equals tsm.Stock
                                             select new FinancialRecordDetailModel()
                                             {
                                                 Stock = eps.Stock,
                                                 Value = eps.Value <= 0 ? 0 : (Convert.ToDecimal(tsm.ClosingPrice) / (eps.Value > 1 ? eps.Value : Math.Ceiling(eps.Value))),
                                                 IndicatorId = indicatorRow.IndicatorId,
                                                 IndicatorName = indicatorRow.Name
                                             }

                            ).ToList();
                        decimal maxPE = indicatorWiseList.Max(x => x.Value);
                        indicatorWiseList = indicatorWiseList.Select(x =>
                        {
                            if (x.Value == 0)
                            {
                                x.Value = maxPE + maxPE / indicatorWiseList.Count;
                            }
                            return x;
                        }).ToList();
                    }

                    if (indicatorRow.Name.ToUpper() == "PBV")
                    {
                        indicatorWiseList = (//from iwl in indicatorWiseList
                                             from pbv in BVrecords //on iwl.Stock equals pbv.Stock
                                             join tsm in todayShareMarket on pbv.Stock equals tsm.Stock
                                             select new FinancialRecordDetailModel()
                                             {
                                                 Stock = pbv.Stock,
                                                 Value = pbv.Value <= 0 ? 0 : (Convert.ToDecimal(tsm.ClosingPrice) / (pbv.Value > 1 ? pbv.Value : Math.Ceiling(pbv.Value))),
                                                 IndicatorId = indicatorRow.IndicatorId,
                                                 IndicatorName = indicatorRow.Name
                                             }
                            ).ToList();
                        decimal maxPBV = indicatorWiseList.Max(x => x.Value);
                        indicatorWiseList = indicatorWiseList.Select(x =>
                        {
                            if (x.Value == 0)
                            {
                                x.Value = maxPBV + maxPBV / indicatorWiseList.Count;
                            }
                            return x;
                        }).ToList();
                    }

                    if (indicatorWiseList.Count == 0)
                    {
                        continue;
                    }

                    //Z-Score calculation
                    decimal meanValue = indicatorWiseList.Average(x => x.Value);
                    decimal standardDeviation = CalculateStandardDeviation(indicatorWiseList.Select(x => x.Value).ToList());
                    foreach (var item in indicatorWiseList)
                    {
                        item.ZScore = (item.Value - meanValue) / standardDeviation;
                    }

                    // for smoothness calculate range

                    decimal maxValue = indicatorWiseList.Max(x => x.ZScore);
                    decimal minValue = indicatorWiseList.Min(x => x.ZScore);
                    decimal rangeHeight = (maxValue - minValue) / 8;// indicatorWiseList.Count();

                    var rangeList = new List<decimal>();
                    if (isAsc)
                    {
                        rangeList.Add(-3);
                        rangeList.Add(-Convert.ToDecimal(2.5));
                        rangeList.Add(-2);
                        rangeList.Add(-Convert.ToDecimal(1.5));
                        rangeList.Add(-1);
                        rangeList.Add(-Convert.ToDecimal(0.5));
                        rangeList.Add(0);
                        rangeList.Add(Convert.ToDecimal(0.5));
                        rangeList.Add(1);
                        rangeList.Add(Convert.ToDecimal(1.5));
                        rangeList.Add(2);
                        rangeList.Add(Convert.ToDecimal(2.5));
                        rangeList.Add(3);
                        //}
                    }
                    else
                    {
                        rangeList.Add(3);
                        rangeList.Add(Convert.ToDecimal(2.5));
                        rangeList.Add(2);
                        rangeList.Add(Convert.ToDecimal(1.5));
                        rangeList.Add(1);
                        rangeList.Add(Convert.ToDecimal(0.5));
                        rangeList.Add(0);
                        rangeList.Add(-Convert.ToDecimal(0.5));
                        rangeList.Add(-1);
                        rangeList.Add(-Convert.ToDecimal(1.5));
                        rangeList.Add(-2);
                        rangeList.Add(-Convert.ToDecimal(2.5));
                        rangeList.Add(-3);
                        //}
                    }

                    foreach (var item in indicatorWiseList.OrderBy(x => x.ZScore))
                    {
                        IndicatorValueModel ivModel = new IndicatorValueModel();
                        ivModel.IndicatorName = item.IndicatorName;
                        ivModel.Value = indicatorWiseList.Where(x => x.Indicator == item.Indicator && x.Stock == item.Stock).FirstOrDefault().Value;

                        //check if this stock is already listed
                        bool isAlreadListed = CheckAlreadyConsistStock(item.Stock, resultList);

                        var sharepriceModel = todayShareMarket.Where(x => x.Stock.ToUpper() == item.Stock.ToUpper()).FirstOrDefault();
                        string shareClosingPrice = "";
                        if (sharepriceModel != null)
                        {
                            shareClosingPrice = sharepriceModel.ClosingPrice;
                        }
                        else
                        {
                            shareClosingPrice = GetLastPriceofStock(item.Stock);
                        }

                        for (int i = 0; i < rangeList.Count; i++)
                        {
                            if (isAsc)
                            {
                                //the value is out of range 3 and -3 (97% of data according to defn of z-score)
                                if (Decimal.Round(item.ZScore, 2) > rangeList[rangeList.Count - 1])
                                {
                                    if (isAlreadListed)
                                    {
                                        var stock = resultList.Where(x => x.Stock == item.Stock).FirstOrDefault();
                                        var oldValue = stock.Value;
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().Value = oldValue + (rangeList.Count * weightage);
                                    }
                                    else
                                    {
                                        resultList.Add(new FinancialRecordDetailModel
                                        {
                                            Stock = item.Stock,
                                            Value = rangeList.Count * weightage,
                                            LTP = shareClosingPrice
                                        });
                                    }
                                    if (item.IndicatorName.ToUpper() != "LTP")
                                    {
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().IndicatorValueList.Add(ivModel);
                                    }
                                    break;
                                }
                                //if value is in range 3 and -3
                                else if (Decimal.Round(item.ZScore, 2) <= rangeList[i])
                                {
                                    if (isAlreadListed)
                                    {
                                        var stock = resultList.Where(x => x.Stock == item.Stock).FirstOrDefault();
                                        var oldValue = stock.Value;
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().Value = oldValue + ((i + 1) * weightage);
                                    }
                                    else
                                    {
                                        resultList.Add(new FinancialRecordDetailModel
                                        {
                                            Stock = item.Stock,
                                            Value = (i + 1) * weightage,
                                            LTP = shareClosingPrice
                                        });
                                    }
                                    if (item.IndicatorName.ToUpper() != "LTP")
                                    {
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().IndicatorValueList.Add(ivModel);
                                    }
                                    break;
                                }
                            }
                            else
                            {
                                //the value is out of range 3 and -3 (97% of data according to defn of z-score)
                                if (Decimal.Round(item.ZScore, 2) < rangeList[rangeList.Count - 1])
                                {
                                    if (isAlreadListed)
                                    {
                                        var stock = resultList.Where(x => x.Stock == item.Stock).FirstOrDefault();
                                        var oldValue = stock.Value;
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().Value = oldValue + (rangeList.Count * weightage);
                                    }
                                    else
                                    {
                                        resultList.Add(new FinancialRecordDetailModel
                                        {
                                            Stock = item.Stock,
                                            Value = rangeList.Count * weightage,
                                            LTP = shareClosingPrice
                                        });
                                    }
                                    if (item.IndicatorName.ToUpper() != "LTP")
                                    {
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().IndicatorValueList.Add(ivModel);
                                    }
                                    break;
                                }
                                //if value is in range 3 and -3
                                else if (decimal.Round(item.ZScore, 2) >= rangeList[i])
                                {
                                    if (isAlreadListed)
                                    {
                                        var stock = resultList.Where(x => x.Stock == item.Stock).FirstOrDefault();
                                        var oldValue = stock.Value;
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().Value = oldValue + ((i + 1) * weightage);
                                    }
                                    else
                                    {
                                        resultList.Add(new FinancialRecordDetailModel
                                        {
                                            Stock = item.Stock,
                                            Value = (i + 1) * weightage,
                                            LTP = shareClosingPrice,
                                        }); ;


                                    }

                                    if (item.IndicatorName.ToUpper() != "LTP")
                                    {
                                        resultList.Where(x => x.Stock == item.Stock).FirstOrDefault().IndicatorValueList.Add(ivModel);
                                    }
                                    break;
                                }
                            }


                        }
                    }

                }
            }

            var finalResult = resultList.OrderBy(x => x.Value).ToList();//.ThenBy(x=>x.LTP).ToList();
            List<ListedCompanyModel> companies = _context.ListedCompanies.Select(x => new ListedCompanyModel() { FullName = x.FullName, Symbol = x.Symbol }).ToList();
            finalResult.Select(c => { c.StockFullName = companies.Where(x => x.Symbol == c.Stock).FirstOrDefault().FullName; return c; }).ToList();
            return finalResult;
        }

        private static decimal CalculateStandardDeviation(List<decimal> sequence)
        {
            decimal result = 0;

            if (sequence.Any())
            {
                decimal average = sequence.Average();
                decimal sum = sequence.Sum(d => Convert.ToDecimal(Math.Pow(Convert.ToDouble(d - average), 2)));
                result = Convert.ToDecimal(Math.Sqrt(Convert.ToDouble((sum) / (sequence.Count() - 1))));
            }
            return result;
        }
        private bool CheckAlreadyConsistStock(string stock, List<FinancialRecordDetailModel> resultList)
        {
            bool alreadyConsist = false;
            foreach (var item in resultList)
            {

                if (item.Stock == stock)
                {
                    alreadyConsist = true;
                }

            }
            return alreadyConsist;
        }
