# Class: QuantLib::DayCounter::Impl

## Brief Description
abstract base class for day counter implementations 

## Functions
### public None ~Impl()=default


### public std::string name() const =0


### public None dayCount(const Date &d1, const Date &d2) const
to be overloaded by more complex day counters 
#### Parameters:
- `const  d1`: 
- `const  d2`: 

### public None yearFraction(const Date &d1, const Date &d2, const Date &refPeriodStart, const Date &refPeriodEnd) const =0

#### Parameters:
- `const  d1`: 
- `const  d2`: 
- `const  refPeriodStart`: 
- `const  refPeriodEnd`: 

