namespace Babylon.Alfred.Api.Infrastructure.YahooFinance.Models
{
    public class YahooResponse
    {
        public QuoteResponse QuoteResponse { get; set; }
    }

    public class QuoteResponse
    {
        public List<YahooQuoteResult> Result { get; set; }
        public object Error { get; set; }
    }

    public class YahooQuoteResult
    {
        public string Symbol { get; set; }
        public string ShortName { get; set; }
        public string Currency { get; set; }
        
        // Price Data
        public decimal RegularMarketPrice { get; set; }
        public decimal RegularMarketPreviousClose { get; set; }
        public decimal RegularMarketChange { get; set; }
        public decimal RegularMarketChangePercent { get; set; }
        
        // Metadata
        public string QuoteType { get; set; } // EQUITY, ETF, CRYPTOCURRENCY
        public string Exchange { get; set; }
        public long RegularMarketTime { get; set; } // Unix Timestamp
    }
}
