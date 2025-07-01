# Class: QuantLib::AmericanExercise

## Brief Description
American exercise. 

## Detailed Description
An American option can be exercised at any time between two predefined dates; the first date might be omitted, in which case the option can be exercised at any time before the expiry.

## Inheritance
- Inherits from: QuantLib::EarlyExercise

## Functions
### public None AmericanExercise(const Date &earliestDate, const Date &latestDate, bool payoffAtExpiry=false)

#### Parameters:
- `const  earliestDate`: 
- `const  latestDate`: 
- `bool payoffAtExpiry`: 

### public None AmericanExercise(const Date &latestDate, bool payoffAtExpiry=false)

#### Parameters:
- `const  latestDate`: 
- `bool payoffAtExpiry`: 

