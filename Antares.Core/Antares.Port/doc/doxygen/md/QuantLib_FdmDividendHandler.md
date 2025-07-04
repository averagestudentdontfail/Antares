# Class: QuantLib::FdmDividendHandler

## Inheritance
- Inherits from: QuantLib::StepCondition< Array >

## Member Variables
- `private None x_`: 
- `private std::vector<  dividendTimes_`: 
- `private std::vector<  dividendDates_`: 
- `private std::vector<  dividends_`: 
- `private const ext::shared_ptr<  mesher_`: 
- `private const  equityDirection_`: 

## Functions
### public None FdmDividendHandler(const DividendSchedule &schedule, const ext::shared_ptr< FdmMesher > &mesher, const Date &referenceDate, const DayCounter &dayCounter, Size equityDirection)

#### Parameters:
- `const  schedule`: 
- `const ext::shared_ptr<  mesher`: 
- `const  referenceDate`: 
- `const  dayCounter`: 
- `None equityDirection`: 

### public void applyTo(Array &a, Time t) const override

#### Parameters:
- `None a`: 
- `None t`: 

### public const std::vector<  dividendTimes() const


### public const std::vector<  dividendDates() const


### public const std::vector<  dividends() const


