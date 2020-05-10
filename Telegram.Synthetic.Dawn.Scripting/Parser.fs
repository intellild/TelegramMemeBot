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

type T<'a> = Parser of (string * (State -> Result<'a * State, string * string>))

type Item =
    | Entity of string * MessageEntity * State
    | Single of Char * State
    | End

let getLabel parser =
    let (label, _) = parser
    label

let setLabel parser label =
    let (_, fn) = parser
    Parser
        (label,
         (fun state ->
             match fn state with
             | Ok (r) -> Ok(r)
             | Error ((_, msg)) -> Error(label, msg)))

let private bumpRow state =
    { state with
          offset = state.offset + 1
          row = state.row + 1
          col = 0 }

let private bumpCol state =
    { state with
          offset = state.offset + 1
          col = state.col + 1 }

let rec take state =
    let normal =
        let ch = state.text.[state.offset + 1]
        if ch = '\n' then take <| bumpRow state
        else if Char.IsWhiteSpace ch then take <| bumpCol state
        else Single(ch, bumpCol state)
    
    if state.offset >= String.length state.text then
        End
    elif List.isEmpty state.entities then
        normal
    else
        let entity = List.head state.entities
        if entity.Offset = state.offset then
            Entity
                (state.text.[entity.Offset..(entity.Length + entity.Offset)], entity,
                 { state with
                       offset = state.offset + entity.Length
                       col = state.col + entity.Length
                       entities = List.tail state.entities })
        else
            normal

(*
let satisfy predicate label =
    Parser
        (label,
         (fun state ->
             if state.offset >= String.length state.text then
                 Error(label, "Unexpected end of input")
             else
                 let ch = state.text.[state.offset + 1]
                 if predicate ch
                 then Ok(ch, { state with offset = state.offset + 1 })
                 else Error(label, sprintf "Unexpected '%c'" ch)))

let char expect =
    let predicate ch = ch = expect
    let label = sprintf "%c" expect
    satisfy predicate label

let digit = satisfy Char.IsDigit "digit"
*)
