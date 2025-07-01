# Class: QuantLib::EscrowedDividendAdjustment

## Member Variables
- `private const  dividendSchedule_`: 
- `private const  rTS_`: 
- `private const  qTS_`: 
- `private const std::function<  toTime_`: 
- `private const  maturity_`: 

## Functions
### public None EscrowedDividendAdjustment(DividendSchedule dividendSchedule, Handle< YieldTermStructure > rTS, Handle< YieldTermStructure > qTS, std::function< Real(Date)> toTime, Time maturity)

#### Parameters:
- `None dividendSchedule`: 
- `None rTS`: 
- `None qTS`: 
- `std::function<  toTime`: 
- `None maturity`: 

### public None dividendAdjustment(Time t) const

#### Parameters:
- `None t`: 

### public const  riskFreeRate() const


### public const  dividendYield() const


