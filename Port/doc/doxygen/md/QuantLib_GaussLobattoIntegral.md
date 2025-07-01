# Class: QuantLib::GaussLobattoIntegral

## Brief Description
Integral of a one-dimensional function. 

## Detailed Description
Given a target accuracy 

## Inheritance
- Inherits from: QuantLib::Integrator

## Member Variables
- `protected None relAccuracy_`: 
- `protected const bool useConvergenceEstimate_`: 

## Functions
### public None GaussLobattoIntegral(Size maxIterations, Real absAccuracy, Real relAccuracy=Null< Real >(), bool useConvergenceEstimate=true)

#### Parameters:
- `None maxIterations`: 
- `None absAccuracy`: 
- `None relAccuracy`: 
- `bool useConvergenceEstimate`: 

## Functions
### protected None integrate(const std::function< Real(Real)> &f, Real a, Real b) const override

#### Parameters:
- `const std::function<  f`: 
- `None a`: 
- `None b`: 

### protected None adaptivGaussLobattoStep(const std::function< Real(Real)> &f, Real a, Real b, Real fa, Real fb, Real is) const

#### Parameters:
- `const std::function<  f`: 
- `None a`: 
- `None b`: 
- `None fa`: 
- `None fb`: 
- `None is`: 

### protected None calculateAbsTolerance(const std::function< Real(Real)> &f, Real a, Real b) const

#### Parameters:
- `const std::function<  f`: 
- `None a`: 
- `None b`: 

