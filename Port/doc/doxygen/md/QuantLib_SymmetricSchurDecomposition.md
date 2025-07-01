# Class: QuantLib::SymmetricSchurDecomposition

## Brief Description
symmetric threshold Jacobi algorithm. 

## Detailed Description
Given a real symmetric matrix S, the Schur decomposition finds the eigenvalues and eigenvectors of S. If D is the diagonal matrix formed by the eigenvalues and U the unitarian matrix of the eigenvectors we can write the Schur decomposition as 

## Member Variables
- `private None diagonal_`: 
- `private None eigenVectors_`: 

## Functions
### public None SymmetricSchurDecomposition(const Matrix &s)

#### Parameters:
- `const  s`: 

### public const  eigenvalues() const


### public const  eigenvectors() const


## Functions
### private void jacobiRotate_(Matrix &m, Real rot, Real dil, Size j1, Size k1, Size j2, Size k2) const
This routines implements the Jacobi, a.k.a. Givens, rotation. 
#### Parameters:
- `None m`: 
- `None rot`: 
- `None dil`: 
- `None j1`: 
- `None k1`: 
- `None j2`: 
- `None k2`: 

