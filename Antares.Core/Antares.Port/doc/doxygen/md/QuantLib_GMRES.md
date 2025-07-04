# Class: QuantLib::GMRES

## Types
### Typedef: MatrixMult
- Type: std::function< 

## Member Variables
- `protected const  A_`: 
- `protected const  M_`: 
- `protected const  maxIter_`: 
- `protected const  relTol_`: 

## Functions
### public None GMRES(MatrixMult A, Size maxIter, Real relTol, MatrixMult preConditioner=MatrixMult())

#### Parameters:
- `None A`: 
- `None maxIter`: 
- `None relTol`: 
- `None preConditioner`: 

### public None solve(const Array &b, const Array &x0=Array()) const

#### Parameters:
- `const  b`: 
- `const  x0`: 

### public None solveWithRestart(Size restart, const Array &b, const Array &x0=Array()) const

#### Parameters:
- `None restart`: 
- `const  b`: 
- `const  x0`: 

## Functions
### protected None solveImpl(const Array &b, const Array &x0) const

#### Parameters:
- `const  b`: 
- `const  x0`: 

