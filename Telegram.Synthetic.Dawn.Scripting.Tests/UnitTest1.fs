module Telegram.Synthetic.Dawn.Scripting.Tests

open System.Numerics
open NUnit.Framework
open Parser

[<SetUp>]
let Setup () =
    ()

[<Test>]
let testParseChar () =
    let state = init "c" []
    match run (parseChar 'c') state with
    | (_, Ok value) -> Assert.AreEqual('c', value)
    | _ -> Assert.Fail("error")

[<Test>]
let testOutOfBound () =
    let state = init "" []
    match run (parseChar 'c') state with
    | (_, Error _) -> Assert.Pass()
    | _ -> Assert.Fail("error")

[<Test>]
let testParseDigit () =
    let state = init "0" []
    match run parseDigit state with
    | (_, Ok value) -> Assert.AreEqual('0', value)
    | _ -> Assert.Fail("error")

[<Test>]
let testParseString () =
    let state = init "abc" []
    match run (parseString "abc") state with
    | (_, Ok value) -> Assert.AreEqual("abc", value)
    | _ -> Assert.Fail("error")

[<Test>]
let testMany () =
    let manyA = many <| parseChar 'a'
    let state = init "aaab" []
    match run manyA state with
    | ({ offset = 3; row = 0; col = 3 }, Ok value) -> Assert.AreEqual("aaa", value)
    | _ -> Assert.Fail("error")

[<Test>]
let testMany1 () =
    let manyA = many1 <| parseChar 'a'
    let state = init "aaab" []
    match run manyA state with
    | ({ offset = 3; row = 0; col = 3 }, Ok value) -> Assert.AreEqual("aaa", value)
    | _ -> Assert.Fail("error")

[<Test>]
let testMany1Error () =
    let manyA = many1 <| parseChar 'a'
    let state = init "b" []
    match run manyA state with
    | (_, Error _) -> Assert.Pass()
    | _ -> Assert.Fail("error")

[<Test>]
let testStringLiteral () =
    let state =
        init "\"\\\\abc abc\\n\\t\\b\\f\\r\\/\\\"\"" []
    match run stringLiteral state with
    | (_, Ok value) -> Assert.AreEqual("\\abc abc\n\t\b\f\r/\"", value)
    | _ -> Assert.Fail("error")

[<Test>]
let testPositiveIntegerLiteral () =
    let state = init "91389681247993671255432112000000" []
    match run integerLiteral state with
    | (_, Ok value) -> Assert.AreEqual(BigInteger.Parse("91389681247993671255432112000000"), value)
    | _ -> Assert.Fail("error")

[<Test>]
let testNegativeIntegerLiteral () =
    let state = init "-91389681247993671255432112000000" []
    match run integerLiteral state with
    | (_, Ok value) -> Assert.AreEqual(BigInteger.Parse("-91389681247993671255432112000000"), value)
    | _ -> Assert.Fail("error")

[<Test>]
let testIdentifier () =
    let state = init "abc?-abc?   " []
    match run identifier state with
    | (_, Ok value) -> Assert.AreEqual("abc?-abc?", value)
    | _ -> Assert.Fail("error")

[<Test>]
let testAtom () =
    let state = init ":abc?-abc?   " []
    match run atom state with
    | (_, Ok value) -> Assert.AreEqual("abc?-abc?", value)
    | _ -> Assert.Fail("error")
