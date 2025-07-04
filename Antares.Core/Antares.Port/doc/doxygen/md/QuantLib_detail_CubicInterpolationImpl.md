# Class: QuantLib::detail::CubicInterpolationImpl

## Inheritance
- Inherits from: QuantLib::detail::CoefficientHolder
- Inherits from: QuantLib::Interpolation::templateImpl< I1, I2 >

## Member Variables
- `private None da_`: 
- `private bool monotonic_`: 
- `private None leftType_`: 
- `private None rightType_`: 
- `private None leftValue_`: 
- `private None rightValue_`: 
- `private None tmp_`: 
- `private std::vector<  dx_`: 
- `private std::vector<  S_`: 
- `private None L_`: 

## Functions
### public None CubicInterpolationImpl(const I1 &xBegin, const I1 &xEnd, const I2 &yBegin, CubicInterpolation::DerivativeApprox da, bool monotonic, CubicInterpolation::BoundaryCondition leftCondition, Real leftConditionValue, CubicInterpolation::BoundaryCondition rightCondition, Real rightConditionValue)

#### Parameters:
- `const I1 & xBegin`: 
- `const I1 & xEnd`: 
- `const I2 & yBegin`: 
- `None da`: 
- `bool monotonic`: 
- `None leftCondition`: 
- `None leftConditionValue`: 
- `None rightCondition`: 
- `None rightConditionValue`: 

### public void update() override


### public None value(Real x) const override

#### Parameters:
- `None x`: 

### public None primitive(Real x) const override

#### Parameters:
- `None x`: 

### public None derivative(Real x) const override

#### Parameters:
- `None x`: 

### public None secondDerivative(Real x) const override

#### Parameters:
- `None x`: 

## Functions
### private None cubicInterpolatingPolynomialDerivative(Real a, Real b, Real c, Real d, Real u, Real v, Real w, Real z, Real x) const

#### Parameters:
- `None a`: 
- `None b`: 
- `None c`: 
- `None d`: 
- `None u`: 
- `None v`: 
- `None w`: 
- `None z`: 
- `None x`: 

