# Class: QuantLib::TripleBandLinearOp

## Inheritance
- Inherits from: QuantLib::FdmLinearOp

## Member Variables
- `protected None direction_`: 
- `protected std::unique_ptr<  i0_`: 
- `protected std::unique_ptr<  i2_`: 
- `protected std::unique_ptr<  reverseIndex_`: 
- `protected std::unique_ptr<  lower_`: 
- `protected std::unique_ptr<  diag_`: 
- `protected std::unique_ptr<  upper_`: 
- `protected ext::shared_ptr<  mesher_`: 

## Functions
### public None TripleBandLinearOp(Size direction, const ext::shared_ptr< FdmMesher > &mesher)

#### Parameters:
- `None direction`: 
- `const ext::shared_ptr<  mesher`: 

### public None TripleBandLinearOp(const TripleBandLinearOp &m)

#### Parameters:
- `const  m`: 

### public None TripleBandLinearOp(TripleBandLinearOp &&m) noexcept

#### Parameters:
- `None m`: 

### public None operator=(const TripleBandLinearOp &m)

#### Parameters:
- `const  m`: 

### public None operator=(TripleBandLinearOp &&m) noexcept

#### Parameters:
- `None m`: 

### public None ~TripleBandLinearOp() override=default


### public None apply(const Array &r) const override

#### Parameters:
- `const  r`: 

### public None solve_splitting(const Array &r, Real a, Real b=1.0) const

#### Parameters:
- `const  r`: 
- `None a`: 
- `None b`: 

### public None mult(const Array &u) const

#### Parameters:
- `const  u`: 

### public None multR(const Array &u) const

#### Parameters:
- `const  u`: 

### public None add(const TripleBandLinearOp &m) const

#### Parameters:
- `const  m`: 

### public None add(const Array &u) const

#### Parameters:
- `const  u`: 

### public void axpyb(const Array &a, const TripleBandLinearOp &x, const TripleBandLinearOp &y, const Array &b)

#### Parameters:
- `const  a`: 
- `const  x`: 
- `const  y`: 
- `const  b`: 

### public void swap(TripleBandLinearOp &m) noexcept

#### Parameters:
- `None m`: 

### public None toMatrix() const override


## Functions
### protected None TripleBandLinearOp()=default


