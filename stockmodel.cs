using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShareAnalysis.Models
{
public class FinancialRecordModel
    {

        public FinancialRecordModel()
        {
            FinancialRecordList = new List<FinancialRecordModel>();
            DetailList = new List<FinancialRecordDetailModel>();
        }

        public int FinancialRecordId { get; set; }

        [DisplayName("Sector")]
        public int SectorId { get; set; }

        public string SectorName { get; set; }

        [DisplayName("Fiscal Year")]
        public int FiscalYearId { get; set; }
        public int? SearchFiscalYearId { get; set; }
        public int? SearchSectorId { get; set; }
        public int? SearchQuaterId { get; set; }

        public string FiscalYearName { get; set; }
        public int Quarter { get; set; }

        public string QuarterName { get; set; }

        public int ExistedFinancialRecordId { get; set; }

        public List<FinancialRecordModel> FinancialRecordList { get; set; }

        public List<FinancialRecordDetailModel> DetailList { get; set; }
    }
    public class FinancialRecordDetailModel
    {

        public FinancialRecordDetailModel()
        {
            IndicatorValueList = new List<IndicatorValueModel>();
        }

        public int FinancialRecordId { get; set; }
        public string Stock { get; set; }
        public string StockFullName { get; set; }
        public int IndicatorId { get; set; }
        public decimal Value { get; set; }

        public string Indicator { get; set; }
        public string IndicatorName { get; set; }

        public string LTP { get; set; }

        public int Rank { get; set; }
        public decimal ZScore { get; set; }

        public List<FinancialRecordDetailModel> FinancialRecordDetailList { get; set; }

        public List<IndicatorValueModel> IndicatorValueList { get; set; }
    }

    public class IndicatorValueModel
    {
        public string IndicatorName { get; set; }

        public decimal Value { get; set; }
    }
    public class ListedCompanyModel
    {
        public string FullName { get; set; }
        public string Symbol { get; set; }
    }
    
}
