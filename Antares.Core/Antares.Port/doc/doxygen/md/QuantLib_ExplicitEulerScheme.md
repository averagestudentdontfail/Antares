# Class: QuantLib::ExplicitEulerScheme

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
- `protected const ext::shared_ptr<  map_`: 
- `protected const  bcSet_`: 

## Functions
### public None ExplicitEulerScheme(ext::shared_ptr< FdmLinearOpComposite > map, const bc_set &bcSet=bc_set())

#### Parameters:
- `ext::shared_ptr<  map`: 
- `const  bcSet`: 

### public void step(array_type &a, Time t)

#### Parameters:
- `None a`: 
- `None t`: 

### public void setStep(Time dt)

#### Parameters:
- `None dt`: 

## Functions
### protected void step(array_type &a, Time t, Real theta)

#### Parameters:
- `None a`: 
- `None t`: 
- `None theta`: 

