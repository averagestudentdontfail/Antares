# Class: QuantLib::Error

## Brief Description
Base error class. 

## Inheritance
- Inherits from: std::exception

## Member Variables
- `private ext::shared_ptr< std::string > message_`: 

## Functions
### public None Error(const std::string &file, long line, const std::string &functionName, const std::string &message="")

#### Parameters:
- `const std::string & file`: 
- `long line`: 
- `const std::string & functionName`: 
- `const std::string & message`: 

### public const char * what() const noexcept override
returns the error message. 

