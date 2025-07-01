# Class: QuantLib::QdFpAmericanEngine

## Brief Description
High performance/precision American engine based on fixed point iteration for the exercise boundary. 

## Detailed Description
References: Leif Andersen, Mark Lake and Dimitri Offengenden (2015) "High Performance American Option Pricing", 

## Inheritance
- Inherits from: QuantLib::detail::QdPutCallParityEngine

## Types
### Enum: FixedPointEquation
- FP_A
- FP_B
- Auto

## Member Variables
- `private const ext::shared_ptr<  iterationScheme_`: 
- `private const  fpEquation_`: 

## Functions
### public None QdFpAmericanEngine(ext::shared_ptr< GeneralizedBlackScholesProcess > bsProcess, ext::shared_ptr< QdFpIterationScheme > iterationScheme=accurateScheme(), FixedPointEquation fpEquation=Auto)

#### Parameters:
- `ext::shared_ptr<  bsProcess`: 
- `ext::shared_ptr<  iterationScheme`: 
- `None fpEquation`: 

## Functions
### static public ext::shared_ptr<  fastScheme()


### static public ext::shared_ptr<  accurateScheme()


### static public ext::shared_ptr<  highPrecisionScheme()


## Functions
### protected None calculatePut(Real S, Real K, Rate r, Rate q, Volatility vol, Time T) const override

#### Parameters:
- `None S`: 
- `None K`: 
- `None r`: 
- `None q`: 
- `None vol`: 
- `None T`: 

