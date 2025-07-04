# Class: QuantLib::Option

## Brief Description
base option class 

## Inheritance
- Inherits from: QuantLib::Instrument

## Types
### Enum: Type
- Put
- Call

## Member Variables
- `protected ext::shared_ptr<  payoff_`: 
- `protected ext::shared_ptr<  exercise_`: 

## Functions
### public None Option(ext::shared_ptr< Payoff > payoff, ext::shared_ptr< Exercise > exercise)

#### Parameters:
- `ext::shared_ptr<  payoff`: 
- `ext::shared_ptr<  exercise`: 

### public void setupArguments(PricingEngine::arguments *) const override

#### Parameters:
- `None `: 

### public ext::shared_ptr<  payoff() const


### public ext::shared_ptr<  exercise() const


