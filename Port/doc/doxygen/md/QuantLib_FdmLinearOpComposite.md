# Class: QuantLib::FdmLinearOpComposite

## Inheritance
- Inherits from: QuantLib::FdmLinearOp

## Functions
### public None size() const =0


### public void setTime(Time t1, Time t2)=0
Time 
#### Parameters:
- `None t1`: 
- `None t2`: 

### public None apply_mixed(const Array &r) const =0

#### Parameters:
- `const  r`: 

### public None apply_direction(Size direction, const Array &r) const =0

#### Parameters:
- `None direction`: 
- `const  r`: 

### public None solve_splitting(Size direction, const Array &r, Real s) const =0

#### Parameters:
- `None direction`: 
- `const  r`: 
- `None s`: 

### public None preconditioner(const Array &r, Real s) const =0

#### Parameters:
- `const  r`: 
- `None s`: 

### public std::vector<  toMatrixDecomp() const


### public None toMatrix() const override


