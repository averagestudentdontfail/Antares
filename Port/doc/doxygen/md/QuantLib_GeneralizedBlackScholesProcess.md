# Class: QuantLib::GeneralizedBlackScholesProcess

## Brief Description
Generalized Black-Scholes stochastic process. 

## Detailed Description
This class describes the stochastic process 

## Inheritance
- Inherits from: QuantLib::StochasticProcess1D

## Functions
### public None GeneralizedBlackScholesProcess(Handle< Quote > x0, Handle< YieldTermStructure > dividendTS, Handle< YieldTermStructure > riskFreeTS, Handle< BlackVolTermStructure > blackVolTS, const ext::shared_ptr< discretization > &d=ext::shared_ptr< discretization >(new EulerDiscretization), bool forceDiscretization=false)

#### Parameters:
- `None x0`: 
- `None dividendTS`: 
- `None riskFreeTS`: 
- `None blackVolTS`: 
- `const ext::shared_ptr<  d`: 
- `bool forceDiscretization`: 

### public None GeneralizedBlackScholesProcess(Handle< Quote > x0, Handle< YieldTermStructure > dividendTS, Handle< YieldTermStructure > riskFreeTS, Handle< BlackVolTermStructure > blackVolTS, Handle< LocalVolTermStructure > localVolTS)

#### Parameters:
- `None x0`: 
- `None dividendTS`: 
- `None riskFreeTS`: 
- `None blackVolTS`: 
- `None localVolTS`: 

