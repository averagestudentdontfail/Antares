# Class: QuantLib::Fdm1DimSolver

## Inheritance
- Inherits from: QuantLib::LazyObject

## Member Variables
- `private const  solverDesc_`: 
- `private const  schemeDesc_`: 
- `private const ext::shared_ptr<  op_`: 
- `private const ext::shared_ptr<  thetaCondition_`: 
- `private const ext::shared_ptr<  conditions_`: 
- `private std::vector<  x_`: 
- `private std::vector<  initialValues_`: 
- `private None resultValues_`: 
- `private ext::shared_ptr<  interpolation_`: 

## Functions
### public None Fdm1DimSolver(const FdmSolverDesc &solverDesc, const FdmSchemeDesc &schemeDesc, ext::shared_ptr< FdmLinearOpComposite > op)

#### Parameters:
- `const  solverDesc`: 
- `const  schemeDesc`: 
- `ext::shared_ptr<  op`: 

### public None interpolateAt(Real x) const

#### Parameters:
- `None x`: 

### public None thetaAt(Real x) const

#### Parameters:
- `None x`: 

### public None derivativeX(Real x) const

#### Parameters:
- `None x`: 

### public None derivativeXX(Real x) const

#### Parameters:
- `None x`: 

## Functions
### protected void performCalculations() const override


