# Class: QuantLib::AnalyticEuropeanEngine

## Brief Description
Pricing engine for European vanilla options using analytical formulae. 

## Inheritance
- Inherits from: VanillaOption::engine

## Member Variables
- `private ext::shared_ptr<  process_`: 
- `private None discountCurve_`: 

## Functions
### public None AnalyticEuropeanEngine(ext::shared_ptr< GeneralizedBlackScholesProcess >)

#### Parameters:
- `ext::shared_ptr<  `: 

### public None AnalyticEuropeanEngine(ext::shared_ptr< GeneralizedBlackScholesProcess > process, Handle< YieldTermStructure > discountCurve)

#### Parameters:
- `ext::shared_ptr<  process`: 
- `None discountCurve`: 

### public void calculate() const override


