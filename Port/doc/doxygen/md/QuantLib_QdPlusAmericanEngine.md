# Class: QuantLib::QdPlusAmericanEngine

## Brief Description
American engine based on the QD+ approximation to the exercise boundary. 

## Detailed Description
The main purpose of this engine is to provide a good initial guess to the exercise boundary for the superior fixed point American engine 

## Inheritance
- Inherits from: QuantLib::detail::QdPutCallParityEngine

## Types
### Enum: SolverType
- Brent
- Newton
- Ridder
- Halley
- SuperHalley

## Member Variables
- `private const  interpolationPoints_`: 
- `private const  solverType_`: 
- `private const  eps_`: 
- `private const  maxIter_`: 

## Functions
### public None QdPlusAmericanEngine(ext::shared_ptr< GeneralizedBlackScholesProcess >, Size interpolationPoints=8, SolverType solverType=Halley, Real eps=1e-6, Size maxIter=Null< Size >())

#### Parameters:
- `ext::shared_ptr<  `: 
- `None interpolationPoints`: 
- `None solverType`: 
- `None eps`: 
- `None maxIter`: 

### public std::pair<  putExerciseBoundaryAtTau(Real S, Real K, Rate r, Rate q, Volatility vol, Time T, Time tau) const

#### Parameters:
- `None S`: 
- `None K`: 
- `None r`: 
- `None q`: 
- `None vol`: 
- `None T`: 
- `None tau`: 

### public ext::shared_ptr<  getPutExerciseBoundary(Real S, Real K, Rate r, Rate q, Volatility vol, Time T) const

#### Parameters:
- `None S`: 
- `None K`: 
- `None r`: 
- `None q`: 
- `None vol`: 
- `None T`: 

## Functions
### static public None xMax(Real K, Rate r, Rate q)

#### Parameters:
- `None K`: 
- `None r`: 
- `None q`: 

## Functions
### protected None calculatePut(Real S, Real K, Rate r, Rate q, Volatility vol, Time T) const override

#### Parameters:
- `None S`: 
- `None K`: 
- `None r`: 
- `None q`: 
- `None vol`: 
- `None T`: 

## Functions
### private None buildInSolver(const QdPlusBoundaryEvaluator &eval, Solver solver, Real S, Real strike, Size maxIter, Real guess=Null< Real >()) const

#### Parameters:
- `const  eval`: 
- `Solver solver`: 
- `None S`: 
- `None strike`: 
- `None maxIter`: 
- `None guess`: 

