# Class: QuantLib::FdmBermudanStepCondition

## Inheritance
- Inherits from: QuantLib::StepCondition< Array >

## Member Variables
- `private std::vector<  exerciseTimes_`: 
- `private const ext::shared_ptr<  mesher_`: 
- `private const ext::shared_ptr<  calculator_`: 

## Functions
### public None FdmBermudanStepCondition(const std::vector< Date > &exerciseDates, const Date &referenceDate, const DayCounter &dayCounter, ext::shared_ptr< FdmMesher > mesher, ext::shared_ptr< FdmInnerValueCalculator > calculator)

#### Parameters:
- `const std::vector<  exerciseDates`: 
- `const  referenceDate`: 
- `const  dayCounter`: 
- `ext::shared_ptr<  mesher`: 
- `ext::shared_ptr<  calculator`: 

### public void applyTo(Array &a, Time t) const override

#### Parameters:
- `None a`: 
- `None t`: 

### public const std::vector<  exerciseTimes() const


