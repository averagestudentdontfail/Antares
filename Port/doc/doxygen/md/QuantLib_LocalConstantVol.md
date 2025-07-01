# Class: QuantLib::LocalConstantVol

## Brief Description
Constant local volatility, no time-strike dependence. 

## Detailed Description
This class implements the LocalVolatilityTermStructure interface for a constant local volatility (no time/asset dependence). Local volatility and Black volatility are the same when volatility is at most time dependent, so this class is basically a proxy for 

## Inheritance
- Inherits from: QuantLib::LocalVolTermStructure

## Functions
### public None LocalConstantVol(const Date &referenceDate, Volatility volatility, DayCounter dayCounter)

#### Parameters:
- `const  referenceDate`: 
- `None volatility`: 
- `None dayCounter`: 

### public None LocalConstantVol(const Date &referenceDate, Handle< Quote > volatility, DayCounter dayCounter)

#### Parameters:
- `const  referenceDate`: 
- `None volatility`: 
- `None dayCounter`: 

### public None LocalConstantVol(Natural settlementDays, const Calendar &, Volatility volatility, DayCounter dayCounter)

#### Parameters:
- `None settlementDays`: 
- `const  `: 
- `None volatility`: 
- `None dayCounter`: 

### public None LocalConstantVol(Natural settlementDays, const Calendar &, Handle< Quote > volatility, DayCounter dayCounter)

#### Parameters:
- `None settlementDays`: 
- `const  `: 
- `None volatility`: 
- `None dayCounter`: 

