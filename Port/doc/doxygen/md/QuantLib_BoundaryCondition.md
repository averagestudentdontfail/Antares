# Class: QuantLib::BoundaryCondition

## Brief Description
Abstract boundary condition class for finite difference problems. 

## Types
### Enum: Side
- None
- Upper
- Lower

### Typedef: operator_type
- Type: Operator

### Typedef: array_type
- Type: Operator::array_type

## Functions
### public None ~BoundaryCondition()=default


### public void applyBeforeApplying(operator_type &) const =0

#### Parameters:
- `None `: 

### public void applyAfterApplying(array_type &) const =0

#### Parameters:
- `None `: 

### public void applyBeforeSolving(operator_type &, array_type &rhs) const =0

#### Parameters:
- `None `: 
- `None rhs`: 

### public void applyAfterSolving(array_type &) const =0

#### Parameters:
- `None `: 

### public void setTime(Time t)=0

#### Parameters:
- `None t`: 

