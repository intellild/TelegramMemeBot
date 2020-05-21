module Parser

open System
open System.Numerics
open Telegram.Bot.Types

type State =
    { offset: int
      row: int
      col: int
      text: string
      entities: MessageEntity list }

type ParserResult<'a> = Result<'a, string * string>

type T<'a> = string * (State -> State * 'a ParserResult)

type NextItem =
    | Entity of String * MessageEntity
    | Single of Char
    | End

let init text entities =
    { offset = 0
      row = 0
      col = 0
      text = text
      entities = entities }

let sprintfResult r =
    match r with
    | (_, Ok (value)) -> sprintf "%A" value
    | (state, Error (label, error)) ->
        let failureCaret = sprintf "%*s^%s" state.col "" error
        sprintf "(%i,%i) Error parsing %s\n" state.row state.col label

let getLabel p =
    let (label, _) = p
    label

let setLabel p label =
    let (_, fn) = p
    (label,
     (fun state ->
         match fn state with
         | (state, Ok (value)) -> (state, Ok(value))
         | (state, Error ((_, msg))) -> (state, Error(label, msg))))

let (<?>) = setLabel

let private bumpRow state =
    { state with
          offset = state.offset + 1
          row = state.row + 1
          col = 0 }

let private bumpCol state =
    { state with
          offset = state.offset + 1
          col = state.col + 1 }

type ParserBuilder() =

    member this.Bind(m, f) =
        fun state ->
            match m state with
            | (state, Ok value) -> f value state
            | (state, Error (label, msg)) -> (state, Error(label, msg))

    member this.Return value = fun state -> (state, Ok(value))
    member this.ReturnFrom m = m
    member this.Yield value = fun state -> (state, Ok(value))
    member this.YieldFrom m = m
    member this.For(list, f) =
        List.foldBack (fun elem acc ->
            this.Bind(elem, (fun head -> this.Bind(acc, (fun tail state -> (state, Ok(head :: tail))))))) list
            (this.Return [])

let take state =
    let normal () =
        let ch = state.text.[state.offset]

        let state =
            if ch = '\n' then bumpRow state else bumpCol state
        (state, Ok(Single ch))
    if state.offset >= String.length state.text then
        (state, Ok(End))
    elif List.isEmpty state.entities then
        normal ()
    else
        let entity = List.head state.entities
        if entity.Offset = state.offset then
            ({ state with
                   offset = state.offset + entity.Length
                   col = state.col + entity.Length
                   entities = List.tail state.entities },
             Ok(Entity(state.text.[entity.Offset..(entity.Length + entity.Offset)], entity)))
        else
            normal ()

let result parserResult =
    fun state -> (state, parserResult)

let parser = ParserBuilder()

let satisfy predicate label =
    let p =
        parser {
            let! next = take
            let r =
                match next with
                | End -> Error(label, "Unexpected end of input")
                | Single (char) ->
                    if predicate char
                    then Ok(char)
                    else Error(label, sprintf "Unexpected character '%c'" char)
                | Entity (text, entity) ->
                    Error(label, sprintf "Unexpected entity '%s' with text '%s'" (entity.Type.ToString()) text)
            return! result r
        }
    (label, p)

let parseChar expect =
    let predicate ch = ch = expect
    let label = sprintf "%c" expect
    satisfy predicate label

let run p state =
    let (_, f) = p
    f state

let optional p =
    let label = sprintf "optional %s" <| getLabel p

    let impl state =
        match run p state with
        | (state, Ok value) -> (state, Ok <| Some value)
        | (_, Error _) -> (state, Ok None)
    (label, impl)

let map f p =
    let impl =
        parser {
            let! char = run p
            return f char }
    (getLabel p, impl)

let mapTo p r = map (fun _ -> r) p

let (>>%) = mapTo

let parseDigit = satisfy Char.IsDigit "digit"

let orElse p1 p2 =
    let label = sprintf "%s orElse %s" (getLabel p1) (getLabel p2)

    let orElseImpl state =
        match run p1 state with
        | (state, Ok value) -> (state, Ok(value))
        | (_, Error _) -> run p2 state
    (label, orElseImpl)

let (<|>) = orElse

let andThen combine p1 p2 =
    let label = sprintf "%s andThen %s" (getLabel p1) (getLabel p2)

    let andThenImpl =
        parser {
            let! value1 = run p1
            let! value2 = run p2
            return combine value1 value2 }
    (label, andThenImpl)

let (.>>.) = andThen

let choice list = List.reduce orElse list

let any list =
    list
    |> List.map parseChar
    |> choice

let many p =
    let rec manyImpl f state =
        match run p state with
        | (_, Error _) -> (state, Ok(f []))
        | (state, Ok head) -> manyImpl (fun tail -> f <| head :: tail) state
    ("", manyImpl id)

let many1 p =
    let many1Impl =
        parser {
            let! head = run p
            let! tail = run <| many p
            return head :: tail }
    ("", many1Impl)

let sequence plist =
    parser {
        for p in (List.map run plist) do
            yield! run p
    }

let parseString str =
    let label = sprintf "parse string \'%s\'" str

    let p =
        str
        |> List.ofSeq
        |> List.map parseChar
        |> sequence

    let inner =
        parser {
            let! chars = p
            return String(Array.ofList chars) }
    (label, inner)

let unescaped = satisfy (fun ch -> ch <> '\\' && ch <> '\"') "unescaped char"

let escaped =
    [ ("\\\"", '\"')
      ("\\\\", '\\')
      ("\\/", '/')
      ("\\b", '\b')
      ("\\f", '\f')
      ("\\n", '\n')
      ("\\r", '\r')
      ("\\t", '\t') ]
    |> List.map (fun (toMatch, result) -> parseString toMatch >>% result)
    |> choice
    <?> "escaped char"

let unicode =
    let label = ""
    let hexDigit = any ([ '0' .. '9' ] @ [ 'A' .. 'F' ] @ [ 'a' .. 'f' ])

    let impl =
        parser {
            let! _ = run <| parseChar '\\'
            let! _ = run <| parseChar 'u'
            let! a = run hexDigit
            let! b = run hexDigit
            let! c = run hexDigit
            let! d = run hexDigit
            let str = sprintf "%c%c%c%c" a b c d
            return UInt32.Parse(str, Globalization.NumberStyles.HexNumber) |> char
        }
    (label, impl)

let charLiteral = unescaped <|> escaped <|> unicode

let stringLiteral =
    let impl =
        parser {
            let! _ = run <| parseChar '\"'
            let! chars = run <| many charLiteral
            let! _ = run <| parseChar '\"'
            return String(Array.ofList chars) }
    ("string", impl)

let integerLiteral =
    let impl =
        parser {
            let! minus =
                parseChar '-'
                |> optional
                |> run
            let! value = many1 parseDigit |> run
            let result =
                match minus with
                | Some _ -> '-'::value |> Array.ofList |> String
                | None -> value |> Array.ofList |> String
            return BigInteger.Parse(result)
        }
    ("number", impl)
