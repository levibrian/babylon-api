DELETE FROM securities c;

INSERT INTO public.securities
("Ticker", "SecurityName", "LastUpdated")
VALUES

-- ETFs (using XETRA exchange tickers)
('IS3R.DE', 'iShares Edge World Momentum USD (Acc)', NOW()),
('QDVB.DE', 'iShares Edge USA Quality USD (Acc)', NOW()),
('IS3N.DE', 'iShares Core MSCI EM IMI USD (Acc)', NOW()),
('PPFB.DE', 'iShares Physical Gold ETC', NOW()),

-- Stocks (using EU exchange tickers)
('ADYEN.AS', 'Adyen', NOW()),
('NVD.DE', 'Nvidia', NOW()),
('MSF.DE', 'Microsoft', NOW()),
('APC.DE', 'Apple', NOW()),
('BY6.DE', 'BYD (H-Shares)', NOW()),
('NOV.DE', 'Novo Nordisk (ADR)', NOW()),
('AMZ.DE', 'Amazon', NOW()),
('TSFA.DE', 'TSMC (ADR)', NOW()),
('EOP.DE', 'Enphase', NOW()),
('ABEC.DE', 'Google (Alphabet Inc. Class A)', NOW()),
('ASME.DE', 'ASML', NOW()),
('3V64.DE', 'Visa', NOW()),
('49V.DE', 'Vertiv', NOW()),
('FP3.F', 'NextEra Energy', NOW()),

-- Cryptocurrencies (using EUR tickers)
('BTCEUR', 'Bitcoin', NOW()),
('SOLEUR', 'Solana', NOW()),
('ETHEUR', 'Ethereum', NOW());
