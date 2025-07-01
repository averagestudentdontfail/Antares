# Class: QuantLib::ObservableValue

## Brief Description
observable and assignable proxy to concrete value 

## Detailed Description
Observers can be registered with instances of this class so that they are notified when a different value is assigned to such instances. Client code can copy the contained value or pass it to functions via implicit conversion. 

## Functions
### public None ObservableValue()


### public None ObservableValue(T &&)

#### Parameters:
- `T && `: 

### public None ObservableValue(const T &)

#### Parameters:
- `const T & `: 

### public None ObservableValue(const ObservableValue< T > &)

#### Parameters:
- `const  `: 

### public None ~ObservableValue()=default


