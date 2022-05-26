using Xunit;

using Ecfg;
using System.Collections.Generic;
using System;

namespace Ecfg.Test;

public class EcfgTest {

    [Fact]
    public void LongDoubleConversionTest() {
        EcfgObject testObject = new EcfgObject() {
            ["TestDouble"] = new EcfgDouble(69),
            ["TestLong"] = new EcfgLong(420),
            ["TestDouble2"] = new EcfgDouble(420.69)
        };
        Assert.Equal(69, testObject.GetLong("TestDouble"));
        Assert.Equal(420, testObject.GetDouble("TestLong"));
        Assert.Throws<EcfgException>(() => testObject.GetLong("TestDouble2")); // Can't convert 420.69 into a long
    }

    [Theory]
    [MemberData(nameof(TestEcfgStrings))]
    public void ToEcfgTest(string textFormat, EcfgObject memoryFormat) {
        Assert.True(EcfgObject.Parse(memoryFormat.ToEcfg()).DeepEquals(memoryFormat));
    }

    [Theory]
    [MemberData(nameof(TestEcfgStrings))]
    public void TokenizeTest(string textFormat, EcfgObject memoryFormat) {
        EcfgDocument document = EcfgDocument.Parse(textFormat);

        Assert.Equal(textFormat, document.ToString());
        Assert.True(memoryFormat.DeepEquals(document));
    }

    [Theory]
    [MemberData(nameof(TestEcfgStrings))]
    public void ParseSerializeDeserializeTest(string textFormat, EcfgObject memoryFormat) {

        EcfgObject parsed = EcfgObject.Parse(textFormat);
        Assert.Equal(parsed.ToString(), memoryFormat.ToString());
        Assert.True(parsed.DeepEquals(memoryFormat), "Parsed object not equal to speicifed object!");

        byte[] serialized = EcfgBin.Serialize(parsed, true);
        EcfgObject deserialized = EcfgBin.Deserialize<EcfgObject>(serialized);
        Assert.True(deserialized.DeepEquals(memoryFormat), "Deserialized object not equal to serialized object!");
    }

    public static IEnumerable<object[]> TestEcfgStrings() {
        yield return new object[] { @"
KeyOne: ""Abcde""
KeyTwo: 'Hello!' # Comment!
Number: 10, # This is a comment
# Another comment
",new EcfgObject() {
    ["KeyOne"] = new EcfgString("Abcde"),
    ["KeyTwo"] = new EcfgString("Hello!"),
    ["Number"] = new EcfgLong(10),
}};
        yield return new object[] {
@"This: ""Is"", AllOn: 1, Line: '!',
ExceptFor: ""ME!""
",new EcfgObject() {
    ["This"] = new EcfgString("Is"),
    ["AllOn"] = new EcfgLong(1),
    ["Line"] = new EcfgString("!"),
    ["ExceptFor"] = new EcfgString("ME!"),
}};
        yield return new object[] {@"
Hello: { World: ""Blah"" }
World: 69
", new EcfgObject() {
    ["Hello"] = new EcfgObject() {
        ["World"] = new EcfgString("Blah")
    },
    ["World"] = new EcfgLong(69)
}};
        yield return new object[] { @"
OkayBut: {
    NestedObjects: { # Comment
        Are     : 'Cool', Test: 1
        Test2 : 22908 # Comment
    }, Arent: ""They?""
    WhatAbout: { AllOn: 1, Long: {Line: ""?""}},
    YeahI: 'Thought so',    Test2: {} # Comment
    Empty: {

        # Object
    }
}
", new EcfgObject() {
    ["OkayBut"] = new EcfgObject() {
        ["NestedObjects"] = new EcfgObject() {
            ["Are"] = new EcfgString("Cool"),
            ["Test"] = new EcfgLong(1),
            ["Test2"] = new EcfgLong(22908),
        },
        ["Arent"] = new EcfgString("They?"),
        ["WhatAbout"] = new EcfgObject() {
            ["AllOn"] = new EcfgLong(1),
            ["Long"] = new EcfgObject() {
                ["Line"] = new EcfgString("?"),
            },
        },
        ["YeahI"] = new EcfgString("Thought so"),
        ["Test2"] = new EcfgObject(),
        ["Empty"] = new EcfgObject(),
    }
}};
        yield return new object[] { @"
Hex: 0xFA83B
PositiveInfinity: +Infinity, Pos2: +InFiNiTy
NegativeInfinity: -Infinity, Neg2: -InFiNiTy
NaN: NaN
CaseNan: Nan, CaseNan2: nan
Decimal: 10.2938
Decomal2: .948
Scientific: 10.2e-2
Scientific2: 1.20123e+2
Double: 2.0
null: null
null2: NULl
Negitive: -120
true: tRue
faluse: falSE
", new EcfgObject() {
    ["Hex"] = new EcfgLong(0xFA83B),
    ["PositiveInfinity"] = new EcfgDouble(Double.PositiveInfinity),
    ["Pos2"] = new EcfgDouble(Double.PositiveInfinity),
    ["NegativeInfinity"] = new EcfgDouble(Double.NegativeInfinity),
    ["Neg2"] = new EcfgDouble(Double.NegativeInfinity),
    ["NaN"] = new EcfgDouble(Double.NaN),
    ["CaseNan"] = new EcfgDouble(Double.NaN),
    ["CaseNan2"] = new EcfgDouble(Double.NaN),
    ["Decimal"] = new EcfgDouble(10.2938),
    ["Decomal2"] = new EcfgDouble(.948),
    ["Scientific"] = new EcfgDouble(0.102),
    ["Scientific2"] = new EcfgDouble(120.123),
    ["Double"] = new EcfgDouble(2),
    ["null"] = null,
    ["null2"] = null,
    ["Negitive"] = new EcfgLong(-120),
    ["true"] = new EcfgBoolean(true),
    ["faluse"] = new EcfgBoolean(false),
}};
        yield return new object[] { @"
Array: [1
2, 3, 4 # Comment
5
# Comment
]
ArrayWithMixedTypes: [1, '2', 3.0 # Comment
NaN, {Hello: 'World'}] 
NestedArray: [1, [2, 3, 4], [5, 6, 7]]
NestedArrayMixedTypes: [1, [2, '3', 4.0, NaN, {Hello: 'World'}], [5, 6, 7]]
Namespaces: [ 'Server', ""Client"" ]
", new EcfgObject() {
    ["Array"] = new EcfgList(new EcfgNode?[] {
        new EcfgLong(1),
        new EcfgLong(2),
        new EcfgLong(3),
        new EcfgLong(4),
        new EcfgLong(5),
    }),
    ["ArrayWithMixedTypes"] = new EcfgList(new EcfgNode?[] {
        new EcfgLong(1),
        new EcfgString("2"),
        new EcfgDouble(3.0),
        new EcfgDouble(Double.NaN),
        new EcfgObject() {
            ["Hello"] = new EcfgString("World"),
        },
    }),
    ["NestedArray"] = new EcfgList(new EcfgNode?[] {
        new EcfgLong(1),
        new EcfgList(new EcfgNode?[] {
            new EcfgLong(2),
            new EcfgLong(3),
            new EcfgLong(4),
        }),
        new EcfgList(new EcfgNode?[] {
            new EcfgLong(5),
            new EcfgLong(6),
            new EcfgLong(7),
        }),
    }),
    ["NestedArrayMixedTypes"] = new EcfgList(new EcfgNode?[] {
        new EcfgLong(1),
        new EcfgList(new EcfgNode?[] {
            new EcfgLong(2),
            new EcfgString("3"),
            new EcfgDouble(4.0),
            new EcfgDouble(Double.NaN),
            new EcfgObject() {
                ["Hello"] = new EcfgString("World"),
            },
        }),
        new EcfgList(new EcfgNode?[] {
            new EcfgLong(5),
            new EcfgLong(6),
            new EcfgLong(7),
        }),
    }),
    ["Namespaces"] = new EcfgList(new EcfgNode?[] {
        new EcfgString("Server"),
        new EcfgString("Client"),
    }),
}};
        yield return new object[] { @"
MaxValueArray: [2147483647, 10, 21]
MinValueArray: [-1, 10, 21, -2147483648] 
BiggerMaxValArray: [10, 21, 2147483648]
", new EcfgObject() {
    ["MaxValueArray"] = new EcfgList(new EcfgNode?[] {
        new EcfgLong(2147483647),
        new EcfgLong(10),
        new EcfgLong(21),
    }),
    ["MinValueArray"] = new EcfgList(new EcfgNode?[] {
        new EcfgLong(-1),
        new EcfgLong(10),
        new EcfgLong(21),
        new EcfgLong(-2147483648),
    }),
    ["BiggerMaxValArray"] = new EcfgList(new EcfgNode?[] {
        new EcfgLong(10),
        new EcfgLong(21),
        new EcfgLong(2147483648),
    }),
}};
    }
}