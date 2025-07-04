# Class: QuantLib::CrankNicolsonScheme

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
- `protected const ext::shared_ptr<  explicit_`: 
- `protected const ext::shared_ptr<  implicit_`: 

## Functions
### public None CrankNicolsonScheme(Real theta, const ext::shared_ptr< FdmLinearOpComposite > &map, const bc_set &bcSet=bc_set(), Real relTol=1e-8, ImplicitEulerScheme::SolverType solverType=ImplicitEulerScheme::BiCGstab)

#### Parameters:
- `None theta`: 
- `const ext::shared_ptr<  map`: 
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


