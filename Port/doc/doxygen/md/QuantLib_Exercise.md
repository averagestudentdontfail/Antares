# Class: QuantLib::Exercise

## Brief Description
Base exercise class. 

## Types
### Enum: Type
- American
- Bermudan
- European

## Member Variables
- `protected std::vector<  dates_`: 
- `protected None type_`: 

## Functions
### public None Exercise(Type type)

#### Parameters:
- `None type`: 

### public None ~Exercise()=default


### public None type() const


### public None date(Size index) const

#### Parameters:
- `None index`: 

### public None dateAt(Size index) const

#### Parameters:
- `None index`: 

### public const std::vector<  dates() const
Returns all exercise dates. 

### public None lastDate() const


