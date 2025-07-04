# Class: QuantLib::CubicInterpolation

## Brief Description
Cubic interpolation between discrete points. 

## Inheritance
- Inherits from: QuantLib::Interpolation

## Types
### Enum: DerivativeApprox
- Spline
- SplineOM1
- SplineOM2
- FourthOrder
- Parabolic
- FritschButland
- Akima
- Kruger
- Harmonic

### Enum: BoundaryCondition
- NotAKnot
- FirstDerivative
- SecondDerivative
- Periodic
- Lagrange

## Functions
### public None CubicInterpolation(const I1 &xBegin, const I1 &xEnd, const I2 &yBegin, CubicInterpolation::DerivativeApprox da, bool monotonic, CubicInterpolation::BoundaryCondition leftCond, Real leftConditionValue, CubicInterpolation::BoundaryCondition rightCond, Real rightConditionValue)

#### Parameters:
- `const I1 & xBegin`: 
- `const I1 & xEnd`: 
- `const I2 & yBegin`: 
- `None da`: 
- `bool monotonic`: 
- `None leftCond`: 
- `None leftConditionValue`: 
- `None rightCond`: 
- `None rightConditionValue`: 

### public const std::vector<  primitiveConstants() const


### public const std::vector<  aCoefficients() const


### public const std::vector<  bCoefficients() const


### public const std::vector<  cCoefficients() const


### public const std::vector< bool > & monotonicityAdjustments() const


## Functions
### private const  coeffs() const


