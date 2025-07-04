# Class: QuantLib::QdFpLegendreScheme

## Brief Description
Gauss-Legendre (l,m,n)-p Scheme. 

## Inheritance
- Inherits from: QuantLib::QdFpIterationScheme

## Member Variables
- `private const  m_`: 
- `private const  n_`: 
- `private const ext::shared_ptr<  fpIntegrator_`: 
- `private const ext::shared_ptr<  exerciseBoundaryIntegrator_`: 

## Functions
### public None QdFpLegendreScheme(Size l, Size m, Size n, Size p)

#### Parameters:
- `None l`: 
- `None m`: 
- `None n`: 
- `None p`: 

### public None getNumberOfChebyshevInterpolationNodes() const override


### public None getNumberOfNaiveFixedPointSteps() const override


### public None getNumberOfJacobiNewtonFixedPointSteps() const override


### public ext::shared_ptr<  getFixedPointIntegrator() const override


### public ext::shared_ptr<  getExerciseBoundaryToPriceIntegrator() const override


