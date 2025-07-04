# Class: QuantLib::NormalDistribution

## Brief Description
Normal distribution function. 

## Detailed Description
Given x, it returns its probability in a Gaussian normal distribution. It provides the first derivative too.

## Member Variables
- `private None average_`: 
- `private None sigma_`: 
- `private None normalizationFactor_`: 
- `private None denominator_`: 
- `private None derNormalizationFactor_`: 

## Functions
### public None NormalDistribution(Real average=0.0, Real sigma=1.0)

#### Parameters:
- `None average`: 
- `None sigma`: 

### public None operator()(Real x) const

#### Parameters:
- `None x`: 

### public None derivative(Real x) const

#### Parameters:
- `None x`: 

