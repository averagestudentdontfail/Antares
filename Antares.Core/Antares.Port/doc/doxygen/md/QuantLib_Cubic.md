# Class: QuantLib::Cubic

## Brief Description
Cubic interpolation factory and traits 

## Member Variables
- `private None da_`: 
- `private bool monotonic_`: 
- `private None leftType_`: 
- `private None rightType_`: 
- `private None leftValue_`: 
- `private None rightValue_`: 

## Functions
### public None Cubic(CubicInterpolation::DerivativeApprox da=CubicInterpolation::Kruger, bool monotonic=false, CubicInterpolation::BoundaryCondition leftCondition=CubicInterpolation::SecondDerivative, Real leftConditionValue=0.0, CubicInterpolation::BoundaryCondition rightCondition=CubicInterpolation::SecondDerivative, Real rightConditionValue=0.0)

#### Parameters:
- `None da`: 
- `bool monotonic`: 
- `None leftCondition`: 
- `None leftConditionValue`: 
- `None rightCondition`: 
- `None rightConditionValue`: 

### public None interpolate(const I1 &xBegin, const I1 &xEnd, const I2 &yBegin) const

#### Parameters:
- `const I1 & xBegin`: 
- `const I1 & xEnd`: 
- `const I2 & yBegin`: 

