DELETE FROM companies c;

INSERT INTO public.companies
("Ticker", "CompanyName", "LastUpdated")
VALUES

-- Companies (using primary exchange tickers - mainly NASDAQ/NYSE)
('NVDA', 'Nvidia', NOW()),
('MSFT', 'Microsoft', NOW()),
('GOOGL', 'Google (Alphabet Inc. Class A)', NOW()),
('V', 'Visa', NOW()),
('AAPL', 'Apple', NOW()),
('ADYEN', 'Adyen', NOW()),
('ASML', 'ASML', NOW()),
('NVO', 'Novo Nordisk (ADR)', NOW()),
('TSM', 'TSMC (ADR)', NOW()),
('AMZN', 'Amazon', NOW()),
('BYD', 'BYD (H-Shares)', NOW()),
('ENPH', 'Enphase', NOW()),
('NEE', 'NextEra Energy', NOW()),
('VRT', 'Vertiv', NOW()),

-- ETFs (using XETRA exchange tickers)
('IS3R.DE', 'iShares Edge World Momentum USD (Acc)', NOW()),
('QDVB.DE', 'iShares Edge USA Quality USD (Acc)', NOW()),
('IS3N.DE', 'iShares Core MSCI EM IMI USD (Acc)', NOW()),
('PPFB.DE', 'iShares Physical Gold ETC', NOW()),

-- Cryptocurrencies (using common tickers)
('BTC', 'Bitcoin', NOW()),
('ETH', 'Ethereum', NOW()),
('SOL', 'Solana', NOW());
