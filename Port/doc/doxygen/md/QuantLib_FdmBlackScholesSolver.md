# Class: QuantLib::FdmBlackScholesSolver

## Inheritance
- Inherits from: QuantLib::LazyObject

## Member Variables
- `private None process_`: 
- `private const  strike_`: 
- `private const  solverDesc_`: 
- `private const  schemeDesc_`: 
- `private const bool localVol_`: 
- `private const  illegalLocalVolOverwrite_`: 
- `private const  quantoHelper_`: 
- `private ext::shared_ptr<  solver_`: 

## Functions
### public None FdmBlackScholesSolver(Handle< GeneralizedBlackScholesProcess > process, Real strike, FdmSolverDesc solverDesc, const FdmSchemeDesc &schemeDesc=FdmSchemeDesc::Douglas(), bool localVol=false, Real illegalLocalVolOverwrite=-Null< Real >(), Handle< FdmQuantoHelper > quantoHelper=Handle< FdmQuantoHelper >())

#### Parameters:
- `None process`: 
- `None strike`: 
- `None solverDesc`: 
- `const  schemeDesc`: 
- `bool localVol`: 
- `None illegalLocalVolOverwrite`: 
- `None quantoHelper`: 

### public None valueAt(Real s) const

#### Parameters:
- `None s`: 

### public None deltaAt(Real s) const

#### Parameters:
- `None s`: 

### public None gammaAt(Real s) const

#### Parameters:
- `None s`: 

### public None thetaAt(Real s) const

#### Parameters:
- `None s`: 

## Functions
### protected void performCalculations() const override


