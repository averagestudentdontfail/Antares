# Class: QuantLib::FdmStepConditionComposite

## Inheritance
- Inherits from: QuantLib::StepCondition< Array >

## Types
### Typedef: Conditions
- Type: std::list< ext::shared_ptr< 

## Member Variables
- `private std::vector<  stoppingTimes_`: 
- `private const  conditions_`: 

## Functions
### public None FdmStepConditionComposite(const std::list< std::vector< Time > > &stoppingTimes, Conditions conditions)

#### Parameters:
- `const std::list< std::vector<  stoppingTimes`: 
- `None conditions`: 

### public void applyTo(Array &a, Time t) const override

#### Parameters:
- `None a`: 
- `None t`: 

### public const std::vector<  stoppingTimes() const


### public const  conditions() const


## Functions
### static public ext::shared_ptr<  joinConditions(const ext::shared_ptr< FdmSnapshotCondition > &c1, const ext::shared_ptr< FdmStepConditionComposite > &c2)

#### Parameters:
- `const ext::shared_ptr<  c1`: 
- `const ext::shared_ptr<  c2`: 

### static public ext::shared_ptr<  vanillaComposite(const DividendSchedule &schedule, const ext::shared_ptr< Exercise > &exercise, const ext::shared_ptr< FdmMesher > &mesher, const ext::shared_ptr< FdmInnerValueCalculator > &calculator, const Date &refDate, const DayCounter &dayCounter)

#### Parameters:
- `const  schedule`: 
- `const ext::shared_ptr<  exercise`: 
- `const ext::shared_ptr<  mesher`: 
- `const ext::shared_ptr<  calculator`: 
- `const  refDate`: 
- `const  dayCounter`: 

