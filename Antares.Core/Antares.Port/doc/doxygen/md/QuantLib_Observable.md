# Class: QuantLib::Observable

## Brief Description
Object that notifies its changes to a set of observers. 

## Types
### Typedef: set_type
- Type: std::set< 

### Typedef: iterator
- Type: set_type::iterator

## Member Variables
- `private None observers_`: 

## Functions
### public None Observable()=default


### public None Observable(const Observable &)

#### Parameters:
- `const  `: 

### public None operator=(const Observable &)

#### Parameters:
- `const  `: 

### public None Observable(Observable &&)=delete

#### Parameters:
- `None `: 

### public None operator=(Observable &&)=delete

#### Parameters:
- `None `: 

### public None ~Observable()=default


### public void notifyObservers()


## Functions
### private std::pair<  registerObserver(Observer *)

#### Parameters:
- `None `: 

### private None unregisterObserver(Observer *)

#### Parameters:
- `None `: 

