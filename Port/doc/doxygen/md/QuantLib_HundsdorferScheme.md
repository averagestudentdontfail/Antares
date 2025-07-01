# Class: QuantLib::HundsdorferScheme

## Types
### Typedef: traits
- Type: None

### Typedef: operator_type
- Type: None

### Typedef: array_type
- Type: None

### Typedef: bc_set
- Type: None

### Typedef: condition_type
- Type: None

## Member Variables
- `protected None dt_`: 
- `protected const  theta_`: 
- `protected const  mu_`: 
- `protected const ext::shared_ptr<  map_`: 
- `protected const  bcSet_`: 

## Functions
### public None HundsdorferScheme(Real theta, Real mu, ext::shared_ptr< FdmLinearOpComposite > map, const bc_set &bcSet=bc_set())

#### Parameters:
- `None theta`: 
- `None mu`: 
- `ext::shared_ptr<  map`: 
- `const  bcSet`: 

### public void step(array_type &a, Time t)

#### Parameters:
- `None a`: 
- `None t`: 

### public void setStep(Time dt)

#### Parameters:
- `None dt`: 

