# Class: QuantLib::MakeFdBlackScholesVanillaEngine

## Member Variables
- `private ext::shared_ptr<  process_`: 
- `private None dividends_`: 
- `private None tGrid_`: 
- `private None xGrid_`: 
- `private None dampingSteps_`: 
- `private ext::shared_ptr<  schemeDesc_`: 
- `private bool localVol_`: 
- `private None illegalLocalVolOverwrite_`: 
- `private ext::shared_ptr<  quantoHelper_`: 
- `private None cashDividendModel_`: 

## Functions
### public None MakeFdBlackScholesVanillaEngine(ext::shared_ptr< GeneralizedBlackScholesProcess > process)

#### Parameters:
- `ext::shared_ptr<  process`: 

### public None withQuantoHelper(const ext::shared_ptr< FdmQuantoHelper > &quantoHelper)

#### Parameters:
- `const ext::shared_ptr<  quantoHelper`: 

### public None withTGrid(Size tGrid)

#### Parameters:
- `None tGrid`: 

### public None withXGrid(Size xGrid)

#### Parameters:
- `None xGrid`: 

### public None withDampingSteps(Size dampingSteps)

#### Parameters:
- `None dampingSteps`: 

### public None withFdmSchemeDesc(const FdmSchemeDesc &schemeDesc)

#### Parameters:
- `const  schemeDesc`: 

### public None withLocalVol(bool localVol)

#### Parameters:
- `bool localVol`: 

### public None withIllegalLocalVolOverwrite(Real illegalLocalVolOverwrite)

#### Parameters:
- `None illegalLocalVolOverwrite`: 

### public None withCashDividends(const std::vector< Date > &dividendDates, const std::vector< Real > &dividendAmounts)

#### Parameters:
- `const std::vector<  dividendDates`: 
- `const std::vector<  dividendAmounts`: 

### public None withCashDividendModel(FdBlackScholesVanillaEngine::CashDividendModel cashDividendModel)

#### Parameters:
- `None cashDividendModel`: 

### public None operator ext::shared_ptr< PricingEngine >() const


