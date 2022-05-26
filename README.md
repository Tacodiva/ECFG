# Ember's Config C#

**ECFG** (Ember's Config) is a file format I made because I didn't like JSON or YAML. The JSON parser is not lenient enough, no comments, no trailing commas, no fun. YAML is too lenient with issues like [this](https://hitchdev.com/strictyaml/why/implicit-typing-removed/) wreaking havoc on unsuspecting victims, and don't like it when indentation effects how something is parsed. I created ECFG because 1. I was bord and 2. I wanted an alternative to JSON and YAML.

ECFG's syntax is somewhat similar to YAML's, but different.
```YAML
KeyOne: "This is a string!"
KeyTwo: 'This is also a string!'

# Comment

CoolObject: {
  Another: -420.69 # A number!
  Hex: 0xFF # Also a number! (255)
  Scienfitic: 3.7729e10 # Scientific number notation
  NotANumber: NaN # Another number (not a number :P)
  NotANumber2: nAn # All values that aren't string are case insensitive
  # Keys, however, are case sensitive
  
  Inf: +Infinity
  NegInf: -Infinity
  Nothing: Null
  
  Happy: TrUE
  Sad: FALSe
}

# Multiple entries can be on the same line if seperated by a commas
KeyThree: 3, KeyFour: 4

# You can trail a comma, or nah
KeyFive: "Hi",

# Array
Arr: [
  "Array are not typed"
  201983
  348092, # <-- You don't need a comma
  243290
  
  3829, 10938, 3939 # <-- Unless they're on the same line
  
  390, {
    Key: "Value"
  }
  
  [ 1, 2, 3, 4, 5 ]
]
```

Load an ECFG file like this:
```C#
using Ecfg;

EcfgObject root = EcfgObject.Parse(File.ReadAllText("Test.ecfg"));

Console.WriteLine(root.GetString("KeyOne"));
Console.WriteLine(root.GetObject("CoolObject"));
```

I also wanted a way to be able to serialize data for my game, for networking and to save to disk. So, in addition to the text format, ECFG also comes with a space-efficent binary format for turning ECFG objects to bytes! It can be used like so;

```C#
using Ecfg;

EcfgObject root = ...
// Write the 'root' object to a byte array
byte[] binary = EcfgBin.Serialize(root);

// Read the root object back out of the array
EcfgObject rootCopy = EcfgBin.Deserialize(root);
```

I don't think anybody other than me will ever use this, so I havn't written proper spec docs for the text or binary formats. But, if somebody is interested, just make an issue!
