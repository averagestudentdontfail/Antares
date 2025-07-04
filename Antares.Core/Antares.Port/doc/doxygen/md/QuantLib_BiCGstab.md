# Class: QuantLib::BiCGstab

## Types
### Typedef: MatrixMult
- Type: std::function< 

## Member Variables
- `protected const  A_`: 
- `protected const  M_`: 
- `protected const  maxIter_`: 
- `protected const  relTol_`: 

## Functions
### public None BiCGstab(MatrixMult A, Size maxIter, Real relTol, MatrixMult preConditioner=MatrixMult())

#### Parameters:
- `None A`: 
- `None maxIter`: 
- `None relTol`: 
- `None preConditioner`: 

### public None solve(const Array &b, const Array &x0=Array()) const

#### Parameters:
- `const  b`: 
- `const  x0`: 

