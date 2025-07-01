# Class: QuantLib::Handle

## Brief Description
Shared handle to an observable. 

## Detailed Description
All copies of an instance of this class refer to the same observable by means of a relinkable smart pointer. When such pointer is relinked to another observable, the change will be propagated to all the copies.

## Member Variables
- `protected ext::shared_ptr<  link_`: 

