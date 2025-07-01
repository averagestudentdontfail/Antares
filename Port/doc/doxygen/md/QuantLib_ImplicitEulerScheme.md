# Class: QuantLib::ImplicitEulerScheme

## Types
### Enum: SolverType
- BiCGstab
- GMRES

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
- `protected ext::shared_ptr<  iterations_`: 
- `protected const  relTol_`: 
- `protected const ext::shared_ptr<  map_`: 
- `protected const  bcSet_`: 
- `protected const  solverType_`: 

## Functions
### public None ImplicitEulerScheme(ext::shared_ptr< FdmLinearOpComposite > map, const bc_set &bcSet=bc_set(), Real relTol=1e-8, SolverType solverType=BiCGstab)

#### Parameters:
- `ext::shared_ptr<  map`: 
- `const  bcSet`: 
- `None relTol`: 
- `None solverType`: 

### public void step(array_type &a, Time t)

#### Parameters:
- `None a`: 
- `None t`: 

### public void setStep(Time dt)

#### Parameters:
- `None dt`: 

### public None numberOfIterations() const


## Functions
### protected void step(array_type &a, Time t, Real theta)

#### Parameters:
- `None a`: 
- `None t`: 
- `None theta`: 

### protected None apply(const Array &r, Real theta) const

#### Parameters:
- `const  r`: 
- `None theta`: 

