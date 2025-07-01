# Class: QuantLib::FdBlackScholesVanillaEngine

## Brief Description
Finite-differences Black Scholes vanilla option engine. 

## Inheritance
- Inherits from: VanillaOption::engine

## Types
### Enum: CashDividendModel
- Spot
- Escrowed

## Member Variables
- `private ext::shared_ptr<  process_`: 
- `private None dividends_`: 
- `private None tGrid_`: 
- `private None xGrid_`: 
- `private None dampingSteps_`: 
- `private None schemeDesc_`: 
- `private bool localVol_`: 
- `private None illegalLocalVolOverwrite_`: 
- `private ext::shared_ptr<  quantoHelper_`: 
- `private None cashDividendModel_`: 

## Functions
### public None FdBlackScholesVanillaEngine(ext::shared_ptr< GeneralizedBlackScholesProcess >, Size tGrid=100, Size xGrid=100, Size dampingSteps=0, const FdmSchemeDesc &schemeDesc=FdmSchemeDesc::Douglas(), bool localVol=false, Real illegalLocalVolOverwrite=-Null< Real >(), CashDividendModel cashDividendModel=Spot)

#### Parameters:
- `ext::shared_ptr<  `: 
- `None tGrid`: 
- `None xGrid`: 
- `None dampingSteps`: 
- `const  schemeDesc`: 
- `bool localVol`: 
- `None illegalLocalVolOverwrite`: 
- `None cashDividendModel`: 

### public None FdBlackScholesVanillaEngine(ext::shared_ptr< GeneralizedBlackScholesProcess >, DividendSchedule dividends, Size tGrid=100, Size xGrid=100, Size dampingSteps=0, const FdmSchemeDesc &schemeDesc=FdmSchemeDesc::Douglas(), bool localVol=false, Real illegalLocalVolOverwrite=-Null< Real >(), CashDividendModel cashDividendModel=Spot)

#### Parameters:
- `ext::shared_ptr<  `: 
- `None dividends`: 
- `None tGrid`: 
- `None xGrid`: 
- `None dampingSteps`: 
- `const  schemeDesc`: 
- `bool localVol`: 
- `None illegalLocalVolOverwrite`: 
- `None cashDividendModel`: 

### public None FdBlackScholesVanillaEngine(ext::shared_ptr< GeneralizedBlackScholesProcess >, ext::shared_ptr< FdmQuantoHelper > quantoHelper, Size tGrid=100, Size xGrid=100, Size dampingSteps=0, const FdmSchemeDesc &schemeDesc=FdmSchemeDesc::Douglas(), bool localVol=false, Real illegalLocalVolOverwrite=-Null< Real >(), CashDividendModel cashDividendModel=Spot)

#### Parameters:
- `ext::shared_ptr<  `: 
- `ext::shared_ptr<  quantoHelper`: 
- `None tGrid`: 
- `None xGrid`: 
- `None dampingSteps`: 
- `const  schemeDesc`: 
- `bool localVol`: 
- `None illegalLocalVolOverwrite`: 
- `None cashDividendModel`: 

### public None FdBlackScholesVanillaEngine(ext::shared_ptr< GeneralizedBlackScholesProcess >, DividendSchedule dividends, ext::shared_ptr< FdmQuantoHelper > quantoHelper, Size tGrid=100, Size xGrid=100, Size dampingSteps=0, const FdmSchemeDesc &schemeDesc=FdmSchemeDesc::Douglas(), bool localVol=false, Real illegalLocalVolOverwrite=-Null< Real >(), CashDividendModel cashDividendModel=Spot)

#### Parameters:
- `ext::shared_ptr<  `: 
- `None dividends`: 
- `ext::shared_ptr<  quantoHelper`: 
- `None tGrid`: 
- `None xGrid`: 
- `None dampingSteps`: 
- `const  schemeDesc`: 
- `bool localVol`: 
- `None illegalLocalVolOverwrite`: 
- `None cashDividendModel`: 

### public void calculate() const override


