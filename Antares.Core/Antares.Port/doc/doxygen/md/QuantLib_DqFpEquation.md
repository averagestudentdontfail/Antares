# Class: QuantLib::DqFpEquation

## Member Variables
- `protected None x_i`: 
- `protected None w_i`: 
- `protected const  r`: 
- `protected const  q`: 
- `protected const  vol`: 
- `protected const std::function<  B`: 
- `protected const ext::shared_ptr<  integrator`: 
- `protected const  phi`: 
- `protected const  Phi`: 

## Functions
### public None DqFpEquation(Rate _r, Rate _q, Volatility _vol, std::function< Real(Real)> B, ext::shared_ptr< Integrator > _integrator)

#### Parameters:
- `None _r`: 
- `None _q`: 
- `None _vol`: 
- `std::function<  B`: 
- `ext::shared_ptr<  _integrator`: 

### public std::pair<  NDd(Real tau, Real b) const =0

#### Parameters:
- `None tau`: 
- `None b`: 

### public std::tuple<  f(Real tau, Real b) const =0

#### Parameters:
- `None tau`: 
- `None b`: 

### public None ~DqFpEquation()=default


## Functions
### protected std::pair<  d(Time t, Real z) const

#### Parameters:
- `None t`: 
- `None z`: 

