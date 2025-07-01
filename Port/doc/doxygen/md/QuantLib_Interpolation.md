# Class: QuantLib::Interpolation

## Brief Description
base class for 1-D interpolations. 

## Detailed Description
Classes derived from this class will provide interpolated values from two sequences of equal length, representing discretized values of a variable and a function of the former, respectively.

## Inheritance
- Inherits from: QuantLib::Extrapolator

## Member Variables
- `protected ext::shared_ptr<  impl_`: 

## Functions
### public None Interpolation()=default


### public None ~Interpolation() override=default


### public bool empty() const


### public None operator()(Real x, bool allowExtrapolation=false) const

#### Parameters:
- `None x`: 
- `bool allowExtrapolation`: 

### public None primitive(Real x, bool allowExtrapolation=false) const

#### Parameters:
- `None x`: 
- `bool allowExtrapolation`: 

### public None derivative(Real x, bool allowExtrapolation=false) const

#### Parameters:
- `None x`: 
- `bool allowExtrapolation`: 

### public None secondDerivative(Real x, bool allowExtrapolation=false) const

#### Parameters:
- `None x`: 
- `bool allowExtrapolation`: 

### public None xMin() const


### public None xMax() const


### public bool isInRange(Real x) const

#### Parameters:
- `None x`: 

### public void update()


## Functions
### protected void checkRange(Real x, bool extrapolate) const

#### Parameters:
- `None x`: 
- `bool extrapolate`: 

