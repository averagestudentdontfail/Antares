# Class: QuantLib::NeumannBC

## Brief Description
Neumann boundary condition (i.e., constant derivative) 

## Inheritance
- Inherits from: QuantLib::BoundaryCondition< TridiagonalOperator >

## Member Variables
- `private None value_`: 
- `private None side_`: 

## Functions
### public None NeumannBC(Real value, Side side)

#### Parameters:
- `None value`: 
- `None side`: 

### public void applyBeforeApplying(TridiagonalOperator &) const override

#### Parameters:
- `None `: 

### public void applyAfterApplying(Array &) const override

#### Parameters:
- `None `: 

### public void applyBeforeSolving(TridiagonalOperator &, Array &rhs) const override

#### Parameters:
- `None `: 
- `None rhs`: 

### public void applyAfterSolving(Array &) const override

#### Parameters:
- `None `: 

### public void setTime(Time) override

#### Parameters:
- `None t`: 

