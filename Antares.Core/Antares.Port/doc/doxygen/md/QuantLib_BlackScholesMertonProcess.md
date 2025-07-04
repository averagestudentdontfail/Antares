# Class: QuantLib::BlackScholesMertonProcess

## Brief Description
Merton (1973) extension to the Black-Scholes stochastic process. 

## Detailed Description
This class describes the stochastic process ln(S) for a stock or stock index paying a continuous dividend yield given by  

## Inheritance
- Inherits from: QuantLib::GeneralizedBlackScholesProcess

## Functions
### public None BlackScholesMertonProcess(const Handle< Quote > &x0, const Handle< YieldTermStructure > &dividendTS, const Handle< YieldTermStructure > &riskFreeTS, const Handle< BlackVolTermStructure > &blackVolTS, const ext::shared_ptr< discretization > &d=ext::shared_ptr< discretization >(new EulerDiscretization), bool forceDiscretization=false)

#### Parameters:
- `const  x0`: 
- `const  dividendTS`: 
- `const  riskFreeTS`: 
- `const  blackVolTS`: 
- `const ext::shared_ptr<  d`: 
- `bool forceDiscretization`: 

