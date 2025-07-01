# Class: QuantLib::Observer

## Brief Description
Object that gets notified when a given observable changes. 

## Types
### Typedef: set_type
- Type: std::set< ext::shared_ptr< 

## Types
### Typedef: iterator
- Type: set_type::iterator

## Member Variables
- `private None observables_`: 

## Functions
### public None Observer()=default


### public None Observer(const Observer &)

#### Parameters:
- `const  `: 

### public None operator=(const Observer &)

#### Parameters:
- `const  `: 

### public None ~Observer()


### public std::pair<  registerWith(const ext::shared_ptr< Observable > &)

#### Parameters:
- `const ext::shared_ptr<  `: 

### public void registerWithObservables(const ext::shared_ptr< Observer > &)

#### Parameters:
- `const ext::shared_ptr<  `: 

### public None unregisterWith(const ext::shared_ptr< Observable > &)

#### Parameters:
- `const ext::shared_ptr<  `: 

### public void unregisterWithAll()


### public void update()=0


### public void deepUpdate()


