# Class: QuantLib::FdmEscrowedLogInnerValueCalculator

## Inheritance
- Inherits from: QuantLib::FdmInnerValueCalculator

## Member Variables
- `private const ext::shared_ptr<  escrowedDividendAdj_`: 
- `private const ext::shared_ptr<  payoff_`: 
- `private const ext::shared_ptr<  mesher_`: 
- `private const  direction_`: 

## Functions
### public None FdmEscrowedLogInnerValueCalculator(ext::shared_ptr< EscrowedDividendAdjustment > escrowedDividendAdj, ext::shared_ptr< Payoff > payoff, ext::shared_ptr< FdmMesher > mesher, Size direction)

#### Parameters:
- `ext::shared_ptr<  escrowedDividendAdj`: 
- `ext::shared_ptr<  payoff`: 
- `ext::shared_ptr<  mesher`: 
- `None direction`: 

### public None innerValue(const FdmLinearOpIterator &iter, Time t) override

#### Parameters:
- `const  iter`: 
- `None t`: 

### public None avgInnerValue(const FdmLinearOpIterator &iter, Time t) override

#### Parameters:
- `const  iter`: 
- `None t`: 

