# Class: QuantLib::Settings

## Brief Description
global repository for run-time library settings 

## Inheritance
- Inherits from: QuantLib::Singleton< Settings >

## Member Variables
- `private None evaluationDate_`: 
- `private bool includeReferenceDateEvents_`: 
- `private ext::optional< bool > includeTodaysCashFlows_`: 
- `private bool enforcesTodaysHistoricFixings_`: 

## Functions
### private None Settings()


## Functions
### public None evaluationDate()
the date at which pricing is to be performed. 

### public const  evaluationDate() const


### public void anchorEvaluationDate()


### public void resetEvaluationDate()


### public bool & includeReferenceDateEvents()


### public bool includeReferenceDateEvents() const


### public ext::optional< bool > & includeTodaysCashFlows()


### public ext::optional< bool > includeTodaysCashFlows() const


### public bool & enforcesTodaysHistoricFixings()


### public bool enforcesTodaysHistoricFixings() const


