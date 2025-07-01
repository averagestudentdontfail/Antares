# Class: QuantLib::QdFpTanhSinhIterationScheme

## Brief Description
tanh-sinh (m,n)-eps Scheme 

## Inheritance
- Inherits from: QuantLib::QdFpIterationScheme

## Member Variables
- `private const  m_`: 
- `private const  n_`: 
- `private const ext::shared_ptr<  integrator_`: 

## Functions
### public None QdFpTanhSinhIterationScheme(Size m, Size n, Real eps)

#### Parameters:
- `None m`: 
- `None n`: 
- `None eps`: 

### public None getNumberOfChebyshevInterpolationNodes() const override


### public None getNumberOfNaiveFixedPointSteps() const override


### public None getNumberOfJacobiNewtonFixedPointSteps() const override


### public ext::shared_ptr<  getFixedPointIntegrator() const override


### public ext::shared_ptr<  getExerciseBoundaryToPriceIntegrator() const override


