# Class: QuantLib::FiniteDifferenceModel

## Brief Description
Generic finite difference model. 

## Types
### Typedef: traits
- Type: Evolver::traits

### Typedef: operator_type
- Type: traits::operator_type

### Typedef: array_type
- Type: traits::array_type

### Typedef: bc_set
- Type: traits::bc_set

### Typedef: condition_type
- Type: traits::condition_type

## Member Variables
- `private Evolver evolver_`: 
- `private std::vector<  stoppingTimes_`: 

## Functions
### public None FiniteDifferenceModel(const operator_type &L, const bc_set &bcs, std::vector< Time > stoppingTimes=std::vector< Time >())

#### Parameters:
- `const  L`: 
- `const  bcs`: 
- `std::vector<  stoppingTimes`: 

### public None FiniteDifferenceModel(Evolver evolver, std::vector< Time > stoppingTimes=std::vector< Time >())

#### Parameters:
- `Evolver evolver`: 
- `std::vector<  stoppingTimes`: 

### public const Evolver & evolver() const


### public void rollback(array_type &a, Time from, Time to, Size steps)

#### Parameters:
- `None a`: 
- `None from`: 
- `None to`: 
- `None steps`: 

### public void rollback(array_type &a, Time from, Time to, Size steps, const condition_type &condition)

#### Parameters:
- `None a`: 
- `None from`: 
- `None to`: 
- `None steps`: 
- `const  condition`: 

## Functions
### private void rollbackImpl(array_type &a, Time from, Time to, Size steps, const condition_type *condition)

#### Parameters:
- `None a`: 
- `None from`: 
- `None to`: 
- `None steps`: 
- `const  condition`: 

