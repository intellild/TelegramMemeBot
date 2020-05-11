module Parser

open System
open Telegram.Bot.Types

type Position = int * int

type Input = string * MessageEntity list

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

let sprintfResult result =
    match result with
    | (_, Ok (value)) -> sprintf "%A" value
    | (state, Error (label, error)) ->
        let failureCaret = sprintf "%*s^%s" state.col "" error
        sprintf "(%i,%i) Error parsing %s\n" state.row state.col label

let getLabel p =
    let (label, _) = p
    label

let setLabel parser label =
    let (_, fn) = parser
    (label,
     (fun state ->
         match fn state with
         | Ok (value) -> (state, Ok(value))
         | Error ((_, msg)) -> (state, Error(label, msg))))

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
            return result r
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
