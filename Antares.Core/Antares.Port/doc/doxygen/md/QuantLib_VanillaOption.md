# Class: QuantLib::VanillaOption

## Brief Description
Vanilla option (no discrete dividends, no barriers) on a single asset. 

## Inheritance
- Inherits from: QuantLib::OneAssetOption

## Functions
### public None VanillaOption(const ext::shared_ptr< StrikedTypePayoff > &, const ext::shared_ptr< Exercise > &)

#### Parameters:
- `const ext::shared_ptr<  `: 
- `const ext::shared_ptr<  `: 

### public None impliedVolatility(Real price, const ext::shared_ptr< GeneralizedBlackScholesProcess > &process, Real accuracy=1.0e-4, Size maxEvaluations=100, Volatility minVol=1.0e-7, Volatility maxVol=4.0) const

#### Parameters:
- `None price`: 
- `const ext::shared_ptr<  process`: 
- `None accuracy`: 
- `None maxEvaluations`: 
- `None minVol`: 
- `None maxVol`: 

### public None impliedVolatility(Real price, const ext::shared_ptr< GeneralizedBlackScholesProcess > &process, const DividendSchedule &dividends, Real accuracy=1.0e-4, Size maxEvaluations=100, Volatility minVol=1.0e-7, Volatility maxVol=4.0) const

#### Parameters:
- `None price`: 
- `const ext::shared_ptr<  process`: 
- `const  dividends`: 
- `None accuracy`: 
- `None maxEvaluations`: 
- `None minVol`: 
- `None maxVol`: 

