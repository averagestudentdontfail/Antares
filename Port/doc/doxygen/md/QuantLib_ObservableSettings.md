# Class: QuantLib::ObservableSettings

## Brief Description
global repository for run-time library settings 

## Inheritance
- Inherits from: QuantLib::Singleton< ObservableSettings >

## Types
### Typedef: set_type
- Type: std::set< 

### Typedef: iterator
- Type: set_type::iterator

## Member Variables
- `private None deferredObservers_`: 
- `private bool updatesEnabled_`: 
- `private bool updatesDeferred_`: 

## Functions
### public void disableUpdates(bool deferred=false)

#### Parameters:
- `bool deferred`: 

### public void enableUpdates()


### public bool updatesEnabled() const


### public bool updatesDeferred() const


## Functions
### private None ObservableSettings()=default


### private void registerDeferredObservers(const Observable::set_type &observers)

#### Parameters:
- `const  observers`: 

### private void unregisterDeferredObserver(Observer *)

#### Parameters:
- `None `: 

