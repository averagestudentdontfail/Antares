# Class: QuantLib::BoundaryConditionSchemeHelper

## Types
### Typedef: array_type
- Type: None

### Typedef: operator_type
- Type: None

## Member Variables
- `private None bcSet_`: 

## Functions
### public None BoundaryConditionSchemeHelper(OperatorTraits< FdmLinearOp >::bc_set bcSet)

#### Parameters:
- `None bcSet`: 

### public void applyBeforeApplying(operator_type &op) const

#### Parameters:
- `None op`: 

### public void applyBeforeSolving(operator_type &op, array_type &a) const

#### Parameters:
- `None op`: 
- `None a`: 

### public void applyAfterApplying(array_type &a) const

#### Parameters:
- `None a`: 

### public void applyAfterSolving(array_type &a) const

#### Parameters:
- `None a`: 

### public void setTime(Time t) const

#### Parameters:
- `None t`: 

## Functions
### private None BoundaryConditionSchemeHelper()=default


