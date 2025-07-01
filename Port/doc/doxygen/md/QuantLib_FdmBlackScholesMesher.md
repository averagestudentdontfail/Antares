# Class: QuantLib::FdmBlackScholesMesher

## Inheritance
- Inherits from: QuantLib::Fdm1dMesher

## Functions
### public None FdmBlackScholesMesher(Size size, const ext::shared_ptr< GeneralizedBlackScholesProcess > &process, Time maturity, Real strike, Real xMinConstraint=Null< Real >(), Real xMaxConstraint=Null< Real >(), Real eps=0.0001, Real scaleFactor=1.5, const std::pair< Real, Real > &cPoint={ Null< Real >(), Null< Real >() }, const DividendSchedule &dividendSchedule={}, const ext::shared_ptr< FdmQuantoHelper > &fdmQuantoHelper={}, Real spotAdjustment=0.0)

#### Parameters:
- `None size`: 
- `const ext::shared_ptr<  process`: 
- `None maturity`: 
- `None strike`: 
- `None xMinConstraint`: 
- `None xMaxConstraint`: 
- `None eps`: 
- `None scaleFactor`: 
- `const std::pair<  cPoint`: 
- `const  dividendSchedule`: 
- `const ext::shared_ptr<  fdmQuantoHelper`: 
- `None spotAdjustment`: 

## Functions
### static public ext::shared_ptr<  processHelper(const Handle< Quote > &s0, const Handle< YieldTermStructure > &rTS, const Handle< YieldTermStructure > &qTS, Volatility vol)

#### Parameters:
- `const  s0`: 
- `const  rTS`: 
- `const  qTS`: 
- `None vol`: 

