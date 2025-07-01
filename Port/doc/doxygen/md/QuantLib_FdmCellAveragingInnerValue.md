# Class: QuantLib::FdmCellAveragingInnerValue

## Inheritance
- Inherits from: QuantLib::FdmInnerValueCalculator

## Member Variables
- `private const ext::shared_ptr<  payoff_`: 
- `private const ext::shared_ptr<  mesher_`: 
- `private const  direction_`: 
- `private const std::function<  gridMapping_`: 
- `private std::vector<  avgInnerValues_`: 

## Functions
### public None FdmCellAveragingInnerValue(ext::shared_ptr< Payoff > payoff, ext::shared_ptr< FdmMesher > mesher, Size direction, std::function< Real(Real)> gridMapping=[](Real x){ return x;})

#### Parameters:
- `ext::shared_ptr<  payoff`: 
- `ext::shared_ptr<  mesher`: 
- `None direction`: 
- `std::function<  gridMapping`: 

### public None innerValue(const FdmLinearOpIterator &iter, Time) override

#### Parameters:
- `const  iter`: 
- `None `: 

### public None avgInnerValue(const FdmLinearOpIterator &iter, Time t) override

#### Parameters:
- `const  iter`: 
- `None t`: 

## Functions
### private None avgInnerValueCalc(const FdmLinearOpIterator &iter, Time t)

#### Parameters:
- `const  iter`: 
- `None t`: 

