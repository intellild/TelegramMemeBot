module Parser

open System
open Telegram.Bot.Types

type State =
    { offset: int
      row: int
      col: int
      text: string
      entities: MessageEntity list }

type ParserResult<'a> = Result<'a, string * string>

type T<'a> = string * (State -> State * 'a ParserResult)

type Item =
    | Entity of string * MessageEntity
    | Single of Char
    | End

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

let rec take state =
    let normal =
        let ch = state.text.[state.offset + 1]
        if ch = '\n' then bumpRow state |> take
        elif Char.IsWhiteSpace ch then bumpCol state |> take
        else (bumpCol state, Ok(Single(ch)))
    if state.offset >= String.length state.text then
        (state, Ok(End))
    elif List.isEmpty state.entities then
        normal
    else
        let entity = List.head state.entities
        if entity.Offset = state.offset then
            ({ state with
                   offset = state.offset + entity.Length
                   col = state.col + entity.Length
                   entities = List.tail state.entities },
             Ok(Entity(state.text.[entity.Offset..(entity.Length + entity.Offset)], entity)))
        else
            normal

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

let char expect =
    let predicate ch = ch = expect
    let label = sprintf "%c" expect
    satisfy predicate label

let digit () = satisfy Char.IsDigit "digit"

let run p state =
    let (_, f) = p
    f state

let map f p =
    let impl =
        parser {
            let! char = run p
            return f char }
    (getLabel p, impl)

let orElse p1 p2 =
    let label = sprintf "%s orElse %s" (getLabel p1) (getLabel p2)

    let orElseImpl state =
        match run p1 state with
        | (state, Ok value) -> (state, Ok(value))
        | (_, Error _) -> run p2 state
    (label, orElseImpl)

let (<|>) = orElse

let andThen p1 p2 =
    let label = sprintf "%s andThen %s" (getLabel p1) (getLabel p2)

    let andThenImpl =
        parser {
            let! _ = run p1
            return! run p2 }
    (label, andThenImpl)

let (.>>.) = andThen

let choice list = List.reduce orElse list

let any list =
    list
    |> List.map char
    |> choice

let many p =
    let rec manyImpl f state =
        match run p state with
        | (_, Error _) -> (state, Ok(f []))
        | (state, Ok value) -> manyImpl (fun rest -> f <| List.Cons(value, rest)) state
    ("", manyImpl id)

let many1 p =
    let many1Impl =
        parser {
            let! value = run p
            let! rest = run <| many p
            return List.Cons(value, rest) }
    ("", many1Impl)

let not dummy p =
    let label = sprintf "not %s" <| getLabel p

    let notImpl state =
        match run p state with
        | (state, Ok value) -> (state, Error(label, sprintf "Unexpected \"%A\"" value))
        | (_, Error _) -> (state, Ok(dummy))
    (label, notImpl)

let escaped =
    let escapedImpl =
        parser {
            let! _ = run <| char '\\'
            let! next = run <| any [ 'n'; 't'; '\\'; '\"' ]
            let r =
                match next with
                | 'n' -> Ok('\n')
                | 't' -> Ok('\t')
                | '\\' -> Ok('\\')
                | '\"' -> Ok('\"')
                | char -> Error("escaped", sprintf "Unexpected character '%c'" char)
            return! result r
        }
    ("escaped", escapedImpl)

let stringContent: T<string> =
    let stringContentImpl =
        [ escaped |> (map (fun char -> sprintf "%c" char))
          not "" <| char '\"' ]
        |> choice
        |> many
        |> (map <| List.reduce (+))
    setLabel stringContentImpl "string"
