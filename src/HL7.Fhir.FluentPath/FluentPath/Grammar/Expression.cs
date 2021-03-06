﻿/* 
 * Copyright (c) 2015, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Sprache;

namespace Hl7.Fhir.FluentPath.Grammar
{
    internal class Expression
    {
        // fpconst: STRING |
        //   '-'? NUMBER |
        //   BOOL |
        //   CONST |
        //   DATETIME;
        public static readonly Parser<Evaluator> FpConst =
            Lexer.String.Select(s => Eval.TypedValue(s))
            .XOr(Lexer.DateTime.Select(dt => Eval.TypedValue(dt)))
            .Or(Lexer.DecimalNumber.Select(d => Eval.TypedValue(d)))
            .Or(Lexer.Number.Select(n => Eval.TypedValue(n)))
            .Or(Lexer.Bool.Select(b => Eval.TypedValue(b)))
            .XOr(Lexer.Const.Select(s => Eval.Constant(s)));

        // term:
        //   '(' expr ')' |
        //   predicate |
        //   fpconst;
        public static readonly Parser<Evaluator> BracketExpr =
            (from lparen in Parse.Char('(')
             from expr in Parse.Ref(() => Expr)
             from rparen in Parse.Char(')')
             select expr)
            .Named("BracketExpr");

        public static readonly Parser<Evaluator> Term =
            FpConst
            .Or(Path.Invoc)
            .XOr(BracketExpr)
            .Token()
            .Named("Term");


        //expr:
        //  term '.' invoc |
        //  expr ('*' | '/') expr |
        //  expr ('+' | '-') expr |
        //  expr ('|' | '&') expr |
        //  expr COMP expr |
        //  expr LOGIC expr;
        public static readonly Parser<Evaluator> InvocExpr =
            from term in Term
            from invoc in 
                (from firstOp in Parse.Char('.')
                 from invocs in Parse.ChainOperator(Parse.Char('.'), Path.Invoc, (op, left, right) => left.Then(right))
                 select invocs).Optional()
            select invoc.IsEmpty ? term : term.Then(invoc.Get());

        public static readonly Parser<Evaluator> MulExpr =
          Parse.ChainOperator(Lexer.Mul.Or(Lexer.Div), InvocExpr, (op, left, right) => left.Infix(op, right));

        public static readonly Parser<Evaluator> AddExpr =
            Parse.ChainOperator(Lexer.Add.Or(Lexer.Sub), MulExpr, (op, left, right) => left.Infix(op, right));

        public static readonly Parser<Evaluator> JoinExpr =
            Parse.ChainOperator(Lexer.Union.Or(Lexer.Concat), AddExpr, (op, left, right) => left.Infix(op, right));

        public static readonly Parser<Evaluator> CompExpr =
            Parse.ChainOperator(Lexer.Comp, JoinExpr, (op, left, right) => left.Infix(op, right));

        public static readonly Parser<Evaluator> LogicExpr =
            Parse.ChainOperator(Lexer.Logic, CompExpr, (op, left, right) => left.Infix(op, right));

        public static readonly Parser<Evaluator> Expr = LogicExpr;    
    }
}
