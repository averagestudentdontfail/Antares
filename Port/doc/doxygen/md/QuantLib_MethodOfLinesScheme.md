# Class: QuantLib::MethodOfLinesScheme

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
- `protected const  eps_`: 
- `protected const  relInitStepSize_`: 
- `protected const ext::shared_ptr<  map_`: 
- `protected const  bcSet_`: 

## Functions
### public None MethodOfLinesScheme(Real eps, Real relInitStepSize, ext::shared_ptr< FdmLinearOpComposite > map, const bc_set &bcSet=bc_set())

#### Parameters:
- `None eps`: 
- `None relInitStepSize`: 
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
### protected std::vector<  apply(Time, const std::vector< Real > &) const

#### Parameters:
- `None `: 
- `const std::vector<  `: 

