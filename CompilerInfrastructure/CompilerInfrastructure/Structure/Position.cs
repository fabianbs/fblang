/******************************************************************************
 * Copyright (c) 2019 Fabian Schiebel.
 * All rights reserved. This program and the accompanying materials are made
 * available under the terms of LICENSE.txt.
 *
 *****************************************************************************/

using CompilerInfrastructure.Utils;
using System;
using System.Collections.Generic;
using System.Text;
namespace CompilerInfrastructure.Structure {
    [Serializable]
    public readonly struct Position : IEquatable<Position> {

        public Position(string fileName, int line, int column) {
            FileName = fileName ?? "";
            Line = line;
            Column = column;
            Next = null;
        }
        public Position(string filename, int line, int column, ReadOnlyBox<Position> nxt)
            : this(filename, line, column) {
            Next = nxt;
        }
        public Position(Position toCpy) {
            FileName = toCpy.FileName;
            Line = toCpy.Line;
            Column = toCpy.Column;
            Next = toCpy.Next;
        }
        public ReadOnlyBox<Position> Next {
            get;
        }
        public string FileName {
            get;
        }
        public int Line {
            get;
        }
        public int Column {
            get;
        }


        public override bool Equals(object obj) => obj is Position && Equals((Position)obj);
        public bool Equals(Position other) => FileName == other.FileName && Line == other.Line && Column == other.Column && Equals(Next, other.Next);

        public override int GetHashCode() {
            var hashCode = 831367854;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            hashCode = hashCode * -1521134295 + Line.GetHashCode();
            hashCode = hashCode * -1521134295 + Column.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<ReadOnlyBox<Position>>.Default.GetHashCode(Next);
            return hashCode;
        }

        public override string ToString() {
            if (Next is null || !Next.HasValue)
                return $"{FileName} at ({Line}, {Column})";
            else
                return $"{FileName} at ({Line}, {Column}){Environment.NewLine}\t>inlined at {Next.Value.ToString()}";
        }

        public void Deconstruct(out string fileName, out int line, out int col) {
            (fileName, line, col) = (FileName, Line, Column);
        }

        public static bool operator ==(Position position1, Position position2) => position1.Equals(position2);
        public static bool operator !=(Position position1, Position position2) => !(position1 == position2);

        public static implicit operator Position((string, int, int) pos) {
            return new Position(pos.Item1, pos.Item2, pos.Item3);
        }
        public Position Concat(Position nxt) {
            return new Position(FileName, Line, Column, new ReadOnlyBox<Position>(nxt));
        }
        public Position Concat(ReadOnlyBox<Position> nxt) {
            return new Position(FileName, Line, Column, nxt);
        }
    }
}
