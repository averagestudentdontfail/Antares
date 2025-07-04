# Class: QuantLib::ChebyshevInterpolation

## Detailed Description
See S.A. Sarra: Chebyshev 

## Inheritance
- Inherits from: QuantLib::Interpolation

## Types
### Enum: PointsType
- FirstKind
- SecondKind

## Member Variables
- `private const  x_`: 
- `private None y_`: 

## Functions
### public None ChebyshevInterpolation(const Array &y, PointsType pointsType=SecondKind)

#### Parameters:
- `const  y`: 
- `None pointsType`: 

### public None ChebyshevInterpolation(Size n, const std::function< Real(Real)> &f, PointsType pointsType=SecondKind)

#### Parameters:
- `None n`: 
- `const std::function<  f`: 
- `None pointsType`: 

### public None ~ChebyshevInterpolation() override=default


### public None ChebyshevInterpolation(const ChebyshevInterpolation &)=delete

#### Parameters:
- `const  `: 

### public None ChebyshevInterpolation(ChebyshevInterpolation &&)=delete

#### Parameters:
- `None `: 

### public None operator=(const ChebyshevInterpolation &)=delete

#### Parameters:
- `const  `: 

### public None operator=(ChebyshevInterpolation &&)=delete

#### Parameters:
- `None `: 

### public void updateY(const Array &y)

#### Parameters:
- `const  y`: 

### public None nodes() const


## Functions
### static public None nodes(Size n, PointsType pointsType)

#### Parameters:
- `None n`: 
- `None pointsType`: 

