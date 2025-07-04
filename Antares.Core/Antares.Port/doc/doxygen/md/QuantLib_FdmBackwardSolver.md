# Class: QuantLib::FdmBackwardSolver

## Types
### Typedef: array_type
- Type: None

## Member Variables
- `protected const ext::shared_ptr<  map_`: 
- `protected const  bcSet_`: 
- `protected const ext::shared_ptr<  condition_`: 
- `protected const  schemeDesc_`: 

## Functions
### public None FdmBackwardSolver(ext::shared_ptr< FdmLinearOpComposite > map, FdmBoundaryConditionSet bcSet, const ext::shared_ptr< FdmStepConditionComposite > &condition, const FdmSchemeDesc &schemeDesc)

#### Parameters:
- `ext::shared_ptr<  map`: 
- `None bcSet`: 
- `const ext::shared_ptr<  condition`: 
- `const  schemeDesc`: 

### public void rollback(array_type &a, Time from, Time to, Size steps, Size dampingSteps)

#### Parameters:
- `None a`: 
- `None from`: 
- `None to`: 
- `None steps`: 
- `None dampingSteps`: 

