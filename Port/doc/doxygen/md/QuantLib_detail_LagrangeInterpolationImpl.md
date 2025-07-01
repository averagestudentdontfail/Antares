# Class: QuantLib::detail::LagrangeInterpolationImpl

## Inheritance
- Inherits from: QuantLib::Interpolation::templateImpl< I1, I2 >
- Inherits from: QuantLib::detail::UpdatedYInterpolation

## Member Variables
- `private const  n_`: 
- `private None lambda_`: 

## Functions
### public None LagrangeInterpolationImpl(const I1 &xBegin, const I1 &xEnd, const I2 &yBegin)

#### Parameters:
- `const I1 & xBegin`: 
- `const I1 & xEnd`: 
- `const I2 & yBegin`: 

### public void update() override


### public None value(Real x) const override

#### Parameters:
- `None x`: 

### public None derivative(Real x) const override

#### Parameters:
- `None x`: 

### public None primitive(Real) const override

#### Parameters:
- `None `: 

### public None secondDerivative(Real) const override

#### Parameters:
- `None `: 

### public None value(const Array &y, Real x) const override

#### Parameters:
- `const  y`: 
- `None x`: 

## Functions
### private None _value(const Iterator &yBegin, Real x) const

#### Parameters:
- `const Iterator & yBegin`: 
- `None x`: 

