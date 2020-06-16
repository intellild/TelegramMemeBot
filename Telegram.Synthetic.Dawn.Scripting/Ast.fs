module Telegram.Synthetic.Dawn.Scripting.Ast

type Constant =
    | Bool of bool
    | Int of bigint
    | String of string
    | Atom of string

type Identifier = Identifier of string
