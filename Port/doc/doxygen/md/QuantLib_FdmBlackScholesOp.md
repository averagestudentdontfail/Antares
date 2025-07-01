# Class: QuantLib::FdmBlackScholesOp

## Inheritance
- Inherits from: QuantLib::FdmLinearOpComposite

## Member Variables
- `private const ext::shared_ptr<  mesher_`: 
- `private const ext::shared_ptr<  rTS_`: 
- `private const ext::shared_ptr<  qTS_`: 
- `private const ext::shared_ptr<  volTS_`: 
- `private const ext::shared_ptr<  localVol_`: 
- `private const  x_`: 
- `private const  dxMap_`: 
- `private const  dxxMap_`: 
- `private None mapT_`: 
- `private const  strike_`: 
- `private const  illegalLocalVolOverwrite_`: 
- `private const  direction_`: 
- `private const ext::shared_ptr<  quantoHelper_`: 

## Functions
### public None FdmBlackScholesOp(const ext::shared_ptr< FdmMesher > &mesher, const ext::shared_ptr< GeneralizedBlackScholesProcess > &process, Real strike, bool localVol=false, Real illegalLocalVolOverwrite=-Null< Real >(), Size direction=0, ext::shared_ptr< FdmQuantoHelper > quantoHelper=ext::shared_ptr< FdmQuantoHelper >())

#### Parameters:
- `const ext::shared_ptr<  mesher`: 
- `const ext::shared_ptr<  process`: 
- `None strike`: 
- `bool localVol`: 
- `None illegalLocalVolOverwrite`: 
- `None direction`: 
- `ext::shared_ptr<  quantoHelper`: 

### public None size() const override


### public void setTime(Time t1, Time t2) override
Time 
#### Parameters:
- `None t1`: 
- `None t2`: 

### public None apply(const Array &r) const override

#### Parameters:
- `const  r`: 

### public None apply_mixed(const Array &r) const override

#### Parameters:
- `const  r`: 

### public None apply_direction(Size direction, const Array &r) const override

#### Parameters:
- `None direction`: 
- `const  r`: 

### public None solve_splitting(Size direction, const Array &r, Real s) const override

#### Parameters:
- `None direction`: 
- `const  r`: 
- `None s`: 

### public None preconditioner(const Array &r, Real s) const override

#### Parameters:
- `const  r`: 
- `None s`: 

### public std::vector<  toMatrixDecomp() const override


