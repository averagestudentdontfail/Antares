# Class: QuantLib::FdmQuantoHelper

## Inheritance
- Inherits from: QuantLib::Observable

## Member Variables
- `public const ext::shared_ptr<  rTS_`: 
- `public const ext::shared_ptr<  fTS_`: 
- `public const ext::shared_ptr<  fxVolTS_`: 
- `public const  equityFxCorrelation_`: 
- `public const  exchRateATMlevel_`: 

## Functions
### public None FdmQuantoHelper(ext::shared_ptr< YieldTermStructure > rTS, ext::shared_ptr< YieldTermStructure > fTS, ext::shared_ptr< BlackVolTermStructure > fxVolTS, Real equityFxCorrelation, Real exchRateATMlevel)

#### Parameters:
- `ext::shared_ptr<  rTS`: 
- `ext::shared_ptr<  fTS`: 
- `ext::shared_ptr<  fxVolTS`: 
- `None equityFxCorrelation`: 
- `None exchRateATMlevel`: 

### public None quantoAdjustment(Volatility equityVol, Time t1, Time t2) const

#### Parameters:
- `None equityVol`: 
- `None t1`: 
- `None t2`: 

### public None quantoAdjustment(const Array &equityVol, Time t1, Time t2) const

#### Parameters:
- `const  equityVol`: 
- `None t1`: 
- `None t2`: 

