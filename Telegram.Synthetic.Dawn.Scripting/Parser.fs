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

type T<'a> = Parser of (string * (State -> State * 'a ParserResult))

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

let getLabel parser =
    let (label, _) = parser
    label

let setLabel parser label =
    let (_, fn) = parser
    Parser
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

    static let rec take state =
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

    member this.Bind(m, f) =
        fun state ->
            match m state with
            | (state, Ok value) -> f value state
            | (state, Error (label, msg)) -> (state, Error(label, msg))

    member this.Return value = fun state -> (state, Ok(value))
    member this.ReturnFrom = id

    member this.Result(result: ParserResult<'a>) = fun state -> (state, result)

    member this.Take() = take

let parser = ParserBuilder()

let satisfy predicate label =
    let p =
        parser {
            let! next = parser.Take()
            let r =
                match next with
                | End -> Error(label, "Unexpected end of input")
                | Single (char) ->
                    if predicate char
                    then Ok(char)
                    else Error(label, sprintf "Unexpected character '%c'" char)
                | Entity (text, entity) ->
                    Error(label, sprintf "Unexpected entity '%s' with text '%s'" (entity.Type.ToString()) text)
            return (parser.Result(r))
        }
    Parser(label, p)

let char expect =
    let predicate ch = ch = expect
    let label = sprintf "%c" expect
    satisfy predicate label

let digit () = satisfy Char.IsDigit "digit"
