# Class: QuantLib::BlackVarianceCurve

## Brief Description
Black volatility curve modelled as variance curve. 

## Detailed Description
This class calculates time-dependent Black volatilities using as input a vector of (ATM) Black volatilities observed in the market.

## Inheritance
- Inherits from: QuantLib::BlackVarianceTermStructure

## Functions
### public None BlackVarianceCurve(const Date &referenceDate, const std::vector< Date > &dates, const std::vector< Volatility > &blackVolCurve, DayCounter dayCounter, bool forceMonotoneVariance=true)

#### Parameters:
- `const  referenceDate`: 
- `const std::vector<  dates`: 
- `const std::vector<  blackVolCurve`: 
- `None dayCounter`: 
- `bool forceMonotoneVariance`: 

