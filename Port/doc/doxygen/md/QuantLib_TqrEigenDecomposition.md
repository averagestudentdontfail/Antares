# Class: QuantLib::TqrEigenDecomposition

## Brief Description
tridiag. QR eigen decomposition with explicite shift aka Wilkinson 

## Detailed Description
References:

## Types
### Enum: EigenVectorCalculation
- WithEigenVector
- WithoutEigenVector
- OnlyFirstRowEigenVector

### Enum: ShiftStrategy
- NoShift
- Overrelaxation
- CloseEigenValue

## Member Variables
- `private None iter_`: 
- `private None d_`: 
- `private None ev_`: 

## Functions
### public None TqrEigenDecomposition(const Array &diag, const Array &sub, EigenVectorCalculation calc=WithEigenVector, ShiftStrategy strategy=CloseEigenValue)

#### Parameters:
- `const  diag`: 
- `const  sub`: 
- `None calc`: 
- `None strategy`: 

### public const  eigenvalues() const


### public const  eigenvectors() const


### public None iterations() const


## Functions
### private bool offDiagIsZero(Size k, Array &e)

#### Parameters:
- `None k`: 
- `None e`: 

