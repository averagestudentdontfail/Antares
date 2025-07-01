# Class: QuantLib::detail::QdPutCallParityEngine

## Inheritance
- Inherits from: QuantLib::OneAssetOption::engine

## Member Variables
- `protected const ext::shared_ptr<  process_`: 

## Functions
### public None QdPutCallParityEngine(ext::shared_ptr< GeneralizedBlackScholesProcess > process)

#### Parameters:
- `ext::shared_ptr<  process`: 

### public void calculate() const override


## Functions
### protected None calculatePut(Real S, Real K, Rate r, Rate q, Volatility vol, Time T) const =0

#### Parameters:
- `None S`: 
- `None K`: 
- `None r`: 
- `None q`: 
- `None vol`: 
- `None T`: 

## Functions
### private None calculatePutWithEdgeCases(Real S, Real K, Rate r, Rate q, Volatility vol, Time T) const

#### Parameters:
- `None S`: 
- `None K`: 
- `None r`: 
- `None q`: 
- `None vol`: 
- `None T`: 

